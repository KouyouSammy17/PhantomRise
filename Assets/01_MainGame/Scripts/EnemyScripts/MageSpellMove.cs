using UnityEngine;

public class MageSpellMove : MonoBehaviour
{
    [SerializeField] private float speed = 10f;
    private Rigidbody rb;

    /// <summary>MageEnemySkill から生成時に設定されるダメージ量</summary>
    [SerializeField] private int damage = 10;

    // 生成直後に自分自身に当たらないようにする猶予時間（秒）
    [SerializeField] private float spawnGrace = 0.15f;
    private float _graceTimer = 0f;

    // 二重ヒット防止フラグ
    private bool _hasHit = false;

    //敵が敵に攻撃してしまわないようにするためのオーナー情報
    private EnemyController owner;

    [SerializeField] private float rotateSpeed = 5f;

    //追尾するためのtransform
    private Transform target;

    public int Damage
    {
        get => damage;
        set => damage = value;
    }

    public void SetOwner(EnemyController enemy)
    {
        owner = enemy;
    }

    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
    }



    void Start()
    {
        rb = GetComponent<Rigidbody>();
        //rb.linearVelocity = transform.forward * speed;
        Invoke("Delete", 4f);
    }

    void Update()
    {
        if (_graceTimer < spawnGrace)
            _graceTimer += Time.deltaTime;

        // ホーミング
        if (target != null)
        {
            Vector3 dir =
                (target.position - transform.position).normalized;

            // 少しずつターゲット方向を向く
            Quaternion targetRotation =
                Quaternion.LookRotation(dir);

            transform.rotation =
                Quaternion.Slerp(
                    transform.rotation,
                    targetRotation,
                    rotateSpeed * Time.deltaTime);

            // 前進
            rb.linearVelocity =
                transform.forward * speed;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // 猶予時間中はヒット判定をスキップ（自分自身への誤ヒット防止）
        if (_graceTimer < spawnGrace) return;

        HandleHit(other.gameObject);

        Debug.Log("当たった");
    }

    private void OnTriggerStay(Collider other)
    {
        HandleHit(other.gameObject);
    }


    private void HandleHit(GameObject target)
    {
        if (_hasHit) return;

        if (target.CompareTag("Player"))
        {

            // 自分（乗っ取り中）が撃った弾ならプレイヤーには当てない
            if (owner != null && owner.IsHijacked)
                return;


            _hasHit = true;

            // 乗っ取り中は PlayerHP にダメージを与える
            PlayerStateMachine machine = target.GetComponentInParent<PlayerStateMachine>();
            if (machine != null && machine.CurrentStateName == nameof(HijackedState))
            {
                machine.PlayerHP?.TakeDamage(damage);
               
                Debug.Log($"[MageSpell] 乗っ取り中プレイヤーに {damage} ダメージ");
            }
            Destroy(gameObject);
        }
        else if (target.CompareTag("Enemy"))
        {
            //敵が敵に攻撃してしまわないようにするためのオーナー情報を確認
            if (owner == null || !owner.IsHijacked)
                return;

            // 乗っ取り中にスキルを使った場合 → 他の敵にダメージ
            EnemyController enemy = target.GetComponentInParent<EnemyController>();
            if (enemy != null && !enemy.IsHijacked && enemy != owner)
            {
                _hasHit = true;
                enemy.TakeDamage(damage);
                Debug.Log($"[MageSpell] {enemy.name} に {damage} ダメージ");

              
                Destroy(gameObject);
            }
        }
    }

    void Delete()
    {
        Destroy(gameObject);
    }
}
