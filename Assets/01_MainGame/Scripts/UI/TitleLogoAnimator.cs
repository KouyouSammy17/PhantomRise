// ============================================================
// TitleLogoAnimator.cs
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
//   ・各 Image に CanvasGroup は不要（Image.color.a を直接操作）。
//
// 演出:
//   登場 … 流線 → RISE 落下＋着地バウンド → PHANTOM スライドイン
//          → 幽霊が降りてくる → シャイン一閃
//   常時 … 幽霊ふわふわ／ロゴの微呼吸／一定間隔でシャイン
// ============================================================

using System.Collections;
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

    /// <summary>RISE が落ちてくる高さ</summary>
    [SerializeField] private float riseDropHeight = 420f;

    /// <summary>着地時のスケールパンチ量</summary>
    [SerializeField] private float landPunch = 0.14f;

    [Header("=== アイドル演出 ===")]
    [SerializeField] private float ghostFloatAmp = 14f;
    [SerializeField] private float ghostFloatSpeed = 1.6f;
    [SerializeField] private float breathAmp = 0.012f;
    [SerializeField] private float breathSpeed = 1.1f;

    /// <summary>シャインを流す間隔（秒）。0 以下でシャイン無し</summary>
    [SerializeField] private float shineInterval = 4.5f;
    [SerializeField] private float shineDuration = 0.7f;

    /// <summary>シャインの移動範囲（ロゴ幅より少し広めに）</summary>
    [SerializeField] private float shineTravel = 1900f;

    // 初期値のキャッシュ
    private Vector2 _riseHome, _phantomHome, _ghostHome, _streaksHome;
    private Vector3 _rootHome;
    private bool _introDone;

    // ─────────────────────────────────────────
    private void Awake()
    {
        if (rise     != null) _riseHome    = rise.anchoredPosition;
        if (phantom  != null) _phantomHome = phantom.anchoredPosition;
        if (ghost    != null) _ghostHome   = ghost.anchoredPosition;
        if (streaks  != null) _streaksHome = streaks.anchoredPosition;
        _rootHome = transform.localScale;

        if (shine != null) SetAlpha(shine, 0f);
    }

    private void Start()
    {
        if (playIntroOnStart) PlayIntro();
        else                  _introDone = true;
    }

    /// <summary>登場演出を再生する（ボタンなどから呼んでもよい）</summary>
    public void PlayIntro()
    {
        StopAllCoroutines();
        StartCoroutine(IntroRoutine());
    }

    // ─────────────────────────────────────────
    // 登場演出
    // ─────────────────────────────────────────
    private IEnumerator IntroRoutine()
    {
        _introDone = false;

        // 初期状態へ
        SetAlpha(streaks, 0f);
        SetAlpha(rise, 0f);
        SetAlpha(phantom, 0f);
        SetAlpha(ghost, 0f);

        if (rise != null)
            rise.anchoredPosition = _riseHome + Vector2.up * riseDropHeight;
        if (phantom != null)
            phantom.anchoredPosition = _phantomHome + Vector2.up * 60f;
        if (ghost != null)
            ghost.anchoredPosition = _ghostHome + Vector2.up * 120f;

        yield return new WaitForSeconds(0.15f);

        // 1) 流線がふわっと出る
        if (streaks != null)
            StartCoroutine(FadeTo(streaks, 1f, 0.45f));

        // 2) RISE が落下 → 着地
        if (rise != null)
        {
            StartCoroutine(FadeTo(rise, 1f, 0.18f));
            yield return Move(rise, _riseHome, 0.42f, EaseOutBack);
            yield return Punch(transform, landPunch, 0.28f);
        }

        // 3) PHANTOM がスライドイン
        if (phantom != null)
        {
            StartCoroutine(FadeTo(phantom, 1f, 0.22f));
            yield return Move(phantom, _phantomHome, 0.34f, EaseOutCubic);
        }

        // 4) 幽霊が降りてくる
        if (ghost != null)
        {
            StartCoroutine(FadeTo(ghost, 1f, 0.28f));
            yield return Move(ghost, _ghostHome, 0.40f, EaseOutBack);
        }

        // 5) シャイン一閃
        yield return ShineOnce();

        _introDone = true;
        StartCoroutine(IdleRoutine());
        if (shineInterval > 0f) StartCoroutine(ShineLoop());
    }

    // ─────────────────────────────────────────
    // アイドル演出
    // ─────────────────────────────────────────
    private IEnumerator IdleRoutine()
    {
        float t = 0f;
        while (true)
        {
            t += Time.unscaledDeltaTime;

            if (ghost != null)
                ghost.anchoredPosition = _ghostHome +
                    Vector2.up * (Mathf.Sin(t * ghostFloatSpeed) * ghostFloatAmp);

            float b = 1f + Mathf.Sin(t * breathSpeed) * breathAmp;
            transform.localScale = _rootHome * b;

            yield return null;
        }
    }

    private IEnumerator ShineLoop()
    {
        while (true)
        {
            yield return new WaitForSeconds(shineInterval);
            yield return ShineOnce();
        }
    }

    private IEnumerator ShineOnce()
    {
        if (shine == null) yield break;

        float half = shineTravel * 0.5f;
        Vector2 from = new Vector2(-half, 0f);
        Vector2 to   = new Vector2( half, 0f);

        shine.anchoredPosition = from;
        SetAlpha(shine, 1f);

        float t = 0f;
        while (t < shineDuration)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / shineDuration);
            shine.anchoredPosition = Vector2.LerpUnclamped(from, to, EaseInOutCubic(k));
            // 端では薄く
            SetAlpha(shine, Mathf.Sin(k * Mathf.PI));
            yield return null;
        }

        SetAlpha(shine, 0f);
    }

    // ─────────────────────────────────────────
    // 汎用トゥイーン
    // ─────────────────────────────────────────
    private delegate float Ease(float t);

    private static float EaseOutCubic(float t) => 1f - Mathf.Pow(1f - t, 3f);
    private static float EaseInOutCubic(float t) =>
        t < 0.5f ? 4f * t * t * t : 1f - Mathf.Pow(-2f * t + 2f, 3f) * 0.5f;

    private static float EaseOutBack(float t)
    {
        const float c1 = 1.70158f;
        const float c3 = c1 + 1f;
        return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
    }

    private IEnumerator Move(RectTransform rt, Vector2 target, float dur, Ease ease)
    {
        if (rt == null) yield break;

        Vector2 from = rt.anchoredPosition;
        float t = 0f;

        while (t < dur)
        {
            t += Time.unscaledDeltaTime;
            float k = ease(Mathf.Clamp01(t / dur));
            rt.anchoredPosition = Vector2.LerpUnclamped(from, target, k);
            yield return null;
        }

        rt.anchoredPosition = target;
    }

    private IEnumerator Punch(Transform tr, float amount, float dur)
    {
        Vector3 baseScale = _rootHome;
        float t = 0f;

        while (t < dur)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / dur);
            // 減衰する振動
            float s = Mathf.Sin(k * Mathf.PI * 2f) * (1f - k) * amount;
            tr.localScale = baseScale * (1f + s);
            yield return null;
        }

        tr.localScale = baseScale;
    }

    private IEnumerator FadeTo(RectTransform rt, float target, float dur)
    {
        if (rt == null) yield break;

        Image img = rt.GetComponent<Image>();
        if (img == null) yield break;

        Color c = img.color;
        float from = c.a;
        float t = 0f;

        while (t < dur)
        {
            t += Time.unscaledDeltaTime;
            c.a = Mathf.Lerp(from, target, Mathf.Clamp01(t / dur));
            img.color = c;
            yield return null;
        }

        c.a = target;
        img.color = c;
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
}
