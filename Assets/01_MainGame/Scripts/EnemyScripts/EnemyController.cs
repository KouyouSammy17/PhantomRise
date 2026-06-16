using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class EnemyController : MonoBehaviour
{
    [SerializeField] private Transform player;

    [SerializeField] private NavMeshAgent agent;

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
    [SerializeField] private Transform VisualRoot;

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
    [SerializeField] private  EnemyRank rank = EnemyRank.D;


    //敵の攻撃力
    [SerializeField] private int attackPower = 10;

    //敵の攻撃範囲
    [SerializeField] private float attackRange = 2f;

   
    // 攻撃間隔とタイマー
    [SerializeField] private float attackCooldown = 3f;
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
　 [SerializeField] private Transform[] patrolPoints;

    //現在のパトロールポイントのインデックス
    private int currentPatrolIndex = 0;


    //i秒待ってから発動するためのフラグ
    private bool isWaiting = false;

    //自分がスタンしているかどうか
    private bool isStunned = true;

    //敵がプレイヤーを見失ってからパトロールに戻るまでの時間を計測するタイマー
    private float lostSightTimer = 0f;
    [SerializeField] private float lostSightDuration = 3f;

    //ダメージを受けたら視界無視でプレイヤーを追いかけるためのフラグ
    private bool alertedByDamage = false;
    private float alertTimer = 0f;
    [SerializeField] private float alertDuration = 5f;

    //継承する
    private EnemyVision _enemyVision;
    private EnemyHealth _enemyHealth;
    private EnemySkillBase _enemySkill;
    private PlayerStateMachine _playerMachine;  // 乗っ取りシステム用キャッシュ
    private EnemyViewCone _viewCone;            // 視野コーン表示
    private EnemyHPbar _enemyHPbar;            // 敵 HP バー
    private BossSkill _bossSkill;                    // ボス専用スキル


    // 外から読み取りだけ可能
    public int AttackPower { 
        get {return attackPower; } 
    }

    public EnemyRank Rank
    {
        get { return rank; }
    }


    // ── 乗っ取りシステム ────────────────────────
    public bool IsHijacked { get; private set; }

    /// <summary>QTE 中は true → AI を一時停止する</summary>
    public bool IsQTETarget { get; private set; }

    // HijackState から読む用
    public int MaxHP => _enemyHealth != null ? _enemyHealth.maxHP : 0;
    public int CurrentHP => _enemyHealth != null ? _enemyHealth.CurrentHP : 0;

    /// <summary>現在スタン状態かどうか（C ランク以上の乗っ取り判定に使用）</summary>
    public bool IsStunned => currentState == EnemyState.Stun;

    //蜘蛛の糸に当たった時に移動速度を遅くするためのコルーチン
    private Coroutine slowCoroutine;
    //元の移動速度を保存する変数
    private float originalSpeed;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    protected virtual void Start()
    {

        _enemyVision = GetComponent<EnemyVision>();
        _enemyHealth = GetComponent<EnemyHealth>();
        _enemySkill = GetComponent<EnemySkillBase>();
        _playerMachine = player?.GetComponent<PlayerStateMachine>();
        _viewCone   = GetComponentInChildren<EnemyViewCone>();
        _enemyHPbar = GetComponentInChildren<EnemyHPbar>();
        _bossSkill = GetComponent<BossSkill>();

        agent = GetComponent<NavMeshAgent>();
        currentState= EnemyState.Patrol;

        currentPatrolIndex = Random.Range(0, patrolPoints.Length);

        //スキルの後に攻撃する
        attackTimer=attackCooldown;

        originalSpeed = agent.speed;

    }

    // Update is called once per frame
     protected virtual void Update()
    {
        

        // ダメージを受けたら一定時間プレイヤーを追いかける
        if (alertedByDamage)
        {
            alertTimer -= Time.deltaTime;

            if (alertTimer <= 0f)
            {
                alertedByDamage = false;
            }
        }


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

                // スキル距離に入ったらスキル
                if (distance <= _enemySkill.SkillRange)
                {
                    if (_enemySkill != null)
                    {
                        _enemySkill.TryUseSkill();
                    }
                }


                if (distance < attackRange)
                    currentState = EnemyState.Attack;
                //else if (!_enemyVision.CanSeePlayer())
                //    currentState = EnemyState.Patrol;
                if (!alertedByDamage)
                {
                    if (_enemyVision.CanSeePlayer())
                    {
                        lostSightTimer = 0f;
                    }
                    else
                    {
                        lostSightTimer += Time.deltaTime;

                        if (lostSightTimer >= lostSightDuration)
                        {
                            currentState = EnemyState.Patrol;
                        }
                    }
                }
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
                Debug.Log("現在スタン中");
                agent.isStopped = true;
                break;
        }


        //hpが0以下になったらDieステートに遷移
        if (_enemyHealth.CurrentHP <= 0 && currentState != EnemyState.Die&& rank == EnemyRank.D)
        {
            currentState = EnemyState.Die;
        }



        //敵のランクがC以上の場合はHPが10％以下になると一回だけスタン状態になる
        //スタン状態が終わった後に攻撃を食らうと死ぬ
        if (rank != EnemyRank.D && (float)_enemyHealth.CurrentHP / _enemyHealth.maxHP <= 0.5f&&CanStun())
        {

            // スタン状態の処理
            if (isStunned == true)
            {
                isStunned = false;
                currentState = EnemyState.Stun;

                float stunTime = 0f;
                if (rank == EnemyRank.C)
                    stunTime = 8f;
                else if (rank == EnemyRank.B)
                    stunTime = 5f;
                else if (rank == EnemyRank.A)
                    stunTime = 3f;
                Invoke(nameof(RecoverFromStun), stunTime);
                //スタンに入るときは1秒無敵になる
                StartCoroutine(_enemyHealth.InvincibleTime(1f));
            }

            if (_enemyHealth.Invincible == false && _enemyHealth.CurrentHP <= 0)
            {
                currentState = EnemyState.Die;
            }

            if (rank == EnemyRank.A && _enemyHealth.CurrentHP <= 0)
            {
                // クリア判定
                GameManager.Instance.TriggerGameClear();
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
        agent.enabled = false;   // NavMeshAgent を完全無効化
        var col = GetComponent<Collider>();
        if (col) col.enabled = false;
        if (_viewCone   != null) _viewCone.gameObject.SetActive(false);
        if (_enemyHPbar != null) _enemyHPbar.gameObject.SetActive(false);
        Debug.Log($"[Enemy] {name} 乗っ取られた");
    }

    /// <summary>QTE 失敗時 — フリーズ解除して Chase 状態にする</summary>
    public void AlertChase()
    {
        if (IsHijacked) return;
        IsQTETarget = false;
        currentState = EnemyState.Chase;
        agent.enabled = true;    // NavMeshAgent を再有効化
        agent.isStopped = false;
        if (_viewCone   != null) _viewCone.gameObject.SetActive(true);
        if (_enemyHPbar != null) _enemyHPbar.gameObject.SetActive(true);
        Debug.Log($"[Enemy] {name} プレイヤーを発見！");
    }

    /// <summary>QTE 開始時に HijackState.Enter() から呼ぶ — AI を一時停止</summary>
    public void FreezeForQTE()
    {
        IsQTETarget = true;
        agent.isStopped = true;
        Debug.Log($"[Enemy] {name} QTE 中フリーズ");
    }

    // ─────────────────────────────────────────
    // スキル共通ユーティリティ
    // ─────────────────────────────────────────

    /// <summary>スキル・攻撃の発生位置（乗っ取り中はプレイヤーの現在位置）</summary>
    public Vector3 GetAttackOrigin()
    {
        return (IsHijacked && _playerMachine != null)
            ? _playerMachine.transform.position
            : transform.position;
    }

    /// <summary>スキル・攻撃の向き（乗っ取り中はプレイヤーの向き）</summary>
    public Quaternion GetAttackRotation()
    {
        return (IsHijacked && _playerMachine != null)
            ? _playerMachine.transform.rotation
            : transform.rotation;
    }

    /// <summary>乗っ取り中に攻撃ボタンが押されたとき — 範囲内の他の敵にダメージ</summary>
    public void PerformAttack()
    {
    
        // 乗っ取り中はプレイヤーの現在位置を基点にする（EnemyController 自体は動かない）
        Vector3 origin = (IsHijacked && _playerMachine != null)
            ? _playerMachine.transform.position
            : transform.position;

        Debug.Log($"[Enemy] {name} 攻撃！ origin={origin}");
        Collider[] hits = Physics.OverlapSphere(origin, attackRange);
        foreach (Collider col in hits)
        {
            EnemyController other = col.GetComponentInParent<EnemyController>();
            if (other == null || other == this) continue;
            other.TakeDamage(attackPower);
            Debug.Log($"[Enemy] {name} → {other.name} に {attackPower} ダメージ");
        }
    }

    /// <summary>他の敵から攻撃されたとき（乗っ取り攻撃など）</summary>
    public virtual void TakeDamage(int damage)
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
            //isStunned = false;
            currentState = EnemyState.Patrol; // スタン状態からパトロール状態に戻る
            Debug.Log("敵がスタン状態から回復しました！");
        }
    }


    protected  virtual void PatrolMode()
    {
        agent.isStopped = false;

        agent.SetDestination(
            patrolPoints[currentPatrolIndex].position);

        if (!isWaiting &&
            !agent.pathPending &&
            agent.remainingDistance < 0.5f)
        {
            isWaiting = true;

            Invoke(
                nameof(SetNextPatrolPoint),
                1f);
        }
    }

    void SetNextPatrolPoint()
    {
        currentPatrolIndex =
         Random.Range(
             0,
             patrolPoints.Length);

        isWaiting = false;
    }

    void ChaseMode()
    {
        // ボスがスキル使用中は追跡を一時停止
        if (_bossSkill != null)
        {
            if (_bossSkill.IsUsingSkill == false)
            {
                agent.isStopped = false;
            }
        }
        else
        {
            agent.isStopped = false;
        }
        
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

    /// <summary>
    /// ダメージを受けたら Chase 状態に移行してプレイヤーを追いかける。
    /// </summary>
    public void AlertDamage()
    {
        if (currentState == EnemyState.Die)
            return;
        if (currentState == EnemyState.Stun)
            return;

        alertedByDamage = true;
        alertTimer = alertDuration;

        currentState = EnemyState.Chase;

        Debug.Log($"[Enemy] {name} ダメージを受けたので追跡開始");
    }


    //
    /// <summary>
    /// 外部（スタントラップなど）からスタンを適用する。
    /// 既存のスタン回復タイマーをキャンセルして上書きする。
    /// </summary>
    public void ApplyStun(float duration)
    {
        if (currentState == EnemyState.Die || IsHijacked) return;

        // 既存の回復タイマーをキャンセル
        CancelInvoke(nameof(RecoverFromStun));

        currentState = EnemyState.Stun;
        agent.isStopped = true;

        Invoke(nameof(RecoverFromStun), duration);
        Debug.Log($"[StunTrap] {name} がスタン（{duration}秒）");
    }

    public void ApplySlow(float slowPercent, float duration)
    {
        if (slowCoroutine != null)
        {
            StopCoroutine(slowCoroutine);
        }

        slowCoroutine = StartCoroutine(
            SlowCoroutine(slowPercent, duration));
    }


    private IEnumerator SlowCoroutine(
        float slowPercent,
        float duration)
    {
        agent.speed = originalSpeed * (1f - slowPercent);

        Debug.Log(
            $"{name} の移動速度が {(int)(slowPercent * 100)}% 低下");

        yield return new WaitForSeconds(duration);

        agent.speed = originalSpeed;

        Debug.Log($"{name} の移動速度が元に戻った");

        slowCoroutine = null;
    }


    protected virtual bool CanStun()
    {
        return true;   // デフォルトはスタン不可。BossController でオーバーライドしてスタン可能にする。
    }
}
