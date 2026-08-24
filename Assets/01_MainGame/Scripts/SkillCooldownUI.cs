// ============================================================
// SkillCooldownUI.cs
// 乗っ取り中の敵スキルクールダウンを表示する UI。
//
// セットアップ:
//   ・playerMachine  — PlayerStateMachine を Inspector でアサイン
//   ・cooldownMask   — Image (Type: Filled, Fill Method: Radial360 など)
//   ・skillUIPanel   — このまるごとの親パネル（乗っ取り中のみ表示）
//   ・skillIconImage — スキルアイコン表示用 Image（SkillIcon そのもの）
//   ・skillNameText  — スキル名表示用 TextMeshProUGUI（任意）
//
// 動作:
//   ・HijackedState 中のみパネルを表示
//   ・現在乗っ取っている敵の EnemySkillBase から CooldownFillAmount を読み取り
//     cooldownMask.fillAmount に反映（0 = 使用可能, 1 = クールダウン中）
//   ・敵ごとの SkillIcon / SkillName（EnemySkillBase で設定）に自動で切り替え
//   ・ボディ転送後も自動で新しい敵のスキルに切り替わる
// ============================================================

using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SkillCooldownUI : MonoBehaviour
{
    [Header("=== 参照 ===")]
    [SerializeField] private PlayerStateMachine playerMachine;

    [Header("=== UI ===")]
    /// <summary>クールダウン量を表す Filled Image（fillAmount 0〜1 で制御）</summary>
    [SerializeField] private Image cooldownMask;

    /// <summary>スキル UI 全体のパネル（乗っ取り中のみ表示）</summary>
    [SerializeField] private GameObject skillUIPanel;

    /// <summary>スキルアイコン表示用 Image（敵ごとに自動で差し替わる）</summary>
    [SerializeField] private Image skillIconImage;

    /// <summary>スキル名表示用テキスト（未アサインでも可）</summary>
    [SerializeField] private TextMeshProUGUI skillNameText;

    /// <summary>SkillIcon が未設定の敵に使うフォールバックアイコン</summary>
    [SerializeField] private Sprite defaultIcon;

    // 現在追跡中の敵スキル
    private EnemySkillBase _trackedSkill;

    private void Awake()
    {
        // Inspector で未アサインでもシーンから自動取得する
        if (playerMachine == null)
            playerMachine = FindAnyObjectByType<PlayerStateMachine>();
    }

    private void Update()
    {
        bool isHijacked = playerMachine != null
            && playerMachine.IsEffectivelyHijacked;

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
            {
                _trackedSkill = skill;
                RefreshSkillVisual();
            }
        }
        else
        {
            if (_trackedSkill != null)
            {
                _trackedSkill = null;
                RefreshSkillVisual();
            }
        }

        // クールダウンを反映
        if (cooldownMask != null)
            cooldownMask.fillAmount = _trackedSkill != null
                ? _trackedSkill.CooldownFillAmount
                : 0f;
    }

    /// <summary>
    /// 追跡中のスキルに合わせてアイコンとスキル名を更新する。
    /// 敵が切り替わったとき（乗っ取り・ボディ転送）に呼ばれる。
    /// </summary>
    private void RefreshSkillVisual()
    {
        // アイコン
        if (skillIconImage != null)
        {
            Sprite icon = _trackedSkill != null && _trackedSkill.SkillIcon != null
                ? _trackedSkill.SkillIcon
                : defaultIcon;

            if (icon != null)
                skillIconImage.sprite = icon;
        }

        // スキル名
        if (skillNameText != null)
        {
            skillNameText.text = _trackedSkill != null
                ? _trackedSkill.SkillName
                : "";
        }
    }
}
