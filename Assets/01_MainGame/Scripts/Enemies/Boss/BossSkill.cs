using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class BossSkill : EnemySkillBase
{
    [Header("攻撃設定")]
    [SerializeField] private int damage = 30;

    [Header("衝撃波")]
    [SerializeField] private GameObject warningPrefab;
    [SerializeField] private GameObject shockwavePrefab;

    [SerializeField] private float warningTime = 1f;
    [SerializeField] private float shockwaveSpeed = 8f;

    [SerializeField] private NavMeshAgent agent;

    private bool isUsingSkill = false;

    // スキル使用中かどうか
    public bool IsUsingSkill => isUsingSkill;

    [SerializeField] private BossController controller;

    public override bool TryUseSkill()
    {
        if (controller != null && controller.IsDead)
            return false;

        if (!CanUseSkill() || isUsingSkill)
            return false;

        enemyController.PlaySkillAnimation();

        StartCoroutine(ShockwaveAttack());

        return true;
    }

    private IEnumerator ShockwaveAttack()
    {
        if (controller.IsDead)
            yield break;

        Debug.Log("ボスが衝撃波を発動しました。");

        // スキル発動中はボスを停止
        if (agent != null)
            agent.isStopped = true;

        isUsingSkill = true;

        // =========================
        // 攻撃範囲の円
        // =========================

        GameObject warning = Instantiate(
            warningPrefab,
            transform.position + Vector3.up * 0.05f,
            Quaternion.Euler(90f, 0f, 0f));

        float targetScale = SkillRange * 2f;

        warning.transform.localScale =
            new Vector3(targetScale, targetScale, targetScale);

        // =========================
        // 広がる波
        // =========================

        GameObject wave = Instantiate(
            shockwavePrefab,
            transform.position + Vector3.up * 0.1f,
            Quaternion.Euler(90f, 0f, 0f));

        wave.transform.localScale = Vector3.zero;

        float currentScale = 0f;

        while (currentScale < targetScale)
        {
            if (controller.IsDead)
            {
                Destroy(warning);
                Destroy(wave);

                isUsingSkill = false;
                yield break;
            }

            currentScale += shockwaveSpeed * Time.deltaTime;

            if (currentScale >= targetScale)
                currentScale = targetScale;

            wave.transform.localScale =
                new Vector3(
                    currentScale,
                    currentScale,
                    currentScale);

            yield return null;
        }

        // =========================
        // 攻撃範囲
        // =========================

        float attackRadius =
            warning.transform.lossyScale.x / 2f + 2.0f;

        Debug.Log(
            $"ボススキル攻撃判定開始 / 範囲 = {attackRadius}");

        if (controller.IsDead)
        {
            Destroy(warning);
            Destroy(wave);

            isUsingSkill = false;
            yield break;
        }

        // =========================
        // 攻撃判定
        // =========================

        Collider[] hits =
            Physics.OverlapSphere(
                transform.position,
                attackRadius);

        foreach (Collider hit in hits)
        {
            // ==================================
            // プレイヤーを攻撃
            // ==================================

            PlayerStateMachine player =
                hit.GetComponentInParent<PlayerStateMachine>();

            if (player != null)
            {
                if (player.CurrentStateName ==
                    nameof(HijackedState))
                {
                    EnemyController hijackedEnemy =
                        player.Hijacked.CurrentEnemy;

                    // 乗っ取り直後の無敵
                    if (hijackedEnemy != null &&
                        hijackedEnemy.IsHijackInvincible())
                    {
                        Debug.Log(
                            "乗っ取り直後の無敵時間中なのでボススキルを無効化");

                        continue;
                    }

                    player.PlayerHP.TakeDamage(damage);

                    Debug.Log(
                        $"ボススキル → プレイヤーに {damage} ダメージ");
                }
                else if (player.CurrentStateName ==
                         nameof(GhostState))
                {
                    player.MarkKilledByBoss();
                    player.Ghost.OnHit();

                    Debug.Log(
                        "ボススキル → ゴーストに命中");
                }

                // プレイヤーだった場合は敵判定をしない
                continue;
            }

            // ==================================
            // 敵を攻撃
            // ==================================

            //EnemyController enemy =
            //    hit.GetComponentInParent<EnemyController>();

            //if (enemy == null)
            //    continue;

            //// ボス自身には当てない
            //if (enemy == controller)
            //    continue;

            //// 死亡済みの敵には当てない
            //if (enemy.CurrentHP <= 0)
            //    continue;

            //enemy.TakeDamage(damage);

            //Debug.Log(
            //    $"ボススキル → {enemy.name} に {damage} ダメージ");
        }

        // 少し表示してから消す
        yield return new WaitForSeconds(0.1f);

        Destroy(warning);
        Destroy(wave);

        isUsingSkill = false;

        if (agent != null)
            agent.isStopped = false;

        // スキルのクールダウン開始
        ResetCooldown();
    }
}

