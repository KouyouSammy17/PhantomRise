using UnityEngine;
using System.Collections;

public class BatEnemySkill : EnemySkillBase
{
    [Header("Bat")]
    [SerializeField] private GameObject bitePrefab;
    [SerializeField] private Transform biteSpawnPoint;

    private float delay = 0.5f;

    public override bool TryUseSkill()
    {
        if (!CanUseSkill())
            return false;

        enemyController.PlaySkillAnimation();

        StartCoroutine(SpawnBiteAfterDelay());

        ResetCooldown();

        return true;
    }

    private IEnumerator SpawnBiteAfterDelay()
    {
        yield return new WaitForSeconds(delay);

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
                FindAnyObjectByType<PlayerStateMachine>();

            blood.Initialize(enemyController, player);
        }

        // プレイヤーとの衝突を無視
        if (enemyController.IsHijacked)
        {
            PlayerStateMachine machine =
                FindAnyObjectByType<PlayerStateMachine>();

            Collider biteCollider = obj.GetComponent<Collider>();

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
    }
}