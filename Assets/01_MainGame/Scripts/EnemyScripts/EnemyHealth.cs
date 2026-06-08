using System.Collections;
using UnityEngine;

public class EnemyHealth : MonoBehaviour
{
    [SerializeField] private int _maxHP = 100;
    public int maxHP => _maxHP;
    private int currentHP;

    //無敵状態のフラグ
    private bool invincible = false;

    private EnemyController enemyController;


    private Coroutine poisonCoroutine;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        currentHP = maxHP;

        enemyController = GetComponent<EnemyController>();
    }

    // 外から読み取りだけ可能
    //現在のHPを取得するプロパティ
    public int CurrentHP
    {
        get { return currentHP; }
    }

    //無敵状態のフラグを取得するプロパティ
    public bool Invincible
    {
        get { return invincible; }
    }   


    //HPバーに表示する敵のHP
    public float HPRatio
    {
        get { return (float)currentHP / maxHP; }
    }

    // Update is called once per frame
    void Update()
    {
      
    }

    public void TakeDamage(int damage)
    {
        if (invincible == false)
        {
            currentHP = Mathf.Max(0, currentHP - damage);
        }
        Debug.Log("敵がダメージを受けました！現在のHP: " + currentHP);

        //敵がプレイヤーからダメージを受けたらチェイスモードに移行する
         enemyController?.AlertDamage();
    }

    public void Heal(int healAmount)
    {
        currentHP = Mathf.Min(maxHP, currentHP + healAmount);
        Debug.Log("敵が回復しました！現在のHP: " + currentHP);
    }


    //無敵状態
    public IEnumerator InvincibleTime(float time)
    {
        invincible = true;

        //hpが0にならないようにmaxhpの10分の三のhpを回復させる
        currentHP = Mathf.Max(currentHP, _maxHP/2);

        Debug.Log("無敵開始");

        yield return new WaitForSeconds(time);

        invincible = false;

        Debug.Log("無敵終了");
    }

    //キノコの毒攻撃を受けたときに呼び出される関数

    public void ApplyPoison(float duration, float interval, float percent)
    {
        if (poisonCoroutine != null)
            return;

        poisonCoroutine =
            StartCoroutine(PoisonCoroutine(duration, interval, percent));
    }

    private IEnumerator PoisonCoroutine(
        float duration,
        float interval,
        float percent)
    {
        float timer = 0f;

        while (timer < duration)
        {
            yield return new WaitForSeconds(interval);

            int poisonDamage =
                Mathf.Max(1, Mathf.CeilToInt(CurrentHP * percent));

            TakeDamage(poisonDamage);

            Debug.Log($"敵に毒ダメージ {poisonDamage}");

            timer += interval;
        }

        poisonCoroutine = null;
    }
}
