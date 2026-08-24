using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class BossController : EnemyController
{
    //ボスは雑魚敵を周りに召喚できるようにする
    //毒などの定数ダメージもきかないようにする
    //HPが減ると攻撃パターンが変わるようにする
    //フェーズを分けて、フェーズごとにスキルを変える
    //BossControllerとBossHealthとBossSkillの三つに分ける

    [Header("ボスHP")]
    [SerializeField] private BossHealth bossHealth;

    [Header("ボススキル")]
    [SerializeField] private BossSkill bossSkill;

    [Header("召喚")]
    //[SerializeField] private GameObject enemyPrefab;
   // [SerializeField] private Transform[] summonPoints;
    [SerializeField] private float summonCooldown = 15f;

    [Header("状態異常")]
    [SerializeField] private bool immuneToPoison = true;
    [SerializeField] private bool immuneToBurn = true;

    private float summonTimer;

    private bool isInvincible;

    //private EnemyAnimation enemyAnimation;

    private NavMeshAgent Bossagent;

    [SerializeField]private bool isDie=false;

    public bool IsDead => isDie;

    protected override void Start()
    {
        //enemyAnimation = GetComponent<EnemyAnimation>();
        Bossagent = GetComponent<NavMeshAgent>();
        base.Start();

        summonTimer = summonCooldown;
    }

    protected override void Update()
    {
        base.Update();

        if (bossHealth == null)
            return;

        if (bossHealth.CurrentHP <= 0)
        {
            OnDie();
            return;
        }

      //SummonMinions();
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

    //private void SummonMinions()
    //{
    //    if (enemyPrefab == null)
    //        return;

    //    if (summonPoints.Length == 0)
    //        return;

    //    summonTimer -= Time.deltaTime;

    //    if (summonTimer > 0)
    //        return;

    //    summonTimer = summonCooldown;

    //    foreach (Transform point in summonPoints)
    //    {
    //        Instantiate(
    //            enemyPrefab,
    //            point.position,
    //            point.rotation);
    //    }

    //    Debug.Log("ボスが雑魚敵を召喚");
    //}

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

    //private void Die()
    //{
    //    if(isdead)
    //        return;

    //    isdead = true;


    //    Debug.Log("ボス撃破");

    //   // GameManager.Instance.TriggerGameClear();

    //    StartCoroutine(Clear());



    //}

    protected override void OnDie()
    {
        if (isDie) return;


        isDie = true;

        Debug.Log("ボス撃破");

        StartCoroutine(Clear());
    }

    private IEnumerator Clear()
    {
        enemyAnimation.PlayDie();
        Bossagent.isStopped = true;

        yield return new WaitForSeconds(1.5f);
        //Destroy(gameObject);
        GameManager.Instance.TriggerGameClear();
    }


}
