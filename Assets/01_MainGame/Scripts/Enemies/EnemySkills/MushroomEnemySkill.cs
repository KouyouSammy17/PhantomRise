using System.Collections;
using UnityEngine;

public class MushroomEnemySkill : EnemySkillBase
{

    [Header("Mushroom")]
    [SerializeField] private GameObject poisonPrefab;

    [SerializeField] private Animator animator;

    public override bool TryUseSkill()
    {
        if (!CanUseSkill())
            return false;

       
        // スキルアニメーション
        //enemyController.PlaySkillAnimation();


        // 乗っ取り中はプレイヤーの位置・向きで生成する
        //GameObject obj = Instantiate(
        //    poisonPrefab,
        //    enemyController.GetAttackOrigin(),
        //    enemyController.GetAttackRotation());

        //// 毒エリアにダメージ値を渡す
        //Poisonarea area = obj.GetComponent<Poisonarea>();

        //if (area != null)
        //{
        //    area.Damage = enemyController.AttackPower;

        //    // 追加
        //    area.isHijackedSkill = enemyController.IsHijacked;
        //}

        //Debug.Log("毒胞子を散布！");

        StartCoroutine(SkillRoutine());

        ResetCooldown();

        return true;
    }

    private IEnumerator SkillRoutine()
    {
        // animator.SetTrigger("Skill");
        enemyController.PlaySkillAnimation();


        float delay;

        if (enemyController.IsHijacked)
        {
            delay = 0.3f; // 乗っ取り中は少し遅らせる
        }
        else
        {
            delay = 0.2f;
        }

        // アニメーションに合わせて待つ
        yield return new WaitForSeconds(delay);

        GameObject obj = Instantiate(
            poisonPrefab,
            enemyController.GetAttackOrigin(),
            enemyController.GetAttackRotation());

        Poisonarea area = obj.GetComponent<Poisonarea>();

        if (area != null)
        {
            area.Damage = enemyController.AttackPower;
            area.isHijackedSkill = enemyController.IsHijacked;
        }

        Debug.Log("毒胞子を散布！");

    }



}