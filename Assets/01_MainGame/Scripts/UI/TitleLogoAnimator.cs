// ============================================================
// TitleLogoAnimator.cs  (DOTween 版)
// タイトルロゴ「PHANTOM RISE」の登場演出＋アイドル演出。
//
// 使い方（Title シーン）:
//   Canvas
//    └ LogoRoot            ← このスクリプトを付ける（RectTransform）
//        ├ Streaks         ← Image: PhantomRise_L_Streaks
//        ├ Rise            ← Image: PhantomRise_L_Rise
//        ├ Phantom         ← Image: PhantomRise_L_Phantom
//        ├ Ghost           ← Image: PhantomRise_L_Ghost
//        └ ShineMask       ← （任意）Image + Mask, 中に Shine を入れる
//            └ Shine       ← Image: PhantomRise_L_Shine
//
//   ・子はすべて同じサイズ（1600x920）／Anchor 中央／Pos 0,0 にする。
//     レイヤーは元から位置が合わせてあるので、ズラす必要はない。
//
// 演出:
//   登場 … 流線 → RISE 落下＋着地パンチ → PHANTOM スライドイン
//          → 幽霊が降りてくる → シャイン一閃
//   常時 … 幽霊ふわふわ／ロゴの微呼吸／一定間隔でシャイン
//
// 実装メモ:
//   ・全 Tween は SetUpdate(true) で timeScale の影響を受けない
//     （ポーズ中のタイトルでも動く）
//   ・OnDestroy / 再生前に必ず Kill する（Tween がオブジェクトより長生きしない）
// ============================================================

