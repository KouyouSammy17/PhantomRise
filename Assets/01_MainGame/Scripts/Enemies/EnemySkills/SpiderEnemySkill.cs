using DG.Tweening.Core.Easing;
using System.Collections;
using UnityEngine;
using static EnemyController;


public class SpiderEnemySkill : EnemySkillBase
{

    [Header("Spider")]
    [SerializeField] private GameObject spiderWebPrefab;
    [SerializeField] private Transform webSpawnPoint;
    private float spawnDelay = 0.3f;   // 発射までの遅延

    public override bool TryUseSkill()
    {
        if (!CanUseSkill())
            return false;

        StartCoroutine(SkillRoutine());

        ResetCooldown();
        return true;
    }

    private IEnumerator SkillRoutine()
    {
        // 先にアニメーション
        enemyController.PlaySkillAnimation();

        // 少し待つ
        yield return new WaitForSeconds(spawnDelay);

        FireWeb();
    }

    private void FireWeb()
    {
        Vector3 spawnPos = enemyController.IsHijacked
            ? enemyController.GetAttackOrigin()
            : webSpawnPoint.position;

        Quaternion spawnRot = enemyController.IsHijacked
            ? enemyController.GetAttackRotation()
            : webSpawnPoint.rotation;

        GameObject obj =
            Instantiate(spiderWebPrefab, spawnPos, spawnRot);

        SpiderThreadMove thread =
            obj.GetComponent<SpiderThreadMove>();

        if (thread != null)
        {
            thread.SetOwner(GetComponent<EnemyController>());
            thread.Damage = enemyController.AttackPower;
        }

        Debug.Log("蜘蛛の糸を発射！");
    }

}
