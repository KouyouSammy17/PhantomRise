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

    /// <summary>
    /// UI 用のクールダウン充填量（0 = 使用可能, 1 = 使用直後）。
    /// Image.fillAmount に直接セットして使う。
    /// </summary>
    public float CooldownFillAmount =>
        CanUseSkill() ? 0f : 1f - (skillTimer / skillCooldown);

    // クールダウンリセット
    protected void ResetCooldown()
    {
        skillTimer = 0f;
    }

    //乗っ取ったときにスキルを即座に使えるようにする
    public void ResetSkillImmediately()
    {
        skillTimer = skillCooldown;
    }

    // 各敵がオーバーライドする
    public abstract bool TryUseSkill();
}