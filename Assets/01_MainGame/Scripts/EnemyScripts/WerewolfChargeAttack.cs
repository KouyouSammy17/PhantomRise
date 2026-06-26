using System;
using Unity.VisualScripting.Antlr3.Runtime.Tree;
using UnityEngine;

public class WerewolfChargeAttack : MonoBehaviour
{
    [SerializeField] private float chargeSpeed = 20f;
    [SerializeField] private float chargeDistance = 3f;
    [SerializeField] private float bleedDuration = 8f;
    [SerializeField] private float hitRadius = 0.5f;
    [SerializeField] private float hitDistance = 0.7f;


    private bool isCharging = false;
    private float movedDistance;

    private int damage;
    private EnemyController owner;

    private Vector3 startPosition;

    private Transform controlRoot;

    private bool hasHit = false;

    private Rigidbody rb;

    private CharacterController cc;


    public void StartCharge(int attackDamage, EnemyController enemy)
    {
        damage = attackDamage;
        owner = enemy;
        controlRoot = enemy.GetControlRoot();
        rb = controlRoot.GetComponent<Rigidbody>();
        cc = controlRoot.GetComponent<CharacterController>();
        startPosition = controlRoot.position; // 突進開始位置を記録
        hasHit = false;
        isCharging = true;
    }

    private void Update()
    {

        if (!isCharging)
            return;


        if (owner.IsStunned)
        {
            StopCharge();
            return;
        }


        // ─────────────────────────────
        // ① 当たり判定
        // ─────────────────────────────
        if (!hasHit &&
            Physics.SphereCast(
            controlRoot.position,
            hitRadius,
            controlRoot.forward,
            out RaycastHit hit,
            hitDistance))
          {
           //hasHit = true;

            Debug.Log("Hit object = " + hit.collider.name);

            // Player取得（最重要：ここで状態を見る）
            PlayerStateMachine player =
                hit.collider.GetComponentInParent<PlayerStateMachine>();

            if (player != null)
            {
                // ─────────────────────────────
                // ② ゴースト状態の時
                // ─────────────────────────────
                if (player.CurrentStateName == nameof(GhostState))
                {
                    hasHit = true;
                    //ゴースト状態の時に当たった場合は即死扱いにする
                    player.TransitionTo(player.Dead); 
                    StopCharge();
                    return;
                }

                // ─────────────────────────────
                // ③ 通常ダメージ
                // ─────────────────────────────
                player.PlayerHP.TakeDamage(damage);
                player.PlayerHP.ApplyBleed(bleedDuration);
                hasHit = true;
                StopCharge();
                return;
            }

            // ─────────────────────────────
            // ④ 敵に当たった場合
            // ─────────────────────────────
            EnemyHealth enemyHP =
                hit.collider.GetComponentInParent<EnemyHealth>();

            if (enemyHP != null)
            {
                hasHit = true;
                enemyHP.TakeDamage(damage);
                enemyHP.ApplyBleed(bleedDuration);
                Debug.Log("敵に当たった");
                StopCharge();
                return;
            }
         }


        //前方向に移動
        if (rb != null)
        {
            rb.linearVelocity =
                controlRoot.forward * chargeSpeed;
        }
        else if (cc != null)
        {
            cc.Move(
                controlRoot.forward *
                chargeSpeed *
                Time.deltaTime);
        }


        float distance =
            Vector3.Distance(startPosition, controlRoot.position);

        if (distance >= chargeDistance)
        {
            StopCharge();
            Debug.Log("突進停止");
        }

}


    private void StopCharge()
    {
        isCharging = false;
        if (rb != null)
            rb.linearVelocity = Vector3.zero;
    }

   
}