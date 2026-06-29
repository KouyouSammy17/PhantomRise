// ============================================================
// SkillCooldownUI.cs
// 乗っ取り中の敵スキルクールダウンを表示する UI。
//
// セットアップ:
//   ・playerMachine  — PlayerStateMachine を Inspector でアサイン
//   ・cooldownMask   — Image (Type: Filled, Fill Method: Radial360 など)
//   ・skillUIPanel   — このまるごとの親パネル（乗っ取り中のみ表示）
//
// 動作:
//   ・HijackedState 中のみパネルを表示
//   ・現在乗っ取っている敵の EnemySkillBase から CooldownFillAmount を読み取り
//     cooldownMask.fillAmount に反映（0 = 使用可能, 1 = クールダウン中）
//   ・ボディ転送後も自動で新しい敵のスキルに切り替わる
// ============================================================

using UnityEngine;
using UnityEngine.UI;

public class SkillCooldownUI : MonoBehaviour
{
    [Header("=== 参照 ===")]
    [SerializeField] private PlayerStateMachine playerMachine;

    [Header("=== UI ===")]
    /// <summary>クールダウン量を表す Filled Image（fillAmount 0〜1 で制御）</summary>
    [SerializeField] private Image cooldownMask;

    /// <summary>スキル UI 全体のパネル（乗っ取り中のみ表示）</summary>
    [SerializeField] private GameObject skillUIPanel;

    // 現在追跡中の敵スキル
    private EnemySkillBase _trackedSkill;

    private void Update()
    {
        bool isHijacked = playerMachine != null
            && playerMachine.CurrentStateName == nameof(HijackedState);

        // パネルの表示切り替え
        if (skillUIPanel != null)
            skillUIPanel.SetActive(isHijacked);

        if (!isHijacked)
        {
            _trackedSkill = null;
            if (cooldownMask != null) cooldownMask.fillAmount = 0f;
            return;
        }

        // 現在乗っ取っている敵のスキルを取得（ボディ転送にも対応）
        EnemyController enemy = playerMachine.Hijacked.CurrentEnemy;
        if (enemy != null)
        {
            // 敵が変わったとき（転送後）に再キャッシュ
            EnemySkillBase skill = enemy.GetComponent<EnemySkillBase>();
            if (skill != _trackedSkill)
                _trackedSkill = skill;
        }
        else
        {
            _trackedSkill = null;
        }

        // クールダウンを反映
        if (cooldownMask != null)
            cooldownMask.fillAmount = _trackedSkill != null
                ? _trackedSkill.CooldownFillAmount
                : 0f;
    }
}
