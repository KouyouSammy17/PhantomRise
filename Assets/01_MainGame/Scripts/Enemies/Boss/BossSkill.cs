using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class BossSkill : EnemySkillBase
{
    [Header("攻撃設定")]
    [SerializeField] private int damage = 30;

    [Header("衝撃波")]
    [SerializeField] private GameObject warningPrefab;
    [SerializeField] private GameObject shockwavePrefab;

    [SerializeField] private float warningTime = 1f;
    [SerializeField] private float shockwaveSpeed = 8f;

    [SerializeField] private NavMeshAgent agent;


    private bool isUsingSkill = false;

    // スキル使用中かどうかを外部から確認できるようにするプロパティ
    public bool IsUsingSkill => isUsingSkill;

   [SerializeField] private BossController controller;

    public override bool TryUseSkill()
    {
        if (controller != null && controller.IsDead)
            return false;


        if (!CanUseSkill() || isUsingSkill)
            return false;

        enemyController.PlaySkillAnimation();

        StartCoroutine(ShockwaveAttack());

        return true;
    }

    private IEnumerator ShockwaveAttack()
    {
        if(controller.IsDead)
            yield break;

        Debug.Log("ボスが衝撃波を発動しました。");
       
        //スキルが発動したらagentの動きを止める
       agent.isStopped = true;

        isUsingSkill = true;
     
        // 攻撃範囲の円
        GameObject warning = Instantiate(
            warningPrefab,
            transform.position + Vector3.up * 0.05f,
            Quaternion.Euler(90f, 0f, 0f));

        float targetScale = SkillRange * 2f;

        warning.transform.localScale =
            new Vector3(targetScale, targetScale, targetScale);

        // 広がる波
        GameObject wave = Instantiate(
            shockwavePrefab,
            transform.position + Vector3.up * 0.1f,
            Quaternion.Euler(90f, 0f, 0f));

        wave.transform.localScale = Vector3.zero;

        float currentScale = 0f;

        while (currentScale < targetScale)
        {
            if (controller.IsDead)
            {
                Destroy(warning);
                Destroy(wave);

                isUsingSkill = false;
                yield break;
            }



            //Debug.Log("SkillRange = " + SkillRange);
            //Debug.Log("TargetScale = " + targetScale);
            // 時間の経過に合わせて広がる
            currentScale += shockwaveSpeed * Time.deltaTime;

            // 円の最大サイズ（攻撃範囲）を超えないようにぴったり合わせる
            if (currentScale >= targetScale)
                currentScale = targetScale;

            wave.transform.localScale =
                new Vector3(
                    currentScale,
                    currentScale,
                    currentScale);

            yield return null;
        }

        float attackRadius = warning.transform.lossyScale.x / 2f+2.0f;


        PlayerStateMachine players =
    FindAnyObjectByType<PlayerStateMachine>();

        //Debug.Log(
        //    "Player Distance = " +
        //    Vector3.Distance(
        //        transform.position,
        //        players.transform.position));

        //Debug.Log(
        //    "SkillRange = " +
        //    SkillRange);

        //Debug.Log(
        //    "AttackRadius = " +
        //    attackRadius);


        if (controller.IsDead)
        {
            Destroy(warning);
            Destroy(wave);

            isUsingSkill = false;
            yield break;
        }

        // 波が端まで到達（目標サイズと重なった）直後に攻撃判定を行う
        // ====== 攻撃 ======

        Collider[] hits =
            Physics.OverlapSphere(
                transform.position,
                attackRadius);
    
        foreach (Collider hit in hits)
        {
            PlayerStateMachine player =
                hit.GetComponent<PlayerStateMachine>();

            if (player == null)
                continue;

            if (player.CurrentStateName ==
                nameof(HijackedState))
            {
                player.PlayerHP.TakeDamage(damage);
            }
            else if (player.CurrentStateName ==
                     nameof(GhostState))
            {
                player.Ghost.OnHit();
            }
        }

        // 重なった瞬間に攻撃が発動後、少しだけその状態を表示して（視認しやすくする）、消す場合はここで待機を追加できます
        yield return new WaitForSeconds(0.1f);

        // 攻撃範囲の円と広がる波を消去する
        Destroy(warning);
        Destroy(wave);

        isUsingSkill = false;
        agent.isStopped = false;

        //スキルのクールダウンを開始する
        ResetCooldown();


    }
}