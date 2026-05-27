using UnityEngine;
using static EnemyController;

public class SpiderEnemySkill : EnemySkillBase
{

    [Header("Spider")]
    [SerializeField] private GameObject spiderWebPrefab;
    [SerializeField] private Transform webSpawnPoint;

    public override bool TryUseSkill()
    {
        if (!CanUseSkill())
            return false;

        // 乗っ取り中はプレイヤーの位置・向きで発射する（通常時は webSpawnPoint を使用）
        Vector3    spawnPos = enemyController.IsHijacked
            ? enemyController.GetAttackOrigin()
            : webSpawnPoint.position;
        Quaternion spawnRot = enemyController.IsHijacked
            ? enemyController.GetAttackRotation()
            : webSpawnPoint.rotation;

        GameObject obj = Instantiate(spiderWebPrefab, spawnPos, spawnRot);

        // 糸にダメージ値を渡す
        SpiderThreadMove thread = obj.GetComponent<SpiderThreadMove>();
        if (thread != null) thread.Damage = enemyController.AttackPower;

        Debug.Log("蜘蛛の糸を発射！");

        ResetCooldown();

        return true;
    }

}
