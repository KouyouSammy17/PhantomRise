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
//     ├── Vignette      (Image, QTE_Vignette)        ← 画面を暗くして集中させる
//     ├── HitWindow     (Image, QTE_HitWindow)       ← 許容範囲の点線リング（自動サイズ）
//     ├── RingInner     (Image, QTE_RingTarget)      ← 固定ターゲット
//     ├── RingOuter     (Image, QTE_RingOuter)       ← 縮むリング（白 = tint される）
//     ├── Burst         (Image, QTE_Burst)           ← ヒット / ミスのフィードバック
//     ├── CountText     (TextMeshPro "1/3")
//     ├── HintText      (TextMeshPro) / KeyCap       ← デバイスで差し替え（Space / Xbox B）
//     └── ResultText    (TextMeshPro "SUCCESS!")
//
// 演出は DOTween、待ち時間は UniTask（animation_stack.md の方針に合わせる）。
// リングの縮小だけは判定そのものなので Update で手動計算のまま。
// ============================================================

using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

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

    [Header("=== 追加演出（無くても動く） ===")]
    [Tooltip("許容範囲の外側＝判定が始まる位置。サイズは自動計算される")]
    [SerializeField] private RectTransform _hitWindowRing;

    [Tooltip("許容範囲の内側＝判定が終わる位置。任意（付けると帯として読める）")]
    [SerializeField] private RectTransform _hitWindowRingInner;

    [Tooltip("画面を暗くするビネット")]
    [SerializeField] private Image _vignette;

    [Tooltip("ヒット / ミス時に弾けるバースト")]
    [SerializeField] private RectTransform _burst;
    [SerializeField] private Image _burstImage;

    [Tooltip("押すボタンのキーキャップ（点滅させる）")]
    [SerializeField] private RectTransform _keyCap;

    [Tooltip("キーキャップの画像。未設定なら _keyCap から探す")]
    [SerializeField] private Image _keyCapImage;

    [Header("=== 押すボタンの表示（デバイス別）===")]
    // QTE の押下は Hijack アクション（Keyboard/space・XInput/buttonEast＝B）。
    // パッドで遊んでいるのに "Press Space!" と出ないよう、デバイスで出し分ける。
    [SerializeField] private string _keyboardHint = "Press Space!";
    [SerializeField] private string _gamepadHint  = "Press B!";

    [Tooltip("キーボード用のキーキャップ。未設定ならキーボード時はキーキャップを隠す")]
    [SerializeField] private Sprite _keyboardKeyCap;

    [Tooltip("ゲームパッド用のキーキャップ（Xbox の B）")]
    [SerializeField] private Sprite _gamepadKeyCap;

    [Tooltip(".inputactions のコントロールスキーム名")]
    [SerializeField] private string _gamepadSchemeName = "Gamepad";

    [Header("=== QTE パラメーター ===")]
    [SerializeField] private float _outerStartSize = 400f;
    [SerializeField] private float _innerSize = 120f;
    [SerializeField] private float _shrinkSpeed = 180f;   // px/秒
    [SerializeField] private float _hitWindow = 30f;    // 成功ウィンドウ幅
    [SerializeField] private int _requiredHits = 1;

    [Tooltip("ON にすると timeScale の影響を受けずに縮む（スロー演出を入れる場合）")]
    [SerializeField] private bool _useUnscaledTime = false;

    [Header("=== 色 ===")]
    [SerializeField] private Color _normalColor = Color.white;
    [SerializeField] private Color _successColor = Color.green;
    [SerializeField] private Color _failColor = Color.red;

    [Header("=== 結果テキスト ===")]
    [SerializeField] private string _successMessage = "SUCCESS!";
    [SerializeField] private string _failMessage    = "FAILED!";
    [SerializeField] private float  _resultDisplayTime = 0.8f;   // 表示時間（秒）

    [Header("=== 演出パラメーター ===")]
    [SerializeField] private float _burstScale = 1.6f;
    [SerializeField] private float _burstDuration = 0.38f;
    [SerializeField] private float _vignetteFadeTime = 0.22f;
    [SerializeField] private float _vignetteAlpha = 1f;
    [SerializeField] private float _ringFadeInTime = 0.12f;

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

    private CancellationTokenSource _cts;
    private Tween _keyCapPulse;
    private Vector3 _burstBaseScale = Vector3.one;
    private Vector3 _resultBaseScale = Vector3.one;

    /// <summary>ヒント表示のデバイス判定に使う（遅延取得）</summary>
    private PlayerInput _playerInput;

    /// <summary>今 QTE 中の敵。結果 SE をこの敵の EnemyAudio で鳴らす</summary>
    private EnemyController _currentEnemy;

    private float Delta => _useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;

    // ─────────────────────────────────────────
    // 初期化
    // ─────────────────────────────────────────

    public void Initialize(HijackState state)
    {
        _hijackState = state;
        _hijackState.OnQTEStart += StartQTE;
        _qtePanel?.SetActive(false);
    }

    private void Awake()
    {
        if (_burst != null) _burstBaseScale = _burst.localScale;

        // プレハブで設定されたスケールを覚えておく。
        // 決め打ちで 1 に戻すと、2 倍に設定してあるテキストが
        // 一度表示しただけで半分のサイズになってしまう。
        if (_resultText != null) _resultBaseScale = _resultText.rectTransform.localScale;

        ApplyHitWindowSize();
        SetAlpha(_burstImage, 0f);
        SetAlpha(_vignette, 0f);
    }

    private void OnDestroy()
    {
        if (_hijackState != null)
            _hijackState.OnQTEStart -= StartQTE;

        CancelPending();
        KillTweens();
    }

