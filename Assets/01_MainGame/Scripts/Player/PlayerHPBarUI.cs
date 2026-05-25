// ============================================================
// PlayerHPBarUI.cs
// 乗っ取り中にプレイヤー HP バーを表示する HUD スクリプト
//
// 【セットアップ手順】
//   1. Canvas の下に UI > Slider を作成（名前例: PlayerHPBar）
//   2. このスクリプトを Canvas か任意の GameObject にアタッチ
//   3. Inspector で HpSlider にその Slider を、
//      PlayerMachine にプレイヤーの PlayerStateMachine をアサイン
//   4. 必要なら FillColor を変更（デフォルト: 緑）
// ============================================================

using UnityEngine;
using UnityEngine.UI;

public class PlayerHPBarUI : MonoBehaviour
{
    [Header("=== 参照 ===")]
    [SerializeField] private Slider hpSlider;
    [SerializeField] private PlayerStateMachine playerMachine;

    [Header("=== 色 ===")]
    [SerializeField] private Color fillColor = Color.green;

    private Image _fillImage;

    // ─────────────────────────────────────────
    // 初期化
    // ─────────────────────────────────────────

    private void Start()
    {
        // PlayerStateMachine が未指定なら自動検索
        if (playerMachine == null)
            playerMachine = FindAnyObjectByType<PlayerStateMachine>();

        if (hpSlider != null)
        {
            // fillRect の Image を緑に染める
            _fillImage = hpSlider.fillRect != null
                ? hpSlider.fillRect.GetComponent<Image>()
                : null;
            if (_fillImage != null)
                _fillImage.color = fillColor;

            // Slider の範囲を 0–1 に固定
            hpSlider.minValue = 0f;
            hpSlider.maxValue = 1f;
            hpSlider.value    = 1f;

            // 最初は非表示
            hpSlider.gameObject.SetActive(false);
        }
    }

    // ─────────────────────────────────────────
    // 毎フレーム更新
    // ─────────────────────────────────────────

    private void Update()
    {
        if (playerMachine == null || hpSlider == null) return;

        bool isHijacked = playerMachine.CurrentStateName == nameof(HijackedState);

        // 表示 / 非表示を切り替え
        if (hpSlider.gameObject.activeSelf != isHijacked)
            hpSlider.gameObject.SetActive(isHijacked);

        // 乗っ取り中だけ値を更新
        if (!isHijacked) return;

        PlayerHP hp = playerMachine.PlayerHP;
        if (hp == null || hp.MaxHP <= 0) return;

        hpSlider.value = (float)hp.CurrentHP / hp.MaxHP;
    }
}
