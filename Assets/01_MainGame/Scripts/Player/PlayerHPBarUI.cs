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
    [SerializeField] private Slider _hpSlider;
    [SerializeField] private PlayerStateMachine _playerMachine;

    [Header("=== 色 ===")]
    [SerializeField] private Color _fillColor = Color.green;

    private Image _fillImage;

    // ─────────────────────────────────────────
    // 初期化
    // ─────────────────────────────────────────

    private void Start()
    {
        // PlayerStateMachine が未指定なら自動検索
        if (_playerMachine == null)
            _playerMachine = FindAnyObjectByType<PlayerStateMachine>();

        if (_hpSlider != null)
        {
            // fillRect の Image を緑に染める
            _fillImage = _hpSlider.fillRect != null
                ? _hpSlider.fillRect.GetComponent<Image>()
                : null;
            if (_fillImage != null)
                _fillImage.color = _fillColor;

            // Slider の範囲を 0–1 に固定
            _hpSlider.minValue = 0f;
            _hpSlider.maxValue = 1f;
            _hpSlider.value    = 1f;

            // 最初は非表示
            _hpSlider.gameObject.SetActive(false);
        }
    }

    // ─────────────────────────────────────────
    // 毎フレーム更新
    // ─────────────────────────────────────────

    private void Update()
    {
        if (_playerMachine == null || _hpSlider == null) return;

        bool isHijacked = _playerMachine.CurrentStateName == nameof(HijackedState);

        // 表示 / 非表示を切り替え
        if (_hpSlider.gameObject.activeSelf != isHijacked)
            _hpSlider.gameObject.SetActive(isHijacked);

        // 乗っ取り中だけ値を更新
        if (!isHijacked) return;

        PlayerHP hp = _playerMachine.PlayerHP;
        if (hp == null || hp.MaxHP <= 0) return;

        _hpSlider.value = (float)hp.CurrentHP / hp.MaxHP;
    }
}
