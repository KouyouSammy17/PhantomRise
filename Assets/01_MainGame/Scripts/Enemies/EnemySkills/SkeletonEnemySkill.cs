using System.Collections;
using UnityEngine;
using static EnemyController;

public class SkeletonEnemySkill : EnemySkillBase
{

    [Header("Skeleton")]
    [SerializeField] private GameObject SlashPrefab;
    [SerializeField] private Transform slashSpawnPoint;

    // 発射数
    [SerializeField] private int slashCount = 3;

    [SerializeField] private float slashInterval = 0.3f;

    public override bool TryUseSkill()
    {
        if (!CanUseSkill())
            return false;

        enemyController.PlaySkillAnimation();

        // 乗っ取り中はプレイヤーの位置・向きで発射する（通常時は webSpawnPoint を使用）
        Vector3    spawnPos = enemyController.IsHijacked
            ? enemyController.GetAttackOrigin()
            : slashSpawnPoint.position;
        Quaternion spawnRot = enemyController.IsHijacked
            ? enemyController.GetAttackRotation()
            : slashSpawnPoint.rotation;

        StartCoroutine(FireSlashCombo());
      
        ResetCooldown();

        return true;
    }

    // 斬撃を連続で発射するコルーチン
    private IEnumerator FireSlashCombo()
    {
        for (int i = 0; i < slashCount; i++)
        {
            // 乗っ取り中ならプレイヤー位置から発射
            Vector3 spawnPos = enemyController.IsHijacked
                ? enemyController.GetAttackOrigin()
                : slashSpawnPoint.position;

            Quaternion spawnRot = enemyController.IsHijacked
                ? enemyController.GetAttackRotation()
                : slashSpawnPoint.rotation;

            GameObject obj =
                Instantiate(SlashPrefab, spawnPos, spawnRot);

            // ダメージ設定
            SlashMove slash = obj.GetComponent<SlashMove>();

            slash.SetOwner(GetComponent<EnemyController>()); // オーナー設定

            if (slash != null)
            {
                slash.Damage = enemyController.AttackPower;
            }

            Debug.Log($"斬撃発射 {i + 1}");

            // 最後以外は待機
            if (i < slashCount - 1)
            {
                yield return new WaitForSeconds(slashInterval);
            }
        }
    }
}
