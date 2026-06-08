using UnityEngine;

public abstract class EnemySkillBase : MonoBehaviour
{
    [Header("スキル設定")]
    [SerializeField] private float skillCooldown = 5f;

    protected float skillTimer;

    protected EnemyController enemyController;

    [SerializeField]
    protected float skillRange = 8f;

    public float SkillRange => skillRange;

    protected virtual void Start()
    {
        enemyController = GetComponent<EnemyController>();

        // 最初からスキル使用可能にする
        skillTimer = skillCooldown;
    }

    protected virtual void Update()
    {
        if (skillTimer < skillCooldown)
        {
            skillTimer += Time.deltaTime;
        }
    }

    // スキルを使えるか
    public bool CanUseSkill()
    {
        return skillTimer >= skillCooldown;
    }

    // クールダウンリセット
    protected void ResetCooldown()
    {
        skillTimer = 0f;
    }

    // 各敵がオーバーライドする
    public abstract bool TryUseSkill();
}