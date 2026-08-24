using UnityEngine;

public class Bloodsucking : MonoBehaviour
{
    [SerializeField] private int damage = 10;

    private EnemyController owner;
    private PlayerStateMachine ownerPlayer;

    private bool _hasHit = false;

    public int Damage
    {
        get => damage;
        set => damage = value;
    }

    public void Initialize(
        EnemyController enemy,
        PlayerStateMachine player)
    {
        owner = enemy;
        ownerPlayer = player;
    }

    private void Start()
    {
        Invoke(nameof(Delete), 1f);
    }

    private void OnTriggerEnter(Collider other)
    {
        HandleHit(other.gameObject);
    }

    private void OnTriggerStay(Collider other)
    {
        HandleHit(other.gameObject);
    }

    private void HandleHit(GameObject target)
    {
        if (_hasHit)
            return;

        // プレイヤー除外は「乗っ取り中だけ」
        if (owner != null && owner.IsHijacked)
        {
            PlayerStateMachine hitPlayer =
                target.GetComponentInParent<PlayerStateMachine>();

            if (hitPlayer != null &&
                hitPlayer == ownerPlayer)
            {
                return;
            }
        }

        // 発射した本人の敵には当たらない
        EnemyController hitEnemy =
            target.GetComponentInParent<EnemyController>();

        if (owner != null &&
            hitEnemy == owner)
        {
            return;
        }

        //------------------------------------------------
        // 敵がプレイヤーを攻撃
        //------------------------------------------------

        if (target.CompareTag("Player"))
        {


            PlayerStateMachine machine =
                target.GetComponentInParent<PlayerStateMachine>();

            Debug.Log($"Player Hit! State={machine?.CurrentStateName}");

            if (machine != null &&
                machine.CurrentStateName == nameof(HijackedState))
            {
                _hasHit = true;

                machine.PlayerHP?.TakeDamage(damage);

                if (owner != null)
                {
                    EnemyHealth hp =
                        owner.GetComponent<EnemyHealth>();

                    if (hp != null)
                    {
                        hp.Heal(damage);

                        Debug.Log(
                            $"[Bite] {owner.name} が {damage} 回復");
                    }
                }

                Destroy(gameObject);
            }
        }

        //------------------------------------------------
        // プレイヤーが乗っ取ったコウモリで敵を攻撃
        //------------------------------------------------

        else if (target.CompareTag("Enemy"))
        {
            EnemyController enemy =
                target.GetComponentInParent<EnemyController>();

            if (enemy != null &&
                !enemy.IsHijacked)
            {
                _hasHit = true;

                enemy.TakeDamage(damage);

                if (ownerPlayer != null)
                {
                    ownerPlayer.PlayerHP?.Heal(damage);

                    Debug.Log(
                        $"[Bite] プレイヤーが {damage} 回復");
                }

                Destroy(gameObject);
            }
        }
    }

    private void Delete()
    {
        Destroy(gameObject);
    }
}