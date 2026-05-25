using UnityEngine;
using UnityEngine.AI;

public class EnemyController : MonoBehaviour
{
    public Transform player;

    public NavMeshAgent agent;

    // 敵のランクを選ぶための列挙型
    public enum EnemyRank
    {
        A,
        B,
        C,
        D
    }

    [Header("モデル")]
    /// <summary>
    /// 敵のビジュアルルート（モデル親 GameObject）。
    /// Inspector で指定しない場合は Renderer を持つ最初の子を自動検索。
    /// </summary>
    public Transform VisualRoot;

    /// <summary>VisualRoot を返す。未設定なら Renderer を持つ最初の子、なければ自身。</summary>
    public Transform GetVisualRoot()
    {
        if (VisualRoot != null) return VisualRoot;
        foreach (Transform child in transform)
            if (child.GetComponentInChildren<Renderer>() != null)
                return child;
        return transform;
    }

    [Header("敵ステータス")]
    // Unityのインスペクターで選べるようにする変数
    public EnemyRank rank = EnemyRank.D;

   
    //敵の攻撃力
    public int attackPower = 10;

    //敵の追跡範囲
    //public float chaseRange = 10f;
    
    //敵の攻撃範囲
    public float attackRange = 2f;

    // 攻撃間隔とタイマー
    public float attackCooldown = 3f;
    private float attackTimer = 0f;

    //ステートで敵の行動を管理
   　    public enum EnemyState
        　{
            Patrol,
            Chase,
            Attack,
            Die,
            Stun
    　　　}

    //現在のステート
    private EnemyState currentState;

    //敵のパトロールポイント
　 public Transform[] patrolPoints;

    //現在のパトロールポイントのインデックス
    private int currentPatrolIndex = 0;


    //i秒待ってから発動するためのフラグ
    private bool isWaiting = false;

    //自分がスタンしているかどうか
    private bool isStunned = true;

    //継承する
    private EnemyVision _enemyVision;
    private EnemyHealth _enemyHealth;
    private EnemySkillBase _enemySkill;
    private PlayerStateMachine _playerMachine;  // 乗っ取りシステム用キャッシュ
    private EnemyViewCone _viewCone;            // 視野コーン表示

    // ── 乗っ取りシステム ────────────────────────
    public bool IsHijacked { get; private set; }

    /// <summary>QTE 中は true → AI を一時停止する</summary>
    public bool IsQTETarget { get; private set; }

    // HijackState から読む用
    public int MaxHP => _enemyHealth != null ? _enemyHealth.maxHP : 0;
    public int CurrentHP => _enemyHealth != null ? _enemyHealth.CurrentHP : 0;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

        _enemyVision = GetComponent<EnemyVision>();
        _enemyHealth = GetComponent<EnemyHealth>();
        _enemySkill = GetComponent<EnemySkillBase>();
        _playerMachine = player?.GetComponent<PlayerStateMachine>();
        _viewCone = GetComponentInChildren<EnemyViewCone>();

        agent = GetComponent<NavMeshAgent>();
        currentState= EnemyState.Patrol;

        currentPatrolIndex = Random.Range(0, patrolPoints.Length);

