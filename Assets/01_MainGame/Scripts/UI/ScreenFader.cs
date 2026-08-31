// ============================================================
// ScreenFader.cs
// 画面全体を覆う暗転オーバーレイ（フェードイン / フェードアウト）。
//
// ・シーンに何も置かなくてよい。
//   ScreenFader.Instance に最初に触れた瞬間、自分で
//   Canvas + Image を作って DontDestroyOnLoad になる。
//   → プレハブの置き忘れでステージごとに動かない、が起きない。
//
// ・シーンをまたいで生き残るので、
//   「暗転 → LoadScene → 次のシーンで自動的に明ける」ができる。
//   明ける処理は SceneManager.sceneLoaded で行うため、
//   遷移元の GameManager が破棄されても止まらない。
//
// ・Tween は必ず SetUpdate(true)。
//   ゲームオーバー / クリア / ポーズは timeScale = 0 なので、
//   スケール時間で回すと暗転が固まる。
//   待ち時間も UniTask.Delay の UnscaledDeltaTime。
//
// 【使い方】
//   // シーン遷移（暗転してから読み込む。明けるのは自動）
//   ScreenFader.Instance.FadeOut(0.35f);
//
//   // 暗転したまま始めて、演出に合わせて明ける
//   ScreenFader.Instance.SetAlphaImmediate(1f);   // Awake で
//   ScreenFader.Instance.FadeIn(0.8f);            // 演出の頭で
//
//   // コルーチンから待ちたいとき（Tween は実時間で動く）
//   ScreenFader.Instance.FadeIn(0.8f);
//   yield return new WaitForSecondsRealtime(0.8f);
//
//   // async から待ちたいとき
//   await ScreenFader.Instance.FadeInAsync(0.8f);
//
// 【注意】
//   このオーバーレイは sortingOrder 30000 で常に最前面に出る。
//   「ゲームクリア パネルの “後ろ” だけを暗くしたい」場合は
//   これではなく、そのパネルと同じ Canvas の中に
//   パネルより上の兄弟順で Image を置くこと。
// ============================================================

