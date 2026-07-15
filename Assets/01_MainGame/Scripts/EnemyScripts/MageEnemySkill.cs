using DG.Tweening.Core.Easing;
using UnityEngine;
using static EnemyController;

public class MageEnemySkill : EnemySkillBase
{

    [Header("Mage")]
    [SerializeField] private GameObject mageSpellPrefab;
    [SerializeField] private Transform spellSpawnPoint;

    public override bool TryUseSkill()
    {
        if (!CanUseSkill())
            return false;


        enemyController.PlaySkillAnimation();


        // 乗っ取り中はプレイヤーの位置・向きで発射する（通常時は spellSpawnPoint を使用）
        //Vector3    spawnPos = enemyController.IsHijacked
        //    ? enemyController.GetAttackOrigin()
        //    : spellSpawnPoint.position;
        Vector3 spawnPos=spellSpawnPoint.position;

        Quaternion spawnRot = enemyController.IsHijacked
            ? enemyController.GetAttackRotation()
            : spellSpawnPoint.rotation;

        GameObject obj = Instantiate(mageSpellPrefab, spawnPos, spawnRot);

        // 魔法にダメージ値を渡す
        MageSpellMove spell = obj.GetComponent<MageSpellMove>();

        //spell.SetOwner(GetComponent<EnemyController>()); // オーナー設定

      
        if (spell != null)
        {
            spell.SetOwner(enemyController);
            spell.Damage = enemyController.AttackPower;

            Transform target = null;

            // ===== 通常時 → プレイヤー追尾 =====
            if (!enemyController.IsHijacked)
            {
                PlayerStateMachine player =
                    FindAnyObjectByType<PlayerStateMachine>();

                if (player != null)
                    target = player.transform;
            }

            // ===== 乗っ取り時 → 一番近い敵追尾 =====
            else
            {
                EnemyController[] enemies =
                FindObjectsByType<EnemyController>();
                float closest = Mathf.Infinity;

                foreach (EnemyController enemy in enemies)
                {
                    if (enemy == enemyController)
                        continue;

                    if (enemy.IsHijacked)
                        continue;

                    float dist = Vector3.Distance(
                        enemyController.transform.position,
                        enemy.transform.position);

                    if (dist < closest)
                    {
                        closest = dist;
                        target = enemy.transform;
                    }
                }
            }

            spell.SetTarget(target);
        }



        Debug.Log("魔法の弾を発射！");

        ResetCooldown();

        return true;
    }

}
