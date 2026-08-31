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

using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class PlayerHPBarUI : MonoBehaviour
{
    [Header("=== 参照 ===")]
    [SerializeField] private Slider _hpSlider;
    [SerializeField] private PlayerStateMachine _playerMachine;

    [Header("=== 色 ===")]
    [SerializeField] private Color _fillColor = Color.green;

    [Header("=== 乗っ取り中の位置調整 ===")]
    // HP バーは Screen Space Overlay の HUD なので、本来は敵の背の高さと無関係。
    // ただし背の高い敵に乗っ取るとモデルがバーに重なって見えるため、
    // 体の高さに応じてバーだけ少し上へずらす。
    [Tooltip("この高さ（ワールド単位）を超えた分だけバーを上へずらす")]
    [SerializeField] private float _referenceBodyHeight = 2f;

    [Tooltip("超過 1 単位あたり何ピクセル上げるか")]
    [SerializeField] private float _offsetPerUnit = 14f;

    [Tooltip("上げ幅の上限（ピクセル）")]
    [SerializeField] private float _maxOffset = 60f;

    [Tooltip("HP バーと一緒にずらす UI（バフアイコンなど）")]
    [SerializeField] private RectTransform[] _offsetTargets;

    private Image _fillImage;

    // ─── 位置調整用 ───────────────────────────
    // HP バー本体 ＋ _offsetTargets をまとめて動かす
    private RectTransform[] _targets;
    private Vector2[] _basePositions;

    /// <summary>今オフセットを計算済みの敵（体が変わったときだけ計算し直す）</summary>
    private EnemyController _offsetAppliedFor;

    // ─────────────────────────────────────────
    // 初期化
    // ─────────────────────────────────────────

    private void Start()
    {
        // PlayerStateMachine が未指定なら自動検索
        if (_playerMachine == null)
            _playerMachine = FindAnyObjectByType<PlayerStateMachine>();

        CacheOffsetTargets();

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

        bool isHijacked = _playerMachine.IsEffectivelyHijacked;

        // 表示 / 非表示を切り替え
        if (_hpSlider.gameObject.activeSelf != isHijacked)
            _hpSlider.gameObject.SetActive(isHijacked);

        // 乗っ取り中だけ値を更新
        if (!isHijacked)
        {
            ResetPosition();
            return;
        }

        UpdateOffset();

        PlayerHP hp = _playerMachine.PlayerHP;
        if (hp == null || hp.MaxHP <= 0) return;

        _hpSlider.value = (float)hp.CurrentHP / hp.MaxHP;
    }

    // ─────────────────────────────────────────
    // 体の高さに応じた位置調整
    // ─────────────────────────────────────────

    /// <summary>乗っ取っている体が変わったときだけ計算し直す。</summary>
    private void UpdateOffset()
    {
        EnemyController enemy = _playerMachine.Hijacked?.CurrentEnemy;

        if (enemy == _offsetAppliedFor) return;
        _offsetAppliedFor = enemy;

        if (enemy == null)
        {
            ApplyOffset(0f);
            return;
        }

        float height = MeasureBodyHeight(enemy);

        ApplyOffset(Mathf.Clamp(
            (height - _referenceBodyHeight) * _offsetPerUnit, 0f, _maxOffset));
    }

    private void ResetPosition()
    {
        if (_offsetAppliedFor == null) return;

        _offsetAppliedFor = null;
        ApplyOffset(0f);
    }

    /// <summary>HP バーと登録された UI をまとめて上へずらす。</summary>
    private void ApplyOffset(float pixels)
    {
        if (_targets == null) return;

        for (int i = 0; i < _targets.Length; i++)
        {
            if (_targets[i] == null) continue;

            _targets[i].anchoredPosition = _basePositions[i] + Vector2.up * pixels;
        }
    }

    /// <summary>ずらす対象と、その元の位置を控えておく。</summary>
    private void CacheOffsetTargets()
    {
        List<RectTransform> targets = new List<RectTransform>();

        if (_hpSlider != null)
        {
            RectTransform sliderRect = _hpSlider.GetComponent<RectTransform>();
            if (sliderRect != null) targets.Add(sliderRect);
        }

        if (_offsetTargets != null)
        {
            foreach (RectTransform rt in _offsetTargets)
                if (rt != null && !targets.Contains(rt)) targets.Add(rt);
        }

        _targets       = targets.ToArray();
        _basePositions = _targets.Select(t => t.anchoredPosition).ToArray();
    }

    /// <summary>
    /// 体の見た目の高さ（ワールド単位）。
    ///
    /// 敵ごとの数値を持たせなくて済むよう、実際の Renderer の
    /// 大きさから測る。見つからなければ基準値を返す（＝ずらさない）。
    /// </summary>
    private float MeasureBodyHeight(EnemyController enemy)
    {
        Transform visual = enemy.GetVisualRoot();
        if (visual == null) return _referenceBodyHeight;

        Renderer[] renderers = visual.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0) return _referenceBodyHeight;

        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
            bounds.Encapsulate(renderers[i].bounds);

        return bounds.size.y;
    }
}