using System;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class ScreenFader : MonoBehaviour
{
    // ─────────────────────────────────────────
    // 既定値
    // ─────────────────────────────────────────

    public const float DefaultFadeOutDuration = 0.35f;
    public const float DefaultFadeInDuration  = 0.45f;

    /// <summary>他の UI より確実に前に出すための sortingOrder</summary>
    private const int OverlaySortingOrder = 30000;

    // ─────────────────────────────────────────
    // Singleton（遅延生成 + DontDestroyOnLoad）
    // ─────────────────────────────────────────

    private static ScreenFader _instance;

    public static ScreenFader Instance
    {
        get
        {
            if (_instance == null) CreateInstance();
            return _instance;
        }
    }

    private static void CreateInstance()
    {
        GameObject go = new GameObject("[ScreenFader]");
        DontDestroyOnLoad(go);

        _instance = go.AddComponent<ScreenFader>();
        _instance.Build();
    }

    // ─────────────────────────────────────────
    // 内部
    // ─────────────────────────────────────────

    private CanvasGroup _group;
    private Image       _image;
    private Tween       _tween;

    /// <summary>LoadSceneAsync が自分で明ける間は sceneLoaded 側を黙らせる</summary>
    private bool _isTransitioning;

    /// <summary>次にシーンが読み込まれたとき、何秒かけて明けるか</summary>
    private float _pendingFadeInDuration = DefaultFadeInDuration;

    /// <summary>true の間は自動フェードインしない（開幕演出が自分で明ける）</summary>
    private bool _holdBlack;

    /// <summary>今、画面が少しでも暗いか</summary>
    public bool IsCovering => _group != null && _group.alpha > 0.001f;

    private void Build()
    {
        // Canvas（Screen Space - Overlay。カメラが無いシーンでも出る）
        Canvas canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode  = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = OverlaySortingOrder;

        // 暗転中のクリックを吸うために必要
        gameObject.AddComponent<GraphicRaycaster>();

        _group = gameObject.AddComponent<CanvasGroup>();
        _group.alpha          = 0f;
        _group.blocksRaycasts = false;
        _group.interactable   = false;

        // 画面いっぱいに伸ばした黒 Image
        GameObject overlay = new GameObject("Overlay", typeof(RectTransform));
        overlay.transform.SetParent(transform, false);

        RectTransform rect = (RectTransform)overlay.transform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        _image = overlay.AddComponent<Image>();
        _image.color         = Color.black;
        _image.raycastTarget = true;

        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;

        _tween?.Kill();

        if (_instance == this) _instance = null;
    }

    // ─────────────────────────────────────────
    // シーンが変わったら自動的に明ける
    //
    // 遷移元の GameManager はシーンと一緒に消えるので、
    // 「明ける」は遷移先に残るこいつが担当する。
    // ─────────────────────────────────────────

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (mode != LoadSceneMode.Single) return;
        if (_isTransitioning) return;   // LoadSceneAsync が自分で明ける
        if (_holdBlack) return;         // 開幕演出が自分のタイミングで明ける
        if (!IsCovering) return;        // 暗転していないなら何もしない

        FadeInAsync(_pendingFadeInDuration).Forget();
    }

    // ─────────────────────────────────────────
    // 公開 API（コルーチン / 同期から呼ぶ版）
    // ─────────────────────────────────────────

    /// <summary>暗転する。シーンを読み込むと遷移先で自動的に明ける。</summary>
    public void FadeOut(
        float duration = DefaultFadeOutDuration,
        float fadeInAfterSceneLoad = DefaultFadeInDuration)
    {
        // 新しい遷移が始まった時点で、前のシーンの「黒のまま保持」は無効。
        // 遷移先が開幕演出を持つなら、そちらの Awake で改めて保持される。
        _holdBlack = false;

        _pendingFadeInDuration = fadeInAfterSceneLoad;
        FadeOutAsync(duration).Forget();
    }

    /// <summary>明るくする。</summary>
    public void FadeIn(float duration = DefaultFadeInDuration)
    {
        FadeInAsync(duration).Forget();
    }

    /// <summary>Tween を挟まず即座に設定する（0 = 透明 / 1 = 真っ黒）。</summary>
    public void SetAlphaImmediate(float alpha)
    {
        _tween?.Kill();

        _group.alpha          = Mathf.Clamp01(alpha);
        _group.blocksRaycasts = _group.alpha > 0.001f;
    }

    /// <summary>
    /// 真っ黒のまま保持する。シーン読み込み後の自動フェードインを止めるので、
    /// 開幕演出の好きなタイミングで自分で FadeIn を呼ぶこと。
    /// 呼ぶのは Awake（sceneLoaded より前）。
    /// </summary>
    public void HoldBlack()
    {
        _holdBlack = true;
        SetAlphaImmediate(1f);
    }

    /// <summary>暗転の色を変える（既定は黒）。ホワイトアウトなどに。</summary>
    public void SetColor(Color color)
    {
        float alpha = _group.alpha;   // 透明度は CanvasGroup 側で持つ

        color.a      = 1f;
        _image.color = color;

        _group.alpha = alpha;
    }

    // ─────────────────────────────────────────
    // 公開 API（async 版）
    // ─────────────────────────────────────────

    public UniTask FadeOutAsync(float duration = DefaultFadeOutDuration)
    {
        return FadeToAsync(1f, duration, blockRaycasts: true);
    }

    public UniTask FadeInAsync(float duration = DefaultFadeInDuration)
    {
        return FadeToAsync(0f, duration, blockRaycasts: true);
    }

    /// <summary>
    /// 半透明で止めたいとき用（例: 0.6f で薄暗く）。
    /// blockRaycasts = false にすると、暗いままでもボタンを押せる。
    /// </summary>
    public async UniTask FadeToAsync(float alpha, float duration, bool blockRaycasts = true)
    {
        alpha = Mathf.Clamp01(alpha);

        // 明るくする指示が来た時点で「黒のまま保持」は終わり
        if (alpha < 0.999f) _holdBlack = false;

        _tween?.Kill();

        // 暗転している間はクリックを吸っておく
        if (blockRaycasts) _group.blocksRaycasts = true;

        if (duration <= 0f)
        {
            _group.alpha = alpha;
        }
        else
        {
            _tween = _group.DOFade(alpha, duration)
                           .SetEase(Ease.Linear)
                           .SetUpdate(true);   // timeScale = 0 でも進む

            try
            {
                await UniTask.Delay(
                    TimeSpan.FromSeconds(duration),
                    DelayType.UnscaledDeltaTime,
                    cancellationToken: this.GetCancellationTokenOnDestroy());
            }
            catch (OperationCanceledException)
            {
                // 破棄された。UI を触らずに抜ける。
                return;
            }

            _group.alpha = alpha;
        }

        // 透明に戻ったらクリックを通す
        _group.blocksRaycasts = blockRaycasts && alpha > 0.001f;
    }

    // ─────────────────────────────────────────
    // 暗転つきシーン遷移（GameManager を通さず直接使う場合）
    // ─────────────────────────────────────────

    public async UniTask LoadSceneAsync(
        string sceneName,
        float  fadeOutDuration = DefaultFadeOutDuration,
        float  fadeInDuration  = DefaultFadeInDuration)
    {
        if (_isTransitioning) return;   // 連打で二重に遷移させない
        _isTransitioning = true;

        await FadeOutAsync(fadeOutDuration);

        // ポーズ中に遷移しても新シーンが止まらないように戻す
        Time.timeScale = 1f;
        SceneManager.LoadScene(sceneName);

        // 新シーンの Awake / Start が走り終わってから明ける
        await UniTask.DelayFrame(1, cancellationToken: this.GetCancellationTokenOnDestroy());

        _isTransitioning = false;

        // 遷移先に開幕演出があるなら、明けるのはそちらに任せる
        if (_holdBlack) return;

        await FadeInAsync(fadeInDuration);
    }
}
