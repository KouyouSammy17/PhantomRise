using UnityEngine;
using static EnemyController;

public class SpiderEnemySkill : EnemySkillBase
{

    [Header("Spider")]
    public GameObject spiderWebPrefab;
    public Transform webSpawnPoint;

    public override bool TryUseSkill()
    {
        if (!CanUseSkill())
            return false;

        Instantiate(
            spiderWebPrefab,
            webSpawnPoint.position,
            webSpawnPoint.rotation);

        Debug.Log("蜘蛛の糸を発射！");

        ResetCooldown();

        return true;
    }

}
