using UnityEngine;

public class BossHealth : EnemyHealth
{
    [Header("ボス設定")]
    [SerializeField] private bool immuneToPoison = true;

    public override void TakeDamage(int damage)
    {
        base.TakeDamage(damage);

        Debug.Log(
            $"ボス被弾 HP:{currentHP}/{maxHP}");
    }

    public override void ApplyPoison(
        float duration,
        float interval,
        float percent)
    {
        if (immuneToPoison)
        {
            Debug.Log("ボスは毒無効");
            return;
        }

        base.ApplyPoison(
            duration,
            interval,
            percent);
    }

    
}