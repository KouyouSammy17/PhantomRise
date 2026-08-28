using UnityEngine;
using UnityEngine.UI;
using TMPro;


public class SkillCooldownUI : MonoBehaviour
{
    [Header("=== 参照 ===")]
    [SerializeField] private PlayerStateMachine playerMachine;

    [Header("=== UI ===")]
    [SerializeField] private Image cooldownMask;
    [SerializeField] private GameObject skillUIPanel;
    [SerializeField] private Image skillIconImage;
    [SerializeField] private TextMeshProUGUI skillNameText;
    [SerializeField] private Sprite defaultIcon;

    // 現在追跡中の敵スキル
    private EnemySkillBase _trackedSkill;

    [Header("=== サウンド ===")]
    [SerializeField] private AudioSource skillAudio;

    // 前フレームでクールダウン中だったか
    private bool _wasOnCooldown = false;


    private void Awake()
    {
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


        // ==========================================
        // 乗っ取り中ではない
        // ==========================================

        if (!isHijacked)
        {
            _trackedSkill = null;

            if (cooldownMask != null)
                cooldownMask.fillAmount = 0f;

            _wasOnCooldown = false;

            return;
        }


        // ==========================================
        // 現在乗っ取っている敵を取得
        // ==========================================

        EnemyController enemy =
            playerMachine.Hijacked.CurrentEnemy;


        if (enemy != null)
        {
            EnemySkillBase skill =
                enemy.GetComponent<EnemySkillBase>();


            // 敵が変わったとき
            if (skill != _trackedSkill)
            {
                _trackedSkill = skill;

                RefreshSkillVisual();

                // 新しい敵を乗っ取った瞬間は
                // 現在のクールダウン状態を記録するだけ
                if (_trackedSkill != null)
                {
                    _wasOnCooldown =
                        _trackedSkill.CooldownFillAmount > 0f;
                }
                else
                {
                    _wasOnCooldown = false;
                }
            }
        }
        else
        {
            if (_trackedSkill != null)
            {
                _trackedSkill = null;
                RefreshSkillVisual();
            }

            _wasOnCooldown = false;
        }


        // ==========================================
        // クールダウン表示
        // ==========================================

        if (cooldownMask != null)
        {
            float currentFill =
                _trackedSkill != null
                    ? _trackedSkill.CooldownFillAmount
                    : 0f;


            cooldownMask.fillAmount = currentFill;


            // ======================================
            // クールダウン完了を検出
            // ======================================

            if (_wasOnCooldown && currentFill <= 0f)
            {
                Debug.Log("[SkillCooldownUI] スキルチャージ完了！");

                if (skillAudio != null)
                {
                    skillAudio.PlayOneShot(
                        skillAudio.clip);
                }
            }


            // 現在の状態を保存
            _wasOnCooldown = currentFill > 0f;
        }
    }


    private void RefreshSkillVisual()
    {
        // アイコン
        if (skillIconImage != null)
        {
            Sprite icon =
                _trackedSkill != null &&
                _trackedSkill.SkillIcon != null
                    ? _trackedSkill.SkillIcon
                    : defaultIcon;

            if (icon != null)
                skillIconImage.sprite = icon;
        }


        // スキル名
        if (skillNameText != null)
        {
            skillNameText.text =
                _trackedSkill != null
                    ? _trackedSkill.SkillName
                    : "";
        }
    }
}