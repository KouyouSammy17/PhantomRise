// ============================================================
// PlayerHPBarUI.cs
// 乗っ取り中にプレイヤー HP バーを表示する HUD スクリプト
//
// 【背の高い体に乗っ取ったとき】
//   デーモンのような、飛び抜けて背の高いモンスターだけ持ち上げる。
//
//   体の高さは CharacterController から測る（center.y + height / 2）。
//   見た目の Renderer から測ると、羽・角・エフェクトまで入ってしまい、
//   マッシュルームのような小さい相手まで持ち上がってしまうため。
//
//   体の上端（足元からの高さ・ワールド単位）：
//     プレイヤー（幽霊） 0.95  … center 0.2 + height 1.5 / 2
//     スパイダー / マッシュルーム 0.50
//     スケルトン 0.83
//     バット / メイジ 1.10
//     ウェアウルフ 1.29
//     スペクター 1.43
//     デーモン 2.31
//   しきい値 1.8 を超えるのはデーモン（とボス）だけ。
//
// 【表示について】
//   PlayerCanvas は World Space（プレイヤーの頭上に浮かぶ）。
//   そのままだとプレイヤーの向きに合わせて回ってしまうので、
//   LateUpdate でカメラと同じ向きに直している（EnemyHPbar と同じやり方）。
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

    [Header("=== カメラの向きに合わせる ===")]
    [Tooltip("OFF にすると、キャンバスがプレイヤーと一緒に回る")]
    [SerializeField] private bool _faceCamera = true;

    [Header("=== 乗っ取り中の位置調整 ===")]
    // 乗っ取った体の一番上を測り、幽霊のときの頭の高さとの差だけ持ち上げる。
    // 「1 単位あたり何ピクセル」のような係数は要らない。
    // 体が 1 ワールド単位高ければ、UI も 1 ワールド単位上がる。
    [Tooltip("体の上端がこの高さ（ワールド単位）を超えたときだけ持ち上げる。\n"
           + "大きくすると、より背の高い相手だけが対象になる")]
    [SerializeField] private float _riseThreshold = 1.8f;

    [Tooltip("幽霊のときの体の上端（CharacterController の center.y + height / 2）。\n"
           + "プレイヤーは center 0.2・height 1.5 なので 0.95")]
    [SerializeField] private float _ghostBodyTop = 0.95f;

    [Tooltip("持ち上げ幅の上限（ワールド単位）")]
    [SerializeField] private float _maxRise = 2f;

    [Tooltip("HP バーと一緒にずらす UI（バフアイコンなど）")]
    [SerializeField] private RectTransform[] _offsetTargets;

    private Image _fillImage;

    /// <summary>向きを合わせる相手。Camera.main は重いので覚えておく。</summary>
    private Transform _cameraTransform;

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
    /// <summary>
    /// World Space キャンバスをカメラと同じ向きに向ける。
    /// カメラが切り替わることがあるので、居なくなったら取り直す。
    /// </summary>
    private void LateUpdate()
    {
        if (!_faceCamera) return;

        if (_cameraTransform == null)
        {
            Camera cam = Camera.main;

            if (cam == null) return;

            _cameraTransform = cam.transform;
        }

        transform.rotation = _cameraTransform.rotation;
    }

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

        if (!TryMeasureBodyTop(enemy, out float topWorldY))
        {
            ApplyOffset(0f);
            return;
        }

        // キャンバスの原点から見た、体の上端の高さ
        float top = topWorldY - transform.position.y;

        // しきい値を超えない体はそのまま。
        // 少し背が高いだけの相手まで動かすと、全体的に浮いて見える。
        if (top <= _riseThreshold)
        {
            Debug.Log($"[PlayerHPBarUI] {enemy.name} 上端 {top:F2} → 持ち上げなし");
            ApplyOffset(0f);
            return;
        }

        // 頭の上に出したいので、幽霊のときの上端との差だけ持ち上げる
        float rise = Mathf.Clamp(top - _ghostBodyTop, 0f, _maxRise);

        Debug.Log($"[PlayerHPBarUI] {enemy.name} 上端 {top:F2} → {rise:F2} 持ち上げ");

        // ワールド単位 → キャンバス内の単位
        float scale = transform.lossyScale.y;

        if (Mathf.Approximately(scale, 0f)) return;

        ApplyOffset(rise / scale);
    }

    private void ResetPosition()
    {
        if (_offsetAppliedFor == null) return;

        _offsetAppliedFor = null;
        ApplyOffset(0f);
    }

    /// <summary>HP バーと登録された UI をまとめて上へずらす（キャンバス内の単位）。</summary>
    private void ApplyOffset(float amount)
    {
        if (_targets == null) return;

        for (int i = 0; i < _targets.Length; i++)
        {
            if (_targets[i] == null) continue;

            _targets[i].anchoredPosition = _basePositions[i] + Vector2.up * amount;
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
    /// <summary>
    /// 乗っ取った体の上端（ワールド座標の Y）。
    /// CharacterController の中心と高さから出す。測れなければ false。
    /// </summary>
    private bool TryMeasureBodyTop(EnemyController enemy, out float topWorldY)
    {
        topWorldY = 0f;

        CharacterController body = enemy.GetComponent<CharacterController>();

        if (body == null) return false;

        // center / height はローカル値なので、スケールを掛けてワールドに直す
        float scale = enemy.transform.lossyScale.y;

        topWorldY = enemy.transform.position.y
                  + (body.center.y + body.height * 0.5f) * scale;

        return true;
    }
}
