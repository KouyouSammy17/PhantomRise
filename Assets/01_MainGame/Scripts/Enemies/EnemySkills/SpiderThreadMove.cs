using UnityEngine;

public class SpiderThreadMove : MonoBehaviour
{
    private float speed = 10f;
    private Rigidbody rb;

    /// <summary>SpiderEnemySkill から生成時に設定されるダメージ量</summary>
    [SerializeField] private int damage = 10;

    // 生成直後に自分自身に当たらないようにする猶予時間（秒）
    [SerializeField] private float spawnGrace = 0f;
    private float _graceTimer = 0f;

    // 二重ヒット防止フラグ
    private bool _hasHit = false;

    // スキルを撃った敵
    private EnemyController owner;

    public int Damage
    {
        get => damage;
        set => damage = value;
    }

    public void SetOwner(EnemyController enemy)
    {
        owner = enemy;
    }

    private void Start()
    {
        rb = GetComponent<Rigidbody>();

        if (rb != null)
        {
            rb.linearVelocity = transform.forward * speed;
        }

        Invoke(nameof(Delete), 2f);
    }

    private void Update()
    {
        if (_graceTimer < spawnGrace)
        {
            _graceTimer += Time.deltaTime;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        Debug.Log($"[SpiderThread] Hit: {other.name}");

        // 猶予時間中はヒット判定をスキップ
        if (_graceTimer < spawnGrace)
            return;

        HandleHit(other.gameObject);
    }

    private void OnTriggerStay(Collider other)
    {
        if (_graceTimer < spawnGrace)
            return;

        HandleHit(other.gameObject);
    }

    private void HandleHit(GameObject target)
    {
        if (_hasHit)
            return;

        // ==========================================
        // プレイヤーに当たった場合
        // ==========================================
        if (target.CompareTag("Player"))
        {
            // 乗っ取り中の蜘蛛が撃った場合、
            // プレイヤー自身には当てない
            if (owner != null && owner.IsHijacked)
                return;

            _hasHit = true;

            PlayerStateMachine machine =
                target.GetComponentInParent<PlayerStateMachine>();

            if (machine != null &&
                machine.CurrentStateName == nameof(HijackedState))
            {
                machine.PlayerHP?.TakeDamage(damage);

                // 6秒間90%減速
                machine.ApplySlow(0.9f, 6f);

                Debug.Log(
                    $"[SpiderThread] 乗っ取り中プレイヤーに {damage} ダメージ"
                );
            }

            Destroy(gameObject);
            return;
        }

        // ==========================================
        // 敵に当たった場合
        // ==========================================
        if (target.CompareTag("Enemy"))
        {
            // 乗っ取り中の蜘蛛だけ敵を攻撃できる
            if (owner == null || !owner.IsHijacked)
                return;

            EnemyController enemy =
                target.GetComponentInParent<EnemyController>();

            if (enemy == null)
                return;

            // ★ 発射した本人には絶対に当てない
            if (enemy == owner)
            {
                Debug.Log("[SpiderThread] 発射した蜘蛛自身なので無視");
                return;
            }

            // 乗っ取り中の敵には当てない
            if (enemy.IsHijacked)
                return;

            _hasHit = true;

            enemy.TakeDamage(damage);

            Debug.Log(
                $"[SpiderThread] {enemy.name} に {damage} ダメージ"
            );

            // 敵を6秒間90%減速
            enemy.ApplySlow(0.9f, 6f);

            Destroy(gameObject);
            return;
        }

        // ==========================================
        // その他のオブジェクト
        // ==========================================
        // PlayerでもEnemyでもないものに当たった場合は
        // 障害物として扱い、蜘蛛の糸を消す
        _hasHit = true;

        Debug.Log(
            $"[SpiderThread] 障害物に当たったため消滅: {target.name}"
        );

        Destroy(gameObject);
    }

    private void Delete()
    {
        Destroy(gameObject);
    }
}