#if UNITY_EDITOR
    // Inspector で数値をいじったら点線リングも即追従させる
    private void OnValidate() => ApplyHitWindowSize();
#endif

    // ─────────────────────────────────────────
    // Unity Update
    // ─────────────────────────────────────────

    private void Update()
    {
        if (!_active) return;

        if (_waitNext)
        {
            _waitTimer -= Delta;
            if (_waitTimer <= 0f) BeginBeat();
            return;
        }

        _currentSize -= _shrinkSpeed * Delta;
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
        _currentEnemy = enemy;   // 結果 SE を鳴らすのに使う
        _hitCount = 0;
        _active = true;
        _waitNext = false;

        CancelPending();
        KillTweens();

        _qtePanel?.SetActive(true);
        if (_resultText) _resultText.gameObject.SetActive(false);
        if (_countText)  _countText.gameObject.SetActive(true);
        ApplyDeviceHint();

        ApplyHitWindowSize();
        UpdateCount();

        // ビネットをふわっと出す
        FadeVignette(_vignetteAlpha);

        // キーキャップを脈打たせる
        if (_keyCap != null)
        {
            _keyCap.localScale = Vector3.one;
            _keyCapPulse = _keyCap
                .DOScale(1.08f, 0.45f)
                .SetEase(Ease.InOutSine)
                .SetLoops(-1, LoopType.Yoyo)
                .SetUpdate(true);
        }

        SetAlpha(_burstImage, 0f);
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

        if (_ringOuterImage)
        {
            _ringOuterImage.color = _normalColor;

            // 出現をわずかにフェードさせて「ポンと出た」感を消す
            Color c = _ringOuterImage.color;
            c.a = 0f;
            _ringOuterImage.color = c;
            _ringOuterImage.DOFade(_normalColor.a, _ringFadeInTime).SetUpdate(true);
        }

        ApplySizes();
    }

    private void RegisterHit()
    {
        _hitCount++;
        UpdateCount();
        Flash(_successColor);
        PlayBurst(_successColor);
        PunchTarget();

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
        PlayBurst(_failColor);

        Debug.Log("[QTE] Miss");
        End(false);
    }

    private void End(bool success)
    {
        _active = false;

        CancelPending();
        _cts = CancellationTokenSource.CreateLinkedTokenSource(
            this.GetCancellationTokenOnDestroy());

        ShowResultThenEndAsync(success, _cts.Token).Forget();
    }

    private async UniTaskVoid ShowResultThenEndAsync(bool success, CancellationToken token)
    {
        try
        {
            // カウント・ヒントを隠して結果テキストを表示
            if (_countText) _countText.gameObject.SetActive(false);
            if (_hintText)  _hintText.gameObject.SetActive(false);

            _keyCapPulse?.Kill();
            _keyCapPulse = null;
            if (_keyCap != null) _keyCap.localScale = Vector3.one;

            // 結果表示と同時に SE を鳴らす
            PlayResultSE(success);

            if (_resultText)
            {
                _resultText.text  = success ? _successMessage : _failMessage;
                _resultText.color = success ? _successColor   : _failColor;
                _resultText.gameObject.SetActive(true);

                // 結果テキストをドンと出す（プレハブのスケールを基準にする）
                RectTransform rt = _resultText.rectTransform;
                DOTween.Kill(rt);
                rt.localScale = _resultBaseScale * 0.7f;
                rt.DOScale(_resultBaseScale, 0.28f).SetEase(Ease.OutBack).SetUpdate(true);
            }

            await UniTask.Delay(
                TimeSpan.FromSeconds(_resultDisplayTime),
                DelayType.UnscaledDeltaTime,
                cancellationToken: token);

            // ビネットを消してからパネルを閉じる
            FadeVignette(0f);

            await UniTask.Delay(
                TimeSpan.FromSeconds(_vignetteFadeTime),
                DelayType.UnscaledDeltaTime,
                cancellationToken: token);

            // UI を元に戻してパネルを閉じる
            if (_countText)  _countText.gameObject.SetActive(true);
            if (_hintText)   _hintText.gameObject.SetActive(true);
            if (_resultText) _resultText.gameObject.SetActive(false);
            _qtePanel?.SetActive(false);

            if (success) _hijackState?.OnQTESuccess();
            else         _hijackState?.OnQTEFail();
        }
        catch (OperationCanceledException)
        {
            // 破棄された / 次の QTE が始まった。何もしない。
        }
    }

    // ─────────────────────────────────────────
    // 演出
    // ─────────────────────────────────────────

    private void Flash(Color c)
    {
        if (_ringOuterImage) _ringOuterImage.color = c;
    }

    /// <summary>ヒット / ミス時のバースト。白スプライトを結果色に着色して弾けさせる。</summary>
    private void PlayBurst(Color color)
    {
        if (_burst == null || _burstImage == null) return;

        DOTween.Kill(_burst);
        DOTween.Kill(_burstImage);

        _burstImage.color = new Color(color.r, color.g, color.b, 1f);
        _burst.localScale = _burstBaseScale * 0.6f;

        _burst.DOScale(_burstBaseScale * _burstScale, _burstDuration)
              .SetEase(Ease.OutCubic)
              .SetUpdate(true);

        _burstImage.DOFade(0f, _burstDuration)
                   .SetEase(Ease.InQuad)
                   .SetUpdate(true);
    }

    /// <summary>ターゲットリングを軽く弾ませる（当たった手応え）</summary>
    private void PunchTarget()
    {
        if (_ringInner == null) return;

        DOTween.Kill(_ringInner);
        _ringInner.localScale = Vector3.one;
        _ringInner.DOPunchScale(Vector3.one * 0.18f, 0.30f, 8, 0.7f).SetUpdate(true);
    }

    private void FadeVignette(float target)
    {
        if (_vignette == null) return;

        DOTween.Kill(_vignette);
        _vignette.DOFade(target, _vignetteFadeTime).SetUpdate(true);
    }

    // ─────────────────────────────────────────
    // ヘルパー
    // ─────────────────────────────────────────

    private void ApplySizes()
    {
        if (_ringOuter) _ringOuter.sizeDelta = new Vector2(_currentSize, _currentSize);
        if (_ringInner) _ringInner.sizeDelta = new Vector2(_innerSize, _innerSize);
    }

    /// <summary>
    /// 点線リングを実際の判定範囲に合わせる。
    ///
    /// 判定は Mathf.Abs(_currentSize - _innerSize) &lt;= _hitWindow なので、
    /// 成功する直径は (_innerSize - _hitWindow) 〜 (_innerSize + _hitWindow)。
    /// 外側リング = 判定が開く瞬間、内側リング = 判定が閉じる瞬間。
    /// </summary>
    private void ApplyHitWindowSize()
    {
        if (_hitWindowRing != null)
        {
            float outer = _innerSize + _hitWindow;
            _hitWindowRing.sizeDelta = new Vector2(outer, outer);
        }

        if (_hitWindowRingInner != null)
        {
            float inner = Mathf.Max(0f, _innerSize - _hitWindow);
            _hitWindowRingInner.sizeDelta = new Vector2(inner, inner);
        }
    }

    private void UpdateCount()
    {
        if (_countText) _countText.text = $"{_hitCount} / {_requiredHits}";
    }

    // ─────────────────────────────────────────
    // 押すボタンの表示（デバイス別）
    // ─────────────────────────────────────────

    /// <summary>
    /// 今使っているデバイスに合わせて、ヒント文とキーキャップ画像を差し替える。
    /// QTE 開始のたびに呼ぶ（途中でパッドに持ち替えても次回から合う）。
    /// </summary>
    private void ApplyDeviceHint()
    {
        bool gamepad = IsUsingGamepad();

        if (_hintText != null)
        {
            _hintText.gameObject.SetActive(true);
            _hintText.text = gamepad ? _gamepadHint : _keyboardHint;
        }

        Sprite cap = gamepad ? _gamepadKeyCap : _keyboardKeyCap;
        Image  img = ResolveKeyCapImage();

        if (img != null)
        {
            // そのデバイス用の画像が無いときは、
            // 別デバイスのボタンを出したままにせず隠す
            img.gameObject.SetActive(cap != null);
            if (cap != null) img.sprite = cap;
        }
    }

    /// <summary>
    /// 成功 / 失敗の SE を鳴らす。敵の EnemyAudio（クリップ設定済み）をそのまま使う。
    ///
    /// 以前は EnemyController.BecomeHijacked / AlertChase の中で鳴らしていたが、
    /// どちらも結果表示・パネルを閉じる処理が終わってから呼ばれるので、
    /// SE が 1〜1.7 秒遅れて（UI が消えた後に）鳴っていた。
    /// </summary>
    private void PlayResultSE(bool success)
    {
        if (_currentEnemy == null) return;

        EnemyAudio audio = _currentEnemy.GetComponent<EnemyAudio>();
        if (audio == null) return;

        if (success) audio.PlayQTESSE();
        else         audio.PlayQTEFSE();
    }

    private Image ResolveKeyCapImage()
    {
        if (_keyCapImage == null && _keyCap != null)
            _keyCapImage = _keyCap.GetComponent<Image>();

        return _keyCapImage;
    }

    /// <summary>
    /// パッドで操作中か。
    /// PlayerInput のコントロールスキームで判定し、
    /// まだ確定していない場合はパッドが繋がっていればパッド扱いにする
    /// （パッド推奨のため、キーボード表示に倒さない）。
    /// </summary>
    private bool IsUsingGamepad()
    {
        if (_playerInput == null)
        {
            PlayerStateMachine player = FindAnyObjectByType<PlayerStateMachine>();
            if (player != null) _playerInput = player.GetComponent<PlayerInput>();
        }

        string scheme = _playerInput != null ? _playerInput.currentControlScheme : null;
        if (!string.IsNullOrEmpty(scheme))
            return scheme == _gamepadSchemeName;

        return Gamepad.current != null;
    }

    private static void SetAlpha(Image img, float a)
    {
        if (img == null) return;

        Color c = img.color;
        c.a = a;
        img.color = c;
    }

    private void CancelPending()
    {
        if (_cts == null) return;

        _cts.Cancel();
        _cts.Dispose();
        _cts = null;
    }

    private void KillTweens()
    {
        _keyCapPulse?.Kill();
        _keyCapPulse = null;

        if (_burst != null)       DOTween.Kill(_burst);
        if (_burstImage != null)  DOTween.Kill(_burstImage);
        if (_vignette != null)    DOTween.Kill(_vignette);
        if (_ringInner != null)   DOTween.Kill(_ringInner);
        if (_keyCap != null)      DOTween.Kill(_keyCap);
        if (_ringOuterImage != null) DOTween.Kill(_ringOuterImage);
        if (_resultText != null)  DOTween.Kill(_resultText.rectTransform);
    }
}
