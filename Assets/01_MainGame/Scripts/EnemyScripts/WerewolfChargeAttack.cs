using UnityEngine;
using UnityEngine.Rendering;

public class WerewolfChargeAttack : MonoBehaviour
{
    [SerializeField] private float bleedDuration = 8f;
    [SerializeField] private float hitRadius = 0.5f;
    [SerializeField] private float hitDistance = 0.7f;

    private bool checkHit = false;
   
    private int damage;
    private EnemyController owner;

    private Transform controlRoot;

    private bool hasHit = false;

    public void StartHitCheck(int attackDamage, EnemyController enemy)
    {
        damage = attackDamage;
        owner = enemy;
        controlRoot = enemy.GetControlRoot();
        hasHit = false;
        checkHit = true;
    }

    private void Update()
    {
      

        if (!checkHit)
            return;


        if (owner.IsStunned)
        {
          
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
                   
                    return;
                }

                // ─────────────────────────────
                // ③ 通常ダメージ
                // ─────────────────────────────
                player.PlayerHP.TakeDamage(damage);
                player.PlayerHP.ApplyBleed(bleedDuration);
                hasHit = true;
             
                return;
            }

            // ─────────────────────────────
            // ④ 敵に当たった場合
            // ─────────────────────────────
            EnemyController enemy =
    hit.collider.GetComponentInParent<EnemyController>();

            if (enemy != null)
            {
                // 乗っ取り中のみ敵に攻撃できる
                if (owner == null || !owner.IsHijacked)
                    return;

                // 自分には当てない
                if (enemy == owner)
                    return;

                // 乗っ取られている敵にも当てない（必要なら）
                if (enemy.IsHijacked)
                    return;

                hasHit = true;

                enemy.TakeDamage(damage);

                EnemyHealth hp = enemy.GetComponent<EnemyHealth>();
                hp?.ApplyBleed(bleedDuration);

                Debug.Log("敵に当たった");

                return;
            }
        }



    }


   
}