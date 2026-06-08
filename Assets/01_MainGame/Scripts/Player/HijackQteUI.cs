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

using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class HijackQTEUI : MonoBehaviour
{
    [Header("=== 参照 ===")]
    [SerializeField] private GameObject _qtePanel;
    [SerializeField] private RectTransform _ringOuter;
    [SerializeField] private RectTransform _ringInner;
    [SerializeField] private Image _ringOuterImage;
    [SerializeField] private TextMeshProUGUI _countText;
    [SerializeField] private TextMeshProUGUI _hintText;
    [SerializeField] private TextMeshProUGUI _resultText;   // 成功・失敗テキスト

    [Header("=== QTE パラメーター ===")]
    [SerializeField] private float _outerStartSize = 400f;
    [SerializeField] private float _innerSize = 120f;
    [SerializeField] private float _shrinkSpeed = 180f;   // px/秒
    [SerializeField] private float _hitWindow = 30f;    // 成功ウィンドウ幅
    [SerializeField] private int _requiredHits = 1;

    [Header("=== 色 ===")]
    [SerializeField] private Color _normalColor = Color.white;
    [SerializeField] private Color _successColor = Color.green;
    [SerializeField] private Color _failColor = Color.red;

    [Header("=== 結果テキスト ===")]
    [SerializeField] private string _successMessage = "SUCCESS!";
    [SerializeField] private string _failMessage    = "FAILED!";
    [SerializeField] private float  _resultDisplayTime = 0.8f;   // 表示時間（秒）

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
        _qtePanel?.SetActive(false);
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

        _currentSize -= _shrinkSpeed * Time.deltaTime;
        ApplySizes();

        // リングが緑ゾーンを通過してしまったら自動ミス
        if (_currentSize < _innerSize - _hitWindow)
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

        _qtePanel?.SetActive(true);
        if (_resultText) _resultText.gameObject.SetActive(false);
        UpdateCount();
        if (_hintText) _hintText.text = "Press Space!";
        BeginBeat();
    }

    // ─────────────────────────────────────────
    // ボタン入力（PlayerStateMachine の OnHijackInput から転送）
    // ─────────────────────────────────────────

    public void OnQTEPress()
    {
        if (!_active || _waitNext) return;

        if (Mathf.Abs(_currentSize - _innerSize) <= _hitWindow)
            RegisterHit();
        else
            RegisterMiss();
    }

    // ─────────────────────────────────────────
    // 内部処理
    // ─────────────────────────────────────────

    private void BeginBeat()
    {
        _currentSize = _outerStartSize;
        _waitNext = false;
        if (_ringOuterImage) _ringOuterImage.color = _normalColor;
        ApplySizes();
    }

    private void RegisterHit()
    {
        _hitCount++;
        UpdateCount();
        Flash(_successColor);
        Debug.Log($"[QTE] Hit {_hitCount}/{_requiredHits}");

        if (_hitCount >= _requiredHits)
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
        Flash(_failColor);
        Debug.Log("[QTE] Miss");
        End(false);
    }

    private void End(bool success)
    {
        _active = false;
        StartCoroutine(ShowResultThenEnd(success));
    }

    private IEnumerator ShowResultThenEnd(bool success)
    {
        // カウント・ヒントを隠して結果テキストを表示
        if (_countText) _countText.gameObject.SetActive(false);
        if (_hintText)  _hintText.gameObject.SetActive(false);

        if (_resultText)
        {
            _resultText.text  = success ? _successMessage : _failMessage;
            _resultText.color = success ? _successColor   : _failColor;
            _resultText.gameObject.SetActive(true);
        }

        yield return new WaitForSecondsRealtime(_resultDisplayTime);

        // UI を元に戻してパネルを閉じる
        if (_countText) _countText.gameObject.SetActive(true);
        if (_hintText)  _hintText.gameObject.SetActive(true);
        if (_resultText) _resultText.gameObject.SetActive(false);
        _qtePanel?.SetActive(false);

        if (success) _hijackState?.OnQTESuccess();
        else         _hijackState?.OnQTEFail();
    }

    private void Flash(Color c)
    {
        if (_ringOuterImage) _ringOuterImage.color = c;
    }

    private void ApplySizes()
    {
        if (_ringOuter) _ringOuter.sizeDelta = new Vector2(_currentSize, _currentSize);
        if (_ringInner) _ringInner.sizeDelta = new Vector2(_innerSize, _innerSize);
    }

    private void UpdateCount()
    {
        if (_countText) _countText.text = $"{_hitCount} / {_requiredHits}";
    }
}