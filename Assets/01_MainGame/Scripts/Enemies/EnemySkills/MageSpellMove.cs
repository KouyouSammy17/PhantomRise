using UnityEngine;

public class MageSpellMove : MonoBehaviour
{
    [SerializeField] private float speed = 10f;
    private Rigidbody rb;

    /// <summary>MageEnemySkill から生成時に設定されるダメージ量</summary>
    [SerializeField] private int spelldamage = 10;

    // 生成直後に自分自身に当たらないようにする猶予時間（秒）
    [SerializeField] private float spawnGrace = 0.15f;
    private float _graceTimer = 0f;

    // 二重ヒット防止フラグ
    private bool _hasHit = false;

    // この魔法を撃った敵
    private EnemyController owner;

    [SerializeField] private float rotateSpeed = 5f;

    // 追尾するターゲット
    private Transform target;

    public void SetOwner(EnemyController enemy)
    {
        owner = enemy;
    }

    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
    }

    private void Start()
    {
        rb = GetComponent<Rigidbody>();

        Invoke(nameof(Delete), 4f);
    }

    private void Update()
    {
        // 発射直後の猶予時間
        if (_graceTimer < spawnGrace)
        {
            _graceTimer += Time.deltaTime;
        }

        // ==========================================
        // ホーミング
        // ==========================================
        if (target != null)
        {
            Vector3 dir =
                (target.position - transform.position).normalized;

            // ターゲット方向を少しずつ向く
            Quaternion targetRotation =
                Quaternion.LookRotation(dir);

            transform.rotation =
                Quaternion.Slerp(
                    transform.rotation,
                    targetRotation,
                    rotateSpeed * Time.deltaTime);

            // 前進
            if (rb != null)
            {
                rb.linearVelocity =
                    transform.forward * speed;
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // 発射直後はヒット判定しない
        if (_graceTimer < spawnGrace)
            return;

        HandleHit(other.gameObject);
    }

    private void OnTriggerStay(Collider other)
    {
        // 発射直後はヒット判定しない
        if (_graceTimer < spawnGrace)
            return;

        HandleHit(other.gameObject);
    }

    private void HandleHit(GameObject hitObject)
    {
        if (_hasHit)
            return;

        // ==========================================
        // プレイヤーに当たった場合
        // ==========================================
        if (hitObject.CompareTag("Player"))
        {
            // 乗っ取り中の魔法使いが撃った場合、
            // プレイヤー自身には当てない
            if (owner != null && owner.IsHijacked)
                return;

            _hasHit = true;

            PlayerStateMachine machine =
                hitObject.GetComponentInParent<PlayerStateMachine>();

            if (machine != null &&
                machine.CurrentStateName == nameof(HijackedState))
            {
                machine.PlayerHP?.TakeDamage(spelldamage);

                Debug.Log(
                    $"[MageSpell] 乗っ取り中プレイヤーに {spelldamage} ダメージ"
                );
            }

            Destroy(gameObject);
            return;
        }

        // ==========================================
        // 敵に当たった場合
        // ==========================================
        if (hitObject.CompareTag("Enemy"))
        {
            // 乗っ取り中の敵だけ、他の敵を攻撃できる
            if (owner == null || !owner.IsHijacked)
                return;

            EnemyController enemy =
                hitObject.GetComponentInParent<EnemyController>();

            if (enemy == null)
                return;

            // ★ 魔法を撃った本人には絶対に当てない
            if (enemy == owner)
            {
                Debug.Log(
                    "[MageSpell] 発射した魔法使い自身なので無視"
                );

                return;
            }

            // 乗っ取り中の敵には当てない
            if (enemy.IsHijacked)
                return;

            _hasHit = true;

            enemy.TakeDamage(spelldamage);

            Debug.Log(
                $"[MageSpell] {enemy.name} に {spelldamage} ダメージ"
            );

            Destroy(gameObject);
            return;
        }

        // ==========================================
        // その他のオブジェクト
        // ==========================================
        // PlayerでもEnemyでもないものに当たった場合は
        // 障害物として扱って魔法を消す
        _hasHit = true;

        Debug.Log(
            $"[MageSpell] 障害物に当たったため消滅: {hitObject.name}"
        );

        Destroy(gameObject);
    }

    private void Delete()
    {
        Destroy(gameObject);
    }
}

