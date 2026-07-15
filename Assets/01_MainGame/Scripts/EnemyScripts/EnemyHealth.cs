using System;
using System.Collections;
using Unity.VisualScripting;
using UnityEngine;

public class EnemyHealth : MonoBehaviour
{
    [SerializeField] protected int _maxHP = 100;
    public int maxHP => _maxHP;

    protected int currentHP;

    protected bool invincible = false;

    protected EnemyController enemyController;

    private Coroutine poisonCoroutine;

    private Coroutine bleedCoroutine;

    private EnemyBuffUI enemyBuffUI;

    //回復エフェクト
    [SerializeField] private ParticleSystem healEffect;


    protected virtual void Start()
    {
        currentHP = maxHP;
        enemyController = GetComponent<EnemyController>();
        enemyBuffUI = GetComponent<EnemyBuffUI>();
    }

    public int CurrentHP => currentHP;

    public bool Invincible => invincible;

    public float HPRatio => (float)currentHP / maxHP;

    public virtual void TakeDamage(int damage)
    {
        if (!invincible)
        {
            currentHP = Mathf.Max(0, currentHP - damage);
        }

        Debug.Log("敵がダメージを受けました！現在のHP: " + currentHP);

        enemyController?.AlertDamage();
    }

    public virtual void Heal(int healAmount)
    {
        currentHP = Mathf.Min(maxHP, currentHP + healAmount);
        StartCoroutine(HealEfects());
    }

    //回復エフェクトを再生するためのメソッド
    public virtual IEnumerator HealEfects()
    {
        healEffect.Play();
        yield return new WaitForSeconds(2f);
        healEffect.Stop();
    }

    public virtual IEnumerator InvincibleTime(float time)
    {
        invincible = true;

        currentHP = Mathf.Max(currentHP, _maxHP / 2);

        yield return new WaitForSeconds(time);

        invincible = false;
    }

    public virtual void ApplyPoison(
        float duration,
        float interval,
        float percent)
    {
        if (poisonCoroutine != null)
            return;

        poisonCoroutine =
            StartCoroutine(
                PoisonCoroutine(duration, interval, percent));
    }

    protected IEnumerator PoisonCoroutine(
        float duration,
        float interval,
        float percent)
    {
        float timer = 0f;

        // 毒状態のアイコンを表示
        enemyBuffUI?.ShowPoisonDebuff(duration);

        while (timer < duration)
        {
            yield return new WaitForSeconds(interval);

            int poisonDamage =
                Mathf.Max(
                    1,
                    Mathf.CeilToInt(CurrentHP * percent));

            TakeDamage(poisonDamage);

            timer += interval;
        }

        poisonCoroutine = null;
    }

    public void ApplyBleed(float duration)
    {
        if (bleedCoroutine != null)
            return;

        bleedCoroutine =
            StartCoroutine(BleedCoroutine(duration));

    }

    private IEnumerator BleedCoroutine(float duration)
    {
        float timer = 0f;

        // 出血状態のアイコンを表示
        enemyBuffUI?.ShowBloodDebuff(duration);

        while (timer < duration)
        {
            TakeDamage(7);   // 1秒ごと7ダメージ

            yield return new WaitForSeconds(1f);

            timer += 1f;
        }

        bleedCoroutine = null;
    }
}