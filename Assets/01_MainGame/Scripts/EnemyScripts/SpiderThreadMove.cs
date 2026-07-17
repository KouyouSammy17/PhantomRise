using Unity.VisualScripting;
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

    //敵が敵に攻撃してしまわないようにするためのオーナー情報
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



    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.linearVelocity = transform.forward * speed;
        Invoke("Delete", 2f);
    }

    void Update()
    {
        if (_graceTimer < spawnGrace)
            _graceTimer += Time.deltaTime;
    }

    private void OnTriggerEnter(Collider other)
    {
        Debug.Log(other.name);

        // 猶予時間中はヒット判定をスキップ（自分自身への誤ヒット防止）
        if (_graceTimer < spawnGrace) return;

        HandleHit(other.gameObject);

       // Debug.Log("当たった");
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
                // 6秒間90%減速
                machine.ApplySlow(0.9f, 6f);
                Debug.Log($"[SpiderThread] 乗っ取り中プレイヤーに {damage} ダメージ");
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
           // EnemyBuffController buffController = target.GetComponentInParent<EnemyBuffController>();
            if (enemy != null && !enemy.IsHijacked&&enemy != owner)
            {
                //敵のアイコンを表示する
                //buffController.ShowSpeedDebuff(6f);

                _hasHit = true;
                enemy.TakeDamage(damage);
                Debug.Log($"[SpiderThread] {enemy.name} に {damage} ダメージ");
                
                // 敵が蜘蛛の糸に当たった場合、移動速度を遅くする
                enemy.ApplySlow(0.9f, 6f);

                Destroy(gameObject);
            }
        }
    }

    void Delete()
    {
        Destroy(gameObject);
    }
}