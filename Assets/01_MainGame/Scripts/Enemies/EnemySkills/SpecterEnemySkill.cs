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

    [SerializeField] private ParticleSystem buffParticle;

    private Coroutine fadeCoroutine;


    // =========================================
    // Collider関連
    // =========================================

    private Collider[] playerColliders;
    private Collider[] enemyColliders;


    public override bool TryUseSkill()
    {
        if (!CanUseSkill())
            return false;

        enemyController.PlaySkillAnimation();

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
            FindAnyObjectByType<PlayerStateMachine>()?.StartSpecterBuff(invisibleDuration);
        }
        else
        {
            bufficon.SetActive(true);
        }

        if (buffParticle != null)
        {
            buffParticle.Play();
        }

        isInvisible = true;

        // ダメージ倍率を2倍にする
        enemyController.SetDamageMultiplier(2f);

        // 敵の時の速さを1.5倍にする
        enemyController.SetSpeedMultiplier(1.5f);

        // 見た目を透明化
        SetInvisible(true);

        // 敵から見つからない
        enemyController.SetHidden(true);

        // ★ プレイヤーと敵の衝突を無視
        IgnorePlayerCollision(true);

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

        if (buffParticle != null)
        {
            buffParticle.Stop();
        }

        isInvisible = false;

        BuffUIController.Instance.HideBuff(BuffType.SpecterBuff);

        bufficon.SetActive(false);

        enemyController.SetHidden(false);

        enemyController.SetDamageMultiplier(1f);
        enemyController.SetSpeedMultiplier(1f);

        SetInvisible(false);

        // ★ プレイヤーと敵の衝突を元に戻す
        IgnorePlayerCollision(false);
    }


    // =========================================
    // プレイヤーと敵の衝突を無視する
    // =========================================

    private void IgnorePlayerCollision(bool ignore)
    {
        GameObject playerObject =
            GameObject.FindGameObjectWithTag("Player");

        if (playerObject == null)
        {
            Debug.LogWarning("Playerが見つかりませんでした。");
            return;
        }

        // プレイヤー側のColliderを取得
        playerColliders =
            playerObject.GetComponentsInChildren<Collider>(true);

        // スペクター側のColliderを取得
        enemyColliders =
            enemyController.GetComponentsInChildren<Collider>(true);

        foreach (Collider playerCollider in playerColliders)
        {
            if (playerCollider == null)
                continue;

            foreach (Collider enemyCollider in enemyColliders)
            {
                if (enemyCollider == null)
                    continue;

                Physics.IgnoreCollision(
                    playerCollider,
                    enemyCollider,
                    ignore);
            }
        }

        Debug.Log(
            ignore
            ? "スペクター透明化：プレイヤーとの衝突を無視"
            : "スペクター透明化解除：プレイヤーとの衝突を復元");
    }


    // =========================================
    // 透明化
    // =========================================

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