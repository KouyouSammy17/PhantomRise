using UnityEngine;

public class BatEnemySkill : EnemySkillBase
{
    [Header("Bat")]
    [SerializeField] private GameObject bitePrefab;
    [SerializeField] private Transform biteSpawnPoint;

    public override bool TryUseSkill()
    {
        if (!CanUseSkill())
            return false;

        Vector3 spawnPos;
        Quaternion spawnRot;

        if (enemyController.IsHijacked)
        {
            spawnRot = enemyController.GetAttackRotation();

            spawnPos =
                enemyController.GetAttackOrigin() +
                spawnRot * biteSpawnPoint.localPosition;
        }
        else
        {
            spawnPos = biteSpawnPoint.position;
            spawnRot = biteSpawnPoint.rotation;
        }

        GameObject obj = Instantiate(
            bitePrefab,
            spawnPos,
            spawnRot);

        Bloodsucking blood = obj.GetComponent<Bloodsucking>();

        if (blood != null)
        {
            blood.Damage = enemyController.AttackPower;

            PlayerStateMachine player =
                FindFirstObjectByType<PlayerStateMachine>();

            blood.Initialize(enemyController, player);
        }

        // プレイヤーとの衝突を無視
        if (enemyController.IsHijacked)
        {

            PlayerStateMachine machine =
            FindFirstObjectByType<PlayerStateMachine>();

            Collider biteCollider =
                obj.GetComponent<Collider>();

            if (machine != null && biteCollider != null)
            {
                Collider[] playerColliders =
                    machine.GetComponentsInChildren<Collider>();

                foreach (Collider col in playerColliders)
                {
                    Physics.IgnoreCollision(
                        biteCollider,
                        col);
                }
            }
        }

        Debug.Log("吸血！");

        ResetCooldown();

        return true;
    }
}