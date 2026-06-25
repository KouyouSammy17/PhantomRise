using System.Collections;
using UnityEngine;

public class WerewolfEnemySkill : EnemySkillBase
{
    [SerializeField] private float chargeDelay = 1f;

    private WerewolfChargeAttack chargeAttack;

    private bool isUsingSkill = false;

    private void Awake()
    {
        chargeAttack =
            GetComponent<WerewolfChargeAttack>();
    }

    public override bool TryUseSkill()
    {
        if (!CanUseSkill())
            return false;

        if (isUsingSkill)
            return false;


        StartCoroutine(ChargeAttack());

        return true;
    }

    private IEnumerator ChargeAttack()
    {

        isUsingSkill = true;
        Debug.Log("人狼が溜め開始");

        yield return new WaitForSeconds(chargeDelay);

        // 本体突進開始
        chargeAttack.StartCharge(
            enemyController.AttackPower,
            enemyController);

        Debug.Log("人狼突進");

        ResetCooldown();

        isUsingSkill = false;
    }
}