        //スキルの後に攻撃する
        attackTimer=attackCooldown;

    }

    // Update is called once per frame
    void Update()
    {
        if (IsHijacked || IsQTETarget) return;   // 乗っ取り中 or QTE 中は AI 停止

        float distance= Vector3.Distance(transform.position,player.transform.position);
        switch (currentState)
        {
            case EnemyState.Patrol:
                PatrolMode();
                
                if (_enemyVision.CanSeePlayer())
                    currentState = EnemyState.Chase;
                break;

            case EnemyState.Chase:
                ChaseMode();
                if (distance < attackRange)
                    currentState = EnemyState.Attack;
                else if (!_enemyVision.CanSeePlayer())
                    currentState = EnemyState.Patrol;
                break;

            case EnemyState.Attack:
                AttackMode();
                if (distance > attackRange)
                    currentState = EnemyState.Chase;
                break;
            case EnemyState.Die:
                // 死亡
                Debug.Log("敵が死亡しました！");
                Destroy(gameObject);
                break;
            case EnemyState.Stun:
                // スタン状態の処理
                if (rank == EnemyRank.C)
                {   
                    agent.isStopped = true;
                    Invoke("RecoverFromStun", 8f);
                    Debug.Log("敵がスタン状態になりました！"); 
                }

                if (rank == EnemyRank.B)
                {
                    agent.isStopped = true;
                    Invoke("RecoverFromStun", 5f);
                    Debug.Log("敵がスタン状態になりました！");
                }

                if (rank == EnemyRank.A)
                {
                    agent.isStopped = true;
                    Invoke("RecoverFromStun", 3f);
                    Debug.Log("敵がスタン状態になりました！");
                }
                break;
        }


        //hpが0以下になったらDieステートに遷移
        if (_enemyHealth.CurrentHP <= 0 && currentState != EnemyState.Die&& rank == EnemyRank.D )
        {
            currentState = EnemyState.Die;
        }

        //敵のランクがC以上の場合はHPが10％以下になると一回だけスタン状態になる
        //スタン状態が終わった後に攻撃を食らうと死ぬ
        if (rank != EnemyRank.D && (float)_enemyHealth.CurrentHP / _enemyHealth.maxHP <= 0.1f)
        {
            // スタン状態の処理
            if (isStunned == true)
            {
                currentState = EnemyState.Stun;
            }

            if (isStunned == false&&_enemyHealth.CurrentHP<=0)
            {
                currentState = EnemyState.Die;
            }


        }

        //スペースキーを押すとダメージを受ける（テスト用）
        //if (Input.GetKeyDown(KeyCode.Space) == true)
        //{
        //    enemyHealth.TakeDamage(5);

        //}
    }

    // ─────────────────────────────────────────
    // 乗っ取りシステム連携
    // ─────────────────────────────────────────

    /// <summary>QTE 成功時に HijackState から呼ぶ</summary>
    public void BecomeHijacked()
    {
        IsQTETarget = false;   // QTE フリーズを解除（IsHijacked で完全停止に移行）
        IsHijacked = true;
        agent.isStopped = true;
        var col = GetComponent<Collider>();
        if (col) col.enabled = false;
        if (_viewCone != null) _viewCone.gameObject.SetActive(false);
        Debug.Log($"[Enemy] {name} 乗っ取られた");
    }

    /// <summary>QTE 失敗時 — フリーズ解除して Chase 状態にする</summary>
    public void AlertChase()
    {
        if (IsHijacked) return;
        IsQTETarget = false;
        currentState = EnemyState.Chase;
        agent.isStopped = false;
        if (_viewCone != null) _viewCone.gameObject.SetActive(true);
        Debug.Log($"[Enemy] {name} プレイヤーを発見！");
    }

    /// <summary>QTE 開始時に HijackState.Enter() から呼ぶ — AI を一時停止</summary>
    public void FreezeForQTE()
    {
        IsQTETarget = true;
        agent.isStopped = true;
        Debug.Log($"[Enemy] {name} QTE 中フリーズ");
    }

    /// <summary>乗っ取り中に攻撃ボタンが押されたとき — 範囲内の他の敵にダメージ</summary>
    public void PerformAttack()
    {
        Debug.Log($"[Enemy] {name} 攻撃！");
        Collider[] hits = Physics.OverlapSphere(transform.position, attackRange);
        foreach (Collider col in hits)
        {
            EnemyController other = col.GetComponentInParent<EnemyController>();
            if (other == null || other == this) continue;
            other.TakeDamage(attackPower);
        }
    }

    /// <summary>他の敵から攻撃されたとき（乗っ取り攻撃など）</summary>
    public void TakeDamage(int damage)
    {
        _enemyHealth?.TakeDamage(damage);
    }

    /// <summary>乗っ取り中に HP が 0 になったとき HijackedState から呼ぶ</summary>
    public void OnHijackedEnemyDied()
    {
        Debug.Log($"[Enemy] {name} 乗っ取り中に死亡");
        Destroy(gameObject);
    }

    /// <summary>乗っ取り中にスキルボタンが押されたとき</summary>
    public void PerformSkill()
    {
        if (_enemySkill == null) return;
        _enemySkill.TryUseSkill();
        Debug.Log($"[Enemy] {name} スキル発動！");
    }


    // ─────────────────────────────────────────
    // AI モード
    // ─────────────────────────────────────────

    void RecoverFromStun()
    {
        if (currentState == EnemyState.Stun)
        {
            isStunned = false;
            currentState = EnemyState.Patrol; // スタン状態からパトロール状態に戻る
            Debug.Log("敵がスタン状態から回復しました！");
        }
    }


    void PatrolMode()
    {
        agent.isStopped = false;
        // 巡回ロジック
        agent.SetDestination(patrolPoints[currentPatrolIndex].position);

        //目的地の0.5f以内に到達したら1秒待ってから次のポイントへ
        if (!agent.pathPending && agent.remainingDistance < 0.5f)
        {
            Invoke("SetNextPatrolPoint", 1f);
            isWaiting = true;
        }
    }

    void SetNextPatrolPoint()
    {
        if (isWaiting == true)
        {
            isWaiting = false;
            currentPatrolIndex = Random.Range(0, patrolPoints.Length);
        }
    }

    void ChaseMode()
    {
        agent.isStopped = false;
        //プレイヤーを追跡するロジック
        agent.SetDestination(player.position);
    }

    /// <summary>
    /// 通常攻撃がヒットしたときプレイヤーへダメージを与える。
    /// 乗っ取り中は PlayerHP を削り、幽霊状態なら即死させる。
    /// </summary>
    void DealDamageToPlayer()
    {
        if (_playerMachine == null) return;

        string state = _playerMachine.CurrentStateName;
        if (state == nameof(HijackedState))
        {
            // 乗っ取り中ボディに蓄積ダメージ → HP 0 で Ghost に戻る
            _playerMachine.PlayerHP?.TakeDamage(attackPower);
        }
        else if (state == nameof(GhostState))
        {
            // 幽霊は攻撃を受けると即 Dead に遷移
            _playerMachine.Ghost.OnHit();
        }
    }

    void AttackMode()
    {
        agent.isStopped = true;

        Vector3 direction =
            (player.position - transform.position).normalized;

        direction.y = 0;

        if (direction != Vector3.zero)
        {
            transform.rotation =
                Quaternion.LookRotation(direction);
        }

        // スキル使用
        if (_enemySkill != null)
        {
            bool usedSkill = _enemySkill.TryUseSkill();

            // スキルを使ったら通常攻撃しない
            if (usedSkill)
            {
                attackTimer = attackCooldown;
                return;
            }
        }

        // 通常攻撃
        attackTimer -= Time.deltaTime;

        if (attackTimer <= 0f)
        {
            attackTimer = attackCooldown;
            Debug.Log($"[Enemy] {name} 通常攻撃！");
            DealDamageToPlayer();
        }
    }

}
