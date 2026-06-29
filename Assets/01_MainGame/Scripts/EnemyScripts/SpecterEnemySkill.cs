using UnityEngine;
using System.Collections;

public class SpecterEnemySkill : EnemySkillBase
{
    //幽霊状態の時は敵から発見されない
    //幽霊状態の時は最初に与えるダメージが2倍、移動速度1.5倍になる
    //幽霊状態の時はダメージを食らわない(ボスのスキルは例外)
    //幽霊状態の時間は5秒でスキルクールタイムは10秒にする

    //幽霊状態の持続時間
    [SerializeField] private float invisibleDuration = 5f;

    //幽霊状態のフラグ
    private bool isInvisible = false;
    private Coroutine invisibleCoroutine;

    public bool IsInvisible => isInvisible;

    [SerializeField] private Renderer[] bodyRenderers;
    [SerializeField] private Material normalMat;
    [SerializeField] private Material invisibleMat;

    [SerializeField] private GameObject bufficon;

    private Coroutine fadeCoroutine;
    public override bool TryUseSkill()
    {
        if (!CanUseSkill())
            return false;

        ActivateInvisible();

        ResetCooldown();

        return true;
    }

    private void ActivateInvisible()
    {
        if (invisibleCoroutine != null)
            StopCoroutine(invisibleCoroutine);

        invisibleCoroutine =
            StartCoroutine(InvisibleRoutine());
    }

    private IEnumerator InvisibleRoutine()
    {
        if (enemyController.IsHijacked)
        {
            BuffUIController.Instance.ShowBuff(BuffType.SpecterBuff);
        }
        else
        {
            bufficon.SetActive(true);
        }
        //bufficon.SetActive(true);
        isInvisible = true;
        // ダメージ倍率を2倍にする
        enemyController.SetDamageMultiplier(2f);
        //敵の時の速さを2倍にする
        enemyController.SetSpeedMultiplier(1.5f);
        // 見た目を透明化
        SetInvisible(true);

        enemyController.SetHidden(true);

        Debug.Log("スペクター透明化開始");

        yield return new WaitForSeconds(invisibleDuration);

        RemoveInvisible();
    }

    public void RemoveInvisible()
    {
        if (!isInvisible)
            return;

        if (invisibleCoroutine != null)
        {
            StopCoroutine(invisibleCoroutine);
            invisibleCoroutine = null;
        }

        isInvisible = false;

        if (enemyController.IsHijacked)
        {
            BuffUIController.Instance.HideBuff(BuffType.SpecterBuff);
        }
        else
        {
            bufficon.SetActive(false);
        }
        //bufficon.SetActive(false);

        enemyController.SetHidden(false);

        enemyController.SetDamageMultiplier(1f);
        enemyController.SetSpeedMultiplier(1f);

        SetInvisible(false);
    }
    private void SetInvisible(bool invisible)
    {
        if (fadeCoroutine != null)
            StopCoroutine(fadeCoroutine);

        fadeCoroutine = StartCoroutine(FadeRoutine(invisible));
    }

    private IEnumerator FadeRoutine(bool invisible)
    {
        float duration = 0.5f;
        float time = 0f;

        if (invisible)
        {
            // 先に透明用マテリアルに切り替える
            foreach (Renderer r in bodyRenderers)
            {
                r.material = new Material(invisibleMat);
            }
        }

        float startAlpha = invisible ? 1f : 0.1f;
        float targetAlpha = invisible ? 0.1f : 1f;

        while (time < duration)
        {
            time += Time.deltaTime;

            float alpha = Mathf.Lerp(
                startAlpha,
                targetAlpha,
                time / duration);

            foreach (Renderer r in bodyRenderers)
            {
                Color c = r.material.color;
                c.a = alpha;
                r.material.color = c;
            }

            yield return null;
        }

        // 元に戻す時だけ normalMat に戻す
        if (!invisible)
        {
            foreach (Renderer r in bodyRenderers)
            {
                r.material = normalMat;
            }
        }

        fadeCoroutine = null;
    }
}
