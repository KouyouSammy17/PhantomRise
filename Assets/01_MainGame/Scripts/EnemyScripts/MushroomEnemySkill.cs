using UnityEngine;

public class MushroomEnemySkill : EnemySkillBase
{

    [Header("Mushroom")]
    public GameObject poisonPrefab;

    public override bool TryUseSkill()
    {
        if (!CanUseSkill())
            return false;

        Instantiate(
            poisonPrefab,
            transform.position,
            transform.rotation);

        Debug.Log("毒胞子を散布！");

        ResetCooldown();

        return true;
    }

}
