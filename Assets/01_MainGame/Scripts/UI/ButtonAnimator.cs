// ============================================================
// ButtonAnimator.cs  (DOTween 版)
// ボタンのホバー / 選択 / 押下 / クリックの演出。
//
// Button.prefab に付けてある。
// Selectable と同じ GameObject に置くこと。
//
// マウス（PointerEnter / Exit）とコントローラー（Select / Deselect）の
// どちらでも同じ「ハイライト」状態になるよう両方拾う。
//
// 実装メモ:
//   ・全 Tween は SetUpdate(true)。
//     ポーズ UI は Time.timeScale = 0 で開くので、
//     これが無いとポーズ中にボタンが固まる。
//   ・OnDisable / OnDestroy で必ず Kill してスケールを戻す
//     （途中で非表示にされても縮んだまま残らない）
// ============================================================

using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(RectTransform))]
public class ButtonAnimator : MonoBehaviour,
    IPointerEnterHandler, IPointerExitHandler,
    IPointerDownHandler, IPointerUpHandler,
    ISelectHandler, IDeselectHandler, ISubmitHandler
{
    // ─────────────────────────────────────────
    // Inspector
    // ─────────────────────────────────────────

    [Header("=== ハイライト（ホバー / 選択）===")]
    [SerializeField] private float hoverScale = 1.06f;
    [SerializeField] private float hoverDuration = 0.16f;
    [SerializeField] private Ease hoverEase = Ease.OutBack;

    [Header("=== 押し込み ===")]
    [SerializeField] private float pressScale = 0.94f;
    [SerializeField] private float pressDuration = 0.07f;

    [Header("=== クリック ===")]
    [Tooltip("決定したときに弾む量。0 で無効")]
    [SerializeField] private float clickPunch = 0.18f;
    [SerializeField] private float clickPunchDuration = 0.28f;

    [Header("=== 効果音 ===")]
    [Tooltip("選択音・決定音を鳴らす。クリップは UISoundPlayer 側にまとめてある")]
    [SerializeField] private bool playSounds = true;

    [Header("=== アイドル（通常時のゆっくりした呼吸）===")]
    [SerializeField] private bool idlePulse = false;
    [SerializeField] private float idleAmp = 0.02f;
    [SerializeField] private float idleDuration = 1.2f;

    // ─────────────────────────────────────────
    // 内部
    // ─────────────────────────────────────────

    private Selectable _selectable;
    private Vector3 _homeScale;

    private bool _hovered;
    private bool _selected;
    private bool _pressed;

    private Tween _scaleTween;
    private Tween _idleTween;
    private Tween _punchTween;

    /// <summary>ホバーも選択も押下もされていない、素の状態か</summary>
    private bool IsNormal => !_hovered && !_selected && !_pressed;

    private bool Interactable => _selectable == null || _selectable.IsInteractable();

    // ─────────────────────────────────────────
    // Unity ライフサイクル
    // ─────────────────────────────────────────

    private void Awake()
    {
        _selectable = GetComponent<Selectable>();
        _homeScale  = transform.localScale;
    }

    private void OnEnable()
    {
        // 前回の状態を持ち越さない（プールや SetActive の使い回し対策）
        _hovered = _selected = _pressed = false;
        transform.localScale = _homeScale;

        if (idlePulse) StartIdle();
    }

    private void OnDisable()
    {
        KillAll();
        transform.localScale = _homeScale;
    }

    private void OnDestroy() => KillAll();

    // ─────────────────────────────────────────
    // イベント
    // ─────────────────────────────────────────

    public void OnPointerEnter(PointerEventData e)
    {
        // カーソルを消しているときはホバーを無視する。
        // 見えないマウスが少し動いただけで、パッドで選んでいるボタンとは
        // 別のボタンが光り、選択音まで鳴ってしまうため。
        if (!Cursor.visible) return;

        _hovered = true;
        PlaySelectSE();
        Refresh();
    }

    public void OnPointerExit(PointerEventData e)
    {
        // 押したままカーソルが外れたケースも戻す
        _hovered = false;
        _pressed = false;
        Refresh();
    }

    public void OnSelect(BaseEventData e)
    {
        _selected = true;
        PlaySelectSE();
        Refresh();
    }

    public void OnDeselect(BaseEventData e)
    {
        _selected = false;
        Refresh();
    }

    public void OnPointerDown(PointerEventData e)
    {
        _pressed = true;
        Refresh();
    }

    public void OnPointerUp(PointerEventData e)
    {
        bool wasPressed = _pressed;
        _pressed = false;
        Refresh();

        // ボタンの上で離したときだけクリック演出
        if (wasPressed && _hovered) PlayClick();
    }

    /// <summary>コントローラーの決定ボタン</summary>
    public void OnSubmit(BaseEventData e) => PlayClick();

    // ─────────────────────────────────────────
    // 演出
    // ─────────────────────────────────────────

    /// <summary>今の状態のスケール倍率</summary>
    private float TargetScale()
    {
        return _pressed                ? pressScale
             : (_hovered || _selected) ? hoverScale
             :                           1f;
    }

    /// <summary>今の状態に合わせてスケールを合わせ直す</summary>
    private void Refresh()
    {
        if (!Interactable) return;

        // パンチも同じ localScale を触るので、必ず全部止めてから動かす
        KillTweens();

        float duration = _pressed ? pressDuration : hoverDuration;
        Ease  ease     = _pressed ? Ease.OutQuad  : hoverEase;

        _scaleTween = transform
            .DOScale(_homeScale * TargetScale(), duration)
            .SetEase(ease)
            .SetUpdate(true)
            .OnComplete(() => { if (idlePulse && IsNormal) StartIdle(); });
    }

    /// <summary>選択音。押せないボタンでは鳴らさない。</summary>
    private void PlaySelectSE()
    {
        if (playSounds && Interactable) UISoundPlayer.PlaySelect();
    }

    /// <summary>決定時に弾ませる。ボタンの onClick からも呼べる。</summary>
    public void PlayClick()
    {
        if (!Interactable) return;

        // 決定音は演出（clickPunch = 0）を切っていても鳴らす
        if (playSounds) UISoundPlayer.PlayConfirm();

        if (clickPunch <= 0f) return;

        // パンチは開始時のスケールに戻して終わるので、
        // 先に今の状態のスケールへ合わせておく
        KillTweens();
        transform.localScale = _homeScale * TargetScale();

        _punchTween = transform
            .DOPunchScale(_homeScale * clickPunch, clickPunchDuration, 6, 0.7f)
            .SetUpdate(true)
            .OnComplete(Refresh);
    }

    private void StartIdle()
    {
        _idleTween?.Kill();
        _idleTween = transform
            .DOScale(_homeScale * (1f + idleAmp), idleDuration)
            .SetEase(Ease.InOutSine)
            .SetLoops(-1, LoopType.Yoyo)
            .SetUpdate(true);
    }

    private void KillTweens()
    {
        _scaleTween?.Kill();  _scaleTween = null;
        _idleTween?.Kill();   _idleTween  = null;
        _punchTween?.Kill();  _punchTween = null;
    }

    private void KillAll()
    {
        KillTweens();
        DOTween.Kill(transform);
    }
}
