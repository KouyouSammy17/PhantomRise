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

        // 敵AIのときだけ停止
        if (!enemyController.IsHijacked &&
            agent.enabled &&
            agent.isOnNavMesh)
        {
            agent.isStopped = true;
        }

        yield return new WaitForSeconds(0.2f);

        if (!enemyController.IsHijacked &&
        agent.enabled &&
        agent.isOnNavMesh)
        {
            agent.isStopped = false;
        }

        ResetCooldown();

        isUsingSkill = false;
    }
}