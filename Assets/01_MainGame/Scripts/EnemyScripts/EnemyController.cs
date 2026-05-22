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

    [Header("敵ステータス")]
    // Unityのインスペクターで選べるようにする変数
    public EnemyRank rank = EnemyRank.C;

   
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
    private EnemyVision enemyVision;
    private EnemyHealth enemyHealth;
    private EnemySkillBase enemySkill;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

        enemyVision=GetComponent<EnemyVision>();
        enemyHealth=GetComponent<EnemyHealth>();
        enemySkill = GetComponent<EnemySkillBase>();

        agent = GetComponent<NavMeshAgent>();
        currentState= EnemyState.Patrol;

        currentPatrolIndex = Random.Range(0, patrolPoints.Length);

        //スキルの後に攻撃する
        attackTimer=attackCooldown;

    }

    // Update is called once per frame
    void Update()
    {
     
       
        float distance= Vector3.Distance(transform.position,player.transform.position);
        switch (currentState)
        {
            case EnemyState.Patrol:
                PatrolMode();
                
                if (enemyVision.CanSeePlayer())
                    currentState = EnemyState.Chase;
                break;

            case EnemyState.Chase:
                ChaseMode();
                if (distance < attackRange)
                    currentState = EnemyState.Attack;
                else if (!enemyVision.CanSeePlayer())
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
        if (enemyHealth.CurrentHP <= 0 && currentState != EnemyState.Die&& rank == EnemyRank.D )
        {
            currentState = EnemyState.Die;
        }

        //敵のランクがC以上の場合はHPが10％以下になると一回だけスタン状態になる
        //スタン状態が終わった後に攻撃を食らうと死ぬ
        if (rank != EnemyRank.D && (float)enemyHealth.CurrentHP / enemyHealth.maxHP <= 0.1f)
        {
            // スタン状態の処理
            if (isStunned == true)
            {
                currentState = EnemyState.Stun;
            }

            if (isStunned == false&&enemyHealth.CurrentHP<=0)
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
        if (enemySkill != null)
        {
            bool usedSkill = enemySkill.TryUseSkill();

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
            Debug.Log("通常攻撃！");

            attackTimer = attackCooldown;
        }
    }

}
