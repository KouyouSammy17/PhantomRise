using UnityEngine;

public class MushroomEnemySkill : EnemySkillBase
{

    [Header("Mushroom")]
    [SerializeField] private GameObject poisonPrefab;

    public override bool TryUseSkill()
    {
        if (!CanUseSkill())
            return false;

        // 乗っ取り中はプレイヤーの位置・向きで生成する
        GameObject obj = Instantiate(
            poisonPrefab,
            enemyController.GetAttackOrigin(),
            enemyController.GetAttackRotation());

        // 毒エリアにダメージ値を渡す
        Poisonarea area = obj.GetComponent<Poisonarea>();

        if (area != null)
        {
            area.Damage = enemyController.AttackPower;

            // 追加
            area.isHijackedSkill = enemyController.IsHijacked;
        }

        Debug.Log("毒胞子を散布！");

        ResetCooldown();

        return true;
    }

}