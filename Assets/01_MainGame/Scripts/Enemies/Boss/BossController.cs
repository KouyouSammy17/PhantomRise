using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class BossController : EnemyController
{
    [Header("ボスHP")]
    [SerializeField] private BossHealth bossHealth;

    [Header("ボススキル")]
    [SerializeField] private BossSkill bossSkill;

    [Header("召喚")]
    [SerializeField] private float summonCooldown = 15f;

    [Header("状態異常")]
    [SerializeField] private bool immuneToPoison = true;
    [SerializeField] private bool immuneToBurn = true;

    [Header("ボス通常攻撃")]
    [SerializeField] private float bossStopDistance = 3f;

    [SerializeField] private float bossAttackRange = 5f;
    [SerializeField] private float bossAttackCooldown = 2.0f;

    private float bossAttackTimer = 0f;

    private float summonTimer;

    private bool isInvincible;

    private NavMeshAgent Bossagent;

    [SerializeField] private bool isDie = false;

    public bool IsDead => isDie;

    [Header("登場演出")]
    [SerializeField] private bool startInactive = true;

    private bool isBossIntro = false;

    public bool IsBossIntro => isBossIntro;


    protected override void Start()
    {
        Bossagent = GetComponent<NavMeshAgent>();

        base.Start();

        summonTimer = summonCooldown;

        bossAttackTimer = 0f;

        if (startInactive)
        {
            isBossIntro = true;

            if (Bossagent != null)
            {
                Bossagent.isStopped = true;
            }
        }
    }


    protected override void Update()
    {
        // 登場演出中はAIを動かさない
        if (isBossIntro)
            return;

        if (bossHealth == null)
            return;

        if (bossHealth.CurrentHP <= 0)
        {
            OnDie();
            return;
        }

        // 通常攻撃クールタイム
        if (bossAttackTimer > 0f)
        {
            bossAttackTimer -= Time.deltaTime;
        }

        // ボス専用AI
        BossBattleUpdate();
    }


    protected override bool CanStun()
    {
        return false;
    }


    public override void TakeDamage(int damage)
    {
        if (bossHealth == null)
            return;

        bossHealth.TakeDamage(damage);

        PlayHitEffect();
    }


    public IEnumerator InvincibleTime(float duration)
    {
        isInvincible = true;

        yield return new WaitForSeconds(duration);

        isInvincible = false;
    }


    public bool IsInvincible()
    {
        return isInvincible;
    }


    public bool CanTakePoisonDamage()
    {
        return !immuneToPoison;
    }


    public bool CanTakeBurnDamage()
    {
        return !immuneToBurn;
    }


    protected override void OnDie()
    {
        if (isDie)
            return;

        isDie = true;

        Debug.Log("ボス撃破");

        StartCoroutine(Clear());
    }


    private IEnumerator Clear()
    {
        enemyAnimation.PlayDie();

        if (Bossagent != null)
        {
            Bossagent.isStopped = true;
        }

        // マップ上の敵を取得
        EnemyController[] enemies =
            FindObjectsByType<EnemyController>(
                FindObjectsSortMode.None);

        yield return new WaitForSeconds(1.5f);

        foreach (EnemyController enemy in enemies)
        {
            // 自分（ボス）は除外
            if (enemy == this)
                continue;

            Destroy(enemy.gameObject);
        }


        GameManager.Instance.TriggerGameClear();
    }


    // =========================================================
    // ボス登場演出
    // =========================================================

    public void StartBossIntro()
    {
        if (!isBossIntro)
            return;

        StartCoroutine(BossIntroCoroutine());
    }


    private IEnumerator BossIntroCoroutine()
    {
        Debug.Log("ボス登場演出開始");

        isBossIntro = true;

        if (Bossagent != null)
        {
            Bossagent.isStopped = true;
        }

        // Taunting
        enemyAnimation.PlayTaunting();

        // Tauntingの長さ
        yield return new WaitForSeconds(2.0f);

        // 戦闘開始
        StartBossBattle();
    }


    public void StartBossBattle()
    {
        if (!isBossIntro)
            return;

        Debug.Log("ボス戦開始");

        isBossIntro = false;

        bossAttackTimer = 0f;

        // 敵をスポーンさせる
        BossArenaEnemySpawner spawner =
            FindAnyObjectByType<BossArenaEnemySpawner>();

        spawner?.StartBossBattle();

        if (Bossagent != null)
        {
            Bossagent.isStopped = false;
        }
    }


    // =========================================================
    // ボスAI
    // =========================================================

    private void BossBattleUpdate()
    {
        if (player == null)
            return;

        if (Bossagent == null || !Bossagent.enabled)
            return;

        Vector3 bossPos = transform.position;
        Vector3 playerPos = player.position;

        bossPos.y = 0f;
        playerPos.y = 0f;

        float distance = Vector3.Distance(bossPos, playerPos);

        // 5mより遠い場合は追跡
        if (distance > bossAttackRange)
        {
            BossChaseMode();
        }
        else
        {
            // 5m以内なら停止して攻撃
            BossAttackMode();
        }
    }


    // =========================================================
    // プレイヤーを追跡
    // =========================================================

    private void BossChaseMode()
    {
        if (player == null)
            return;

        Bossagent.isStopped = false;

        // プレイヤーから5m離れたところで止まる
        Bossagent.stoppingDistance = bossAttackRange;

        // プレイヤーそのものを目的地にする
        Bossagent.SetDestination(player.position);

        LookAtPlayer();
    }


    // =========================================================
    // 攻撃
    // =========================================================

    private void BossAttackMode()
    {
        if (player == null)
            return;

        // =========================
        // 停止
        // =========================

        Bossagent.isStopped = true;
        Bossagent.ResetPath();
        Bossagent.velocity = Vector3.zero;

        // プレイヤーを見る
        LookAtPlayer();


        // =========================
        // スキル使用中
        // =========================

        if (bossSkill != null && bossSkill.IsUsingSkill)
        {
            // スキル中は通常攻撃もしない
            return;
        }


        // =========================
        // スキルを試す
        // =========================

        if (bossSkill != null)
        {
            bool usedSkill = bossSkill.TryUseSkill();

            if (usedSkill)
            {
                // スキル発動
                return;
            }
        }


        // =========================
        // 通常攻撃
        // =========================

        if (bossAttackTimer > 0f)
        {
            return;
        }

        bossAttackTimer = bossAttackCooldown;

        BossNormalAttack();
    }


    // =========================================================
    // 通常攻撃
    // =========================================================

    private void BossNormalAttack()
    {
        Debug.Log("ボスが通常攻撃！");

        // 攻撃アニメーション
        enemyAnimation.PlayAttack();

        // プレイヤーへダメージ
        DealDamageToPlayer();

        // ★追加
        // ボスの周囲にいる敵にもダメージ
        //DealDamageToEnemies();
    }


    // =========================================================
    // ボス通常攻撃 → 周囲の敵
    // =========================================================

    private void DealDamageToEnemies()
    {
        Collider[] hits = Physics.OverlapSphere(
            transform.position,
            bossAttackRange
        );

        foreach (Collider hit in hits)
        {
            // 子オブジェクトにColliderがある場合にも対応
            EnemyController enemy =
                hit.GetComponentInParent<EnemyController>();

            // EnemyControllerがない
            if (enemy == null)
                continue;

            // 自分自身（ボス）には攻撃しない
            if (enemy == this)
                continue;

            // 死亡している敵には攻撃しない
            if (enemy.CurrentHP <= 0)
                continue;

            // 敵にダメージ
            enemy.TakeDamage(AttackPower);

            Debug.Log(
                $"ボス通常攻撃 → {enemy.name} に {AttackPower} ダメージ");
        }
    }


    // =========================================================
    // プレイヤーへダメージ
    // =========================================================

    private void DealDamageToPlayer()
    {
        if (player == null)
            return;

        PlayerStateMachine playerMachine =
            player.GetComponent<PlayerStateMachine>();

        if (playerMachine == null)
            return;


        // 乗っ取り中
        if (playerMachine.CurrentStateName == nameof(HijackedState))
        {
            EnemyController hijackedEnemy =
                playerMachine.Hijacked.CurrentEnemy;

            if (hijackedEnemy != null &&
                hijackedEnemy.IsHijackInvincible())
            {
                Debug.Log("乗っ取り直後の無敵時間中なのでボス通常攻撃を無効化");
                return;
            }

            playerMachine.PlayerHP.TakeDamage(AttackPower);

            Debug.Log(
                $"ボスがプレイヤーに {AttackPower} ダメージ");
        }

        // 幽霊状態
        else if (playerMachine.CurrentStateName ==
                 nameof(GhostState))
        {
            playerMachine.MarkKilledByBoss();
            playerMachine.Ghost.OnHit();

            Debug.Log("ボスの攻撃でゴーストが攻撃を受けた");
        }
    }


    // =========================================================
    // プレイヤーを見る
    // =========================================================

    private void LookAtPlayer()
    {
        if (player == null)
            return;

        Vector3 direction =
            player.position - transform.position;

        direction.y = 0f;

        if (direction == Vector3.zero)
            return;

        Quaternion targetRotation =
            Quaternion.LookRotation(direction);

        transform.rotation =
            Quaternion.Slerp(
                transform.rotation,
                targetRotation,
                Time.deltaTime * 8f);
    }


    // =========================================================
    // デバッグ用：通常攻撃範囲表示
    // =========================================================

    private void OnDrawGizmosSelected()
    {
        Gizmos.DrawWireSphere(
            transform.position,
            bossAttackRange);
    }
}