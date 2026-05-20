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

    // 敵のランクを選ぶための列挙型
    public enum EnemyName
    {
        Spider,
        Mushroom,
        Skeleton,
        Werewolf
    }

    // Unityのインスペクターで選べるようにする変数
    public EnemyName name = EnemyName.Spider;

    //敵のHP
    public int maxHP = 100;
    private int currentHP;

    //敵の攻撃力
    public int attackPower = 10;


    //敵の追跡範囲
    public float chaseRange = 10f;
    
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


    [Header("視界設定")]

    // 視野角
    public float viewAngle = 90f;

    // 障害物レイヤー
    public LayerMask obstacleMask;

    // プレイヤーレイヤー
    public LayerMask playerMask;

    //敵のスキルクールダウン
    private float skillCooldown = 5f;
    private float skillTimer = 0f;

    //最初だけスキルが溜まっていたら撃ってそのあとに攻撃するためのフラグ
    private bool hasInitialSkill = false;

    [Header("Spider")]
    public GameObject spiderWebPrefab;
    public Transform webSpawnPoint;

    [Header("Mushroom")]
    public GameObject poisonPrefab;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        currentState= EnemyState.Patrol;

        currentPatrolIndex = Random.Range(0, patrolPoints.Length);
        currentHP = maxHP;

        //敵によってスキルのクールダウンを変える
        if (name == EnemyName.Spider)
        {
            skillCooldown = 8f;
            skillTimer =8f; // 最初からスキルが溜まっている状態にする
            attackTimer = attackCooldown; // 攻撃をスキルの後にする
        }
        else if (name == EnemyName.Mushroom)
        {
            skillCooldown = 10f;
            skillTimer = 10f; // 最初からスキルが溜まっている状態にする
            attackTimer = attackCooldown; // 攻撃をスキルの後にする
        }
        else if (name == EnemyName.Skeleton)
        {
            skillCooldown = 6f;
            skillTimer = 6f; // 最初からスキルが溜まっている状態にする
            attackTimer = attackCooldown; // 攻撃をスキルの後にする
        }
        else if (name == EnemyName.Werewolf)
        {
            skillCooldown = 13f;
            skillTimer = 13f; // 最初からスキルが溜まっている状態にする
            attackTimer = attackCooldown; // 攻撃をスキルの後にする
        }

    }

    // Update is called once per frame
    void Update()
    {
      


       if (skillTimer <= skillCooldown)
        {
            skillTimer += Time.deltaTime;
        }
       
       // Debug.Log("スキルタイマー: " + skillTimer);

        float distance= Vector3.Distance(transform.position,player.transform.position);
        switch (currentState)
        {
            case EnemyState.Patrol:
                PatrolMode();
                //if (distance < chaseRange)
                //    currentState = EnemyState.Chase;
                if (CanSeePlayer())
                    currentState = EnemyState.Chase;
                break;

            case EnemyState.Chase:
                ChaseMode();
                if (distance < attackRange)
                    currentState = EnemyState.Attack;
                //else if (distance > chaseRange)
                else if (!CanSeePlayer())
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
        if (currentHP <= 0 && currentState != EnemyState.Die&& rank == EnemyRank.D )
        {
            currentState = EnemyState.Die;
        }

        //敵のランクがC以上の場合はHPが10％以下になると一回だけスタン状態になる
        //スタン状態が終わった後に攻撃を食らうと死ぬ
        if (rank != EnemyRank.D && (float)currentHP / maxHP <= 0.1f)
        {
            // スタン状態の処理
            if (isStunned == true)
            {
                currentState = EnemyState.Stun;
            }

            if (isStunned == false&&currentHP<=0)
            {
                currentState = EnemyState.Die;
            }


        }

        //スペースキーを押すとダメージを受ける（テスト用）
        if (Input.GetKeyDown(KeyCode.Space) == true)
        {
            TakeDamage(5);
          
        }
      

    }


    bool CanSeePlayer()
    {
        Vector3 eyePosition = transform.position + Vector3.up * 1.5f;

        Vector3 dirToPlayer = (player.position - eyePosition).normalized;

        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        // 距離判定
        if (distanceToPlayer > chaseRange)
            return false;

        // 角度判定
        float angle = Vector3.Angle(transform.forward, dirToPlayer);

        if (angle > viewAngle / 2f)
            return false;

        // プレイヤーまでRayを飛ばす
        Ray ray = new Ray(eyePosition, dirToPlayer);

        // 壁またはプレイヤーにだけ当たる
        LayerMask combinedMask = obstacleMask | playerMask;

        if (Physics.Raycast(ray, out RaycastHit hit, chaseRange, combinedMask))
        {
            // 最初に当たったのがプレイヤーなら視認成功
            if (((1 << hit.collider.gameObject.layer) & playerMask) != 0)
            {
                return true;
            }
        }

        return false;
    }

    void TakeDamage(int damage)
    {
        currentHP -= damage;
        Debug.Log("敵がダメージを受けました！現在のHP: " + currentHP);
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
        // プレイヤーの手前で止まる
        agent.isStopped = true;

        // プレイヤーの方向を向かせる
        Vector3 direction = (player.position - transform.position).normalized;
        direction.y = 0;
        if (direction != Vector3.zero)
        {
            transform.rotation = Quaternion.LookRotation(direction);
        }

        // タイマーを進めて、0以下になったら攻撃
        if (hasInitialSkill == true)
        {
            attackTimer -= Time.deltaTime;
        }


            if (attackTimer <= 0f)
        {
            Debug.Log("攻撃！");
            // 攻撃したらタイマーをリセット(3秒に戻す)
            attackTimer = attackCooldown;

        }

        //敵の名前と一致した場合、対応するスキルを使用
        if (name == EnemyName.Spider && skillTimer >= skillCooldown)
        {
            //攻撃内容
            Instantiate(spiderWebPrefab, webSpawnPoint.position, webSpawnPoint.transform.rotation);
            Debug.Log("スパイダーの攻撃！糸を飛ばす！");

            skillTimer = 0f;
            // スキルを使ったら攻撃タイマーをリセット(3秒に戻す)
            attackTimer = attackCooldown;
            hasInitialSkill = true; // 最初のスキル使用フラグを立てる
        }

        if (name == EnemyName.Mushroom && skillTimer >= skillCooldown)
        {
            //攻撃内容
            Instantiate(poisonPrefab, transform.position, transform.rotation);
            Debug.Log("キノコの攻撃！毒を与える！");
            skillTimer = 0f;
            // スキルを使ったら攻撃タイマーをリセット(3秒に戻す)
            attackTimer = attackCooldown;
            hasInitialSkill = true; // 最初のスキル使用フラグを立てる
        }



    }

    //視界を可視化するためのもの
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;

        Gizmos.DrawWireSphere(transform.position, chaseRange);

        Vector3 leftDirection = Quaternion.Euler(0, -viewAngle / 2, 0) * transform.forward;
        Vector3 rightDirection = Quaternion.Euler(0, viewAngle / 2, 0) * transform.forward;

        Gizmos.color = Color.yellow;

        Gizmos.DrawLine(transform.position, transform.position + leftDirection * chaseRange);
        Gizmos.DrawLine(transform.position, transform.position + rightDirection * chaseRange);
    }
}