using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class TitleLogoAnimator : MonoBehaviour
{
    [Header("=== レイヤー ===")]
    [SerializeField] private RectTransform streaks;
    [SerializeField] private RectTransform rise;
    [SerializeField] private RectTransform phantom;
    [SerializeField] private RectTransform ghost;

    /// <summary>シャイン（無くても動く）</summary>
    [SerializeField] private RectTransform shine;

    [Header("=== 登場演出 ===")]
    [SerializeField] private bool playIntroOnStart = true;

    [Tooltip("演出開始までの間")]
    [SerializeField] private float introDelay = 0.15f;

    /// <summary>RISE が落ちてくる高さ</summary>
    [SerializeField] private float riseDropHeight = 420f;
    [SerializeField] private float riseDropDuration = 0.42f;

    /// <summary>着地時のスケールパンチ量</summary>
    [SerializeField] private float landPunch = 0.14f;
    [SerializeField] private float landPunchDuration = 0.30f;

    [Header("=== アイドル演出 ===")]
    [SerializeField] private float ghostFloatAmp = 14f;
    [Tooltip("片道にかかる秒数")]
    [SerializeField] private float ghostFloatDuration = 1.0f;

    [SerializeField] private float breathAmp = 0.012f;
    [SerializeField] private float breathDuration = 1.4f;

    /// <summary>シャインを流す間隔（秒）。0 以下でシャイン無し</summary>
    [SerializeField] private float shineInterval = 4.5f;
    [SerializeField] private float shineDuration = 0.7f;

    /// <summary>シャインの移動範囲（ロゴ幅より少し広めに）</summary>
    [SerializeField] private float shineTravel = 1900f;

    // ─── 初期値のキャッシュ
    private Vector2 _riseHome, _phantomHome, _ghostHome;
    private Vector3 _rootHome;

    // ─── Tween 参照（Kill 用）
    private Sequence _intro;
    private Sequence _shineLoop;
    private Tween _ghostBob;
    private Tween _breath;

    // ─────────────────────────────────────────
    private void Awake()
    {
        if (rise    != null) _riseHome    = rise.anchoredPosition;
        if (phantom != null) _phantomHome = phantom.anchoredPosition;
        if (ghost   != null) _ghostHome   = ghost.anchoredPosition;
        _rootHome = transform.localScale;

        SetAlpha(shine, 0f);
    }

    private void Start()
    {
        if (playIntroOnStart) PlayIntro();
        else                  StartIdle();
    }

    private void OnDestroy() => KillAll();

    // ─────────────────────────────────────────
    // 登場演出
    // ─────────────────────────────────────────

    /// <summary>登場演出を再生する（ボタンなどから呼んでもよい）</summary>
    public void PlayIntro()
    {
        KillAll();

        // ── 初期状態
        SetAlpha(streaks, 0f);
        SetAlpha(rise, 0f);
        SetAlpha(phantom, 0f);
        SetAlpha(ghost, 0f);
        transform.localScale = _rootHome;

        if (rise    != null) rise.anchoredPosition    = _riseHome    + Vector2.up * riseDropHeight;
        if (phantom != null) phantom.anchoredPosition = _phantomHome + Vector2.up * 60f;
        if (ghost   != null) ghost.anchoredPosition   = _ghostHome   + Vector2.up * 120f;

        // ── タイムライン（Insert でカーソルを進めていく）
        _intro = DOTween.Sequence().SetUpdate(true);
        float t = introDelay;

        // 1) 流線がふわっと出る ＆ RISE 落下
        Ins(_intro, t, Fade(streaks, 1f, 0.45f));
        Ins(_intro, t, Fade(rise, 1f, 0.18f));
        Ins(_intro, t, Move(rise, _riseHome, riseDropDuration, Ease.OutBack));
        t += riseDropDuration;

        // 2) 着地のパンチ
        Ins(_intro, t, transform
            .DOPunchScale(_rootHome * landPunch, landPunchDuration, 6, 0.6f)
            .SetUpdate(true));
        t += 0.10f;

        // 3) PHANTOM がスライドイン
        Ins(_intro, t, Fade(phantom, 1f, 0.22f));
        Ins(_intro, t, Move(phantom, _phantomHome, 0.34f, Ease.OutCubic));
        t += 0.34f;

        // 4) 幽霊が降りてくる
        Ins(_intro, t, Fade(ghost, 1f, 0.28f));
        Ins(_intro, t, Move(ghost, _ghostHome, 0.40f, Ease.OutBack));
        t += 0.40f;

        // 5) シャイン一閃 → アイドルへ
        _intro.InsertCallback(t, PlayShine);
        _intro.OnComplete(StartIdle);
    }

    // ─────────────────────────────────────────
    // アイドル演出
    // ─────────────────────────────────────────
    private void StartIdle()
    {
        // 幽霊ふわふわ（Yoyo で往復）
        if (ghost != null)
        {
            ghost.anchoredPosition = _ghostHome;
            _ghostBob = ghost
                .DOAnchorPosY(_ghostHome.y + ghostFloatAmp, ghostFloatDuration)
                .SetEase(Ease.InOutSine)
                .SetLoops(-1, LoopType.Yoyo)
                .SetUpdate(true);
        }

        // ロゴ全体の微呼吸
        _breath = transform
            .DOScale(_rootHome * (1f + breathAmp), breathDuration)
            .SetEase(Ease.InOutSine)
            .SetLoops(-1, LoopType.Yoyo)
            .SetUpdate(true);

        // 一定間隔でシャイン
        if (shineInterval > 0f && shine != null)
        {
            _shineLoop = DOTween.Sequence().SetUpdate(true);
            _shineLoop.AppendInterval(shineInterval);
            _shineLoop.AppendCallback(PlayShine);
            _shineLoop.SetLoops(-1);
        }
    }

    /// <summary>シャインを 1 回流す</summary>
    public void PlayShine()
    {
        if (shine == null) return;

        Image img = shine.GetComponent<Image>();
        float half = shineTravel * 0.5f;

        shine.anchoredPosition = new Vector2(-half, 0f);
        SetAlpha(shine, 0f);

        Sequence s = DOTween.Sequence().SetUpdate(true);
        s.Append(shine.DOAnchorPosX(half, shineDuration).SetEase(Ease.InOutCubic).SetUpdate(true));

        if (img != null)
        {
            // 入りでフェードイン、抜けでフェードアウト
            s.Join(img.DOFade(1f, shineDuration * 0.30f).SetUpdate(true));
            s.Insert(shineDuration * 0.60f,
                     img.DOFade(0f, shineDuration * 0.40f).SetUpdate(true));
        }
    }

    // ─────────────────────────────────────────
    // ヘルパー
    // ─────────────────────────────────────────

    /// <summary>null Tween を Insert しても落ちないようにする</summary>
    private static void Ins(Sequence seq, float at, Tween tw)
    {
        if (seq != null && tw != null) seq.Insert(at, tw);
    }

    private Tween Move(RectTransform rt, Vector2 target, float dur, Ease ease)
    {
        if (rt == null) return null;
        return rt.DOAnchorPos(target, dur).SetEase(ease).SetUpdate(true);
    }

    private Tween Fade(RectTransform rt, float target, float dur)
    {
        if (rt == null) return null;

        Image img = rt.GetComponent<Image>();
        if (img == null) return null;

        return img.DOFade(target, dur).SetUpdate(true);
    }

    private void SetAlpha(RectTransform rt, float a)
    {
        if (rt == null) return;

        Image img = rt.GetComponent<Image>();
        if (img == null) return;

        Color c = img.color;
        c.a = a;
        img.color = c;
    }

    /// <summary>この演出が作った Tween をすべて止める</summary>
    private void KillAll()
    {
        _intro?.Kill();      _intro = null;
        _shineLoop?.Kill();  _shineLoop = null;
        _ghostBob?.Kill();   _ghostBob = null;
        _breath?.Kill();     _breath = null;

        // Insert した個々の Tween も対象ごとに止める
        DOTween.Kill(transform);
        KillTarget(streaks);
        KillTarget(rise);
        KillTarget(phantom);
        KillTarget(ghost);
        KillTarget(shine);
    }

    private static void KillTarget(RectTransform rt)
    {
        if (rt == null) return;

        DOTween.Kill(rt);                       // DOAnchorPos 系

        Image img = rt.GetComponent<Image>();
        if (img != null) DOTween.Kill(img);     // DOFade 系
    }
}
