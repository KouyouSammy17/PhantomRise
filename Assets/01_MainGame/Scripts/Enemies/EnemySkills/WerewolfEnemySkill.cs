using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class WerewolfEnemySkill : EnemySkillBase
{
    [SerializeField] private float chargeDistance = 5f;
    [SerializeField] private float chargeSpeed = 12f;

    private bool isUsingSkill = false;

    private NavMeshAgent agent;

    private WerewolfChargeAttack chargeAttack;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        chargeAttack = GetComponent<WerewolfChargeAttack>();
    }

    public override bool TryUseSkill()
    {
        if (!CanUseSkill())
            return false;

        if (isUsingSkill)
            return false;

        StartCoroutine(SkillRoutine());

        return true;
    }

    private IEnumerator SkillRoutine()
    {
        isUsingSkill = true;

        // アニメーション開始
        enemyController.PlaySkillAnimation();

        chargeAttack.StartHitCheck(enemyController.AttackPower, enemyController);

        // 敵AIのときだけ NavMeshAgent の移動を止める
        // （突進はこのコルーチンが自前で動かす）
        bool aiControlled = !enemyController.IsHijacked
                            && agent != null
                            && agent.enabled
                            && agent.isOnNavMesh;

        if (aiControlled) agent.isStopped = true;

        yield return Charge(aiControlled);

        // 判定を必ず閉じる
        chargeAttack.StopHitCheck();

        if (aiControlled) agent.isStopped = false;

        ResetCooldown();

        isUsingSkill = false;
    }

    /// <summary>
    /// 前方へ chargeDistance ぶん突進する。
    ///
    /// 乗っ取り中は EnemyController ではなくプレイヤーが実体なので、
    /// GetControlRoot() が返す方を動かす。
    /// </summary>
    private IEnumerator Charge(bool aiControlled)
    {
        Transform root = enemyController.GetControlRoot();
        if (root == null) yield break;

        Vector3 direction = root.forward;
        direction.y = 0f;

        if (direction.sqrMagnitude < 0.001f) yield break;
        direction.Normalize();

        // 乗っ取り中はプレイヤーの CharacterController 越しに動かす
        CharacterController cc = root.GetComponent<CharacterController>();

        // 突進中は入力移動・回転を止める。
        // 止めないと ApplyMovement が同じフレームでもう一度 CC.Move して
        // 向きだけ入力方向に回り、突進方向とズレる。
        PlayerStateMachine machine = root.GetComponent<PlayerStateMachine>();
        if (machine != null) machine.ExternalMotion = true;

        try
        {
            float travelled = 0f;

            while (travelled < chargeDistance)
            {
                float step = Mathf.Min(chargeSpeed * Time.deltaTime, chargeDistance - travelled);
                Vector3 delta = direction * step;

                if (cc != null && cc.enabled)  cc.Move(delta);        // プレイヤー
                else if (aiControlled)         agent.Move(delta);     // NavMesh 上を維持したまま移動
                else                           root.position += delta;

                travelled += step;
                yield return null;
            }
        }
        finally
        {
            // 途中で中断されても必ず解除する
            if (machine != null) machine.ExternalMotion = false;
        }
    }
}