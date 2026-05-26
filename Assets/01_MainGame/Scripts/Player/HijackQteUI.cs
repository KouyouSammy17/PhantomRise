// ============================================================
// HijackQTEUI.cs
// 音ゲー風 QTE UI
//
// 外側から内側へ縮むリングを N 回タイミングよく押す。
// 中心の緑ゾーンに重なったとき → 成功カウント +1
// ミス 1 回 → 即失敗
//
// Hierarchy 構成（Screen Space Overlay Canvas）:
//   QTEPanel
//     ├── RingInner   (Image, 緑, 固定サイズ)
//     ├── RingOuter   (Image, 白リング, 縮むやつ)
//     ├── CountText   (TextMeshPro "1/3")
//     └── HintText    (TextMeshPro "タイミングよく押せ！")
// ============================================================

using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class HijackQTEUI : MonoBehaviour
{
    [Header("=== 参照 ===")]
    [SerializeField] private GameObject qtePanel;
    [SerializeField] private RectTransform ringOuter;
    [SerializeField] private RectTransform ringInner;
    [SerializeField] private Image ringOuterImage;
    [SerializeField] private TextMeshProUGUI countText;
    [SerializeField] private TextMeshProUGUI hintText;

    [Header("=== QTE パラメーター ===")]
    [SerializeField] private float outerStartSize = 400f;
    [SerializeField] private float innerSize = 120f;
    [SerializeField] private float shrinkSpeed = 180f;   // px/秒
    [SerializeField] private float hitWindow = 30f;    // 成功ウィンドウ幅
    [SerializeField] private int requiredHits = 1;

    [Header("=== 色 ===")]
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color successColor = Color.green;
    [SerializeField] private Color failColor = Color.red;

    // ─────────────────────────────────────────
    // 内部
    // ─────────────────────────────────────────

    private HijackState _hijackState;
    private bool _active;
    private float _currentSize;
    private int _hitCount;
    private bool _waitNext;
    private float _waitTimer;
    private const float WaitBetweenBeats = 0.35f;

    // ─────────────────────────────────────────
    // 初期化
    // ─────────────────────────────────────────

    public void Initialize(HijackState state)
    {
        _hijackState = state;
        _hijackState.OnQTEStart += StartQTE;
        qtePanel?.SetActive(false);
    }

    private void OnDestroy()
    {
        if (_hijackState != null)
            _hijackState.OnQTEStart -= StartQTE;
    }

    // ─────────────────────────────────────────
    // Unity Update
    // ─────────────────────────────────────────

    private void Update()
    {
        if (!_active) return;

        if (_waitNext)
        {
            _waitTimer -= Time.deltaTime;
            if (_waitTimer <= 0f) BeginBeat();
            return;
        }

        _currentSize -= shrinkSpeed * Time.deltaTime;
        ApplySizes();

        // リングが緑ゾーンを通過してしまったら自動ミス
        if (_currentSize < innerSize - hitWindow)
            RegisterMiss();
    }

    // ─────────────────────────────────────────
    // QTE 開始
    // ─────────────────────────────────────────

    private void StartQTE(EnemyController enemy)
    {
        _hitCount = 0;
        _active = true;
        _waitNext = false;

        qtePanel?.SetActive(true);
        UpdateCount();
        if (hintText) hintText.text = "Press Space!";
        BeginBeat();
    }

    // ─────────────────────────────────────────
    // ボタン入力（PlayerStateMachine の OnHijackInput から転送）
    // ─────────────────────────────────────────

    public void OnQTEPress()
    {
        if (!_active || _waitNext) return;

        if (Mathf.Abs(_currentSize - innerSize) <= hitWindow)
            RegisterHit();
        else
            RegisterMiss();
    }

    // ─────────────────────────────────────────
    // 内部処理
    // ─────────────────────────────────────────

    private void BeginBeat()
    {
        _currentSize = outerStartSize;
        _waitNext = false;
        if (ringOuterImage) ringOuterImage.color = normalColor;
        ApplySizes();
    }

    private void RegisterHit()
    {
        _hitCount++;
        UpdateCount();
        Flash(successColor);
        Debug.Log($"[QTE] Hit {_hitCount}/{requiredHits}");

        if (_hitCount >= requiredHits)
        {
            End(true);
        }
        else
        {
            _waitNext = true;
            _waitTimer = WaitBetweenBeats;
        }
    }

    private void RegisterMiss()
    {
        Flash(failColor);
        Debug.Log("[QTE] Miss");
        End(false);
    }

    private void End(bool success)
    {
        _active = false;
        qtePanel?.SetActive(false);

        if (success) _hijackState?.OnQTESuccess();
        else _hijackState?.OnQTEFail();
    }

    private void Flash(Color c)
    {
        if (ringOuterImage) ringOuterImage.color = c;
    }

    private void ApplySizes()
    {
        if (ringOuter) ringOuter.sizeDelta = new Vector2(_currentSize, _currentSize);
        if (ringInner) ringInner.sizeDelta = new Vector2(innerSize, innerSize);
    }

    private void UpdateCount()
    {
        if (countText) countText.text = $"{_hitCount} / {requiredHits}";
    }
}