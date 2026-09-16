// ============================================================
// LanguageSwitchUI.cs
// タイトル画面の言語切り替えボタン。
//
// ・シーンに何も置かなくてよい。
//   RuntimeInitializeOnLoadMethod でシーン読み込みを監視し、
//   タイトルシーン（Scenes.Title）に入ったときだけ自分を生成する。
//   → タイトルのレイアウトを触らずに済む。
//
// ・押すと Localization.Toggle() が走り、
//   画面に出ている LocalizedText がその場で貼り替わる。
//   選んだ言語は PlayerPrefs に残るので次回起動でも保たれる。
//
// ・ボタンには「押したら切り替わる先の言語」を、その言語のフォントで出す。
//     日本語のとき → "English"（Lilita One）
//     英語のとき   → "日本語"（Noto Sans JP）
//   文字は Strings.csv の lang.switch_to、
//   フォントは LocalizationSettings から取る。
//   設定が無いときは ASCII の "EN" / "JP" に落とす
//   （拾い物のフォントは日本語グリフを持たない可能性があるため）。
//
// 【あとで自分でデザインしたくなったら】
//   タイトルの Canvas に好きなボタンを置き、
//   OnClick に LanguageSwitchUI.OnClick() を割り当てるだけでよい。
//   その場合はこのファイル冒頭の AutoCreate を false にする。
// ============================================================

using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LanguageSwitchUI : MonoBehaviour
{
    // ─────────────────────────────────────────
    // 設定
    // ─────────────────────────────────────────

    /// <summary>タイトルシーンでボタンを自動生成するか</summary>
    private const bool AutoCreate = false;   // タイトルの Button_Language（LanguageButton）に置き換えた

    /// <summary>Strings.csv のキー。今の言語で引くと「切り替え先の言語名」が出る</summary>
    private const string LabelKey = "lang.switch_to";

    /// <summary>タイトルロゴより手前、暗転（30000）より奥</summary>
    private const int SortingOrder = 500;

    private static readonly Vector2 ButtonSize = new Vector2(168f, 56f);

    /// <summary>画面右上からの余白</summary>
    private static readonly Vector2 Margin = new Vector2(-32f, -32f);

    private static readonly Color ButtonColor = new Color(0.08f, 0.06f, 0.14f, 0.82f);
    private static readonly Color LabelColor  = new Color(0.93f, 0.90f, 1f, 1f);

    // ─────────────────────────────────────────
    // 内部
    // ─────────────────────────────────────────

    private static LanguageSwitchUI _instance;

    private TextMeshProUGUI _label;
    private RectTransform   _root;

    /// <summary>LocalizationSettings が無いときに使う、タイトル画面から借りたフォント</summary>
    private TMP_FontAsset _borrowedFont;

    // ─────────────────────────────────────────
    // 自動生成
    // ─────────────────────────────────────────

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Hook()
    {
        if (!AutoCreate) return;

        // BeforeSceneLoad で刺すので、最初のシーンにも sceneLoaded が飛ぶ
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == Scenes.Title) Spawn();
    }

    private static void Spawn()
    {
        if (_instance != null) return;

        var go = new GameObject("LanguageSwitch",
            typeof(RectTransform), typeof(Canvas),
            typeof(CanvasScaler), typeof(GraphicRaycaster));

        _instance = go.AddComponent<LanguageSwitchUI>();
        _instance.Build();
    }

    // ─────────────────────────────────────────
    // 生成
    // ─────────────────────────────────────────

    private void Build()
    {
        var canvas = GetComponent<Canvas>();
        canvas.renderMode  = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = SortingOrder;

        var scaler = GetComponent<CanvasScaler>();
        scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight  = 0.5f;

        // ボタン本体（右上）
        var buttonGo = new GameObject("Button",
            typeof(RectTransform), typeof(CanvasRenderer),
            typeof(Image), typeof(Button));

        buttonGo.transform.SetParent(transform, false);

        _root = (RectTransform)buttonGo.transform;
        _root.anchorMin        = Vector2.one;
        _root.anchorMax        = Vector2.one;
        _root.pivot            = Vector2.one;
        _root.sizeDelta        = ButtonSize;
        _root.anchoredPosition = Margin;

        buttonGo.GetComponent<Image>().color = ButtonColor;

        // ラベル
        var labelGo = new GameObject("Label",
            typeof(RectTransform), typeof(CanvasRenderer));

        labelGo.transform.SetParent(buttonGo.transform, false);

        var labelRt = (RectTransform)labelGo.transform;
        labelRt.anchorMin = Vector2.zero;
        labelRt.anchorMax = Vector2.one;
        labelRt.offsetMin = Vector2.zero;
        labelRt.offsetMax = Vector2.zero;

        _label = labelGo.AddComponent<TextMeshProUGUI>();

        // 設定が無いときの保険として、タイトル画面のフォントを 1 つ借りておく
        _borrowedFont = FindSceneFont();

        _label.alignment    = TextAlignmentOptions.Center;
        _label.fontSize     = 28f;
        _label.color        = LabelColor;
        _label.raycastTarget = false;

        buttonGo.GetComponent<Button>().onClick.AddListener(OnClick);

        Refresh();
    }

    /// <summary>
    /// タイトル画面に置かれている TextMeshPro からフォントを 1 つ借りる。
    /// 見つからなければ null（TMP の既定フォントが使われる）。
    /// </summary>
    private TMP_FontAsset FindSceneFont()
    {
        TextMeshProUGUI[] texts =
            FindObjectsByType<TextMeshProUGUI>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        foreach (TextMeshProUGUI t in texts)
        {
            if (t == _label || t.font == null) continue;
            return t.font;
        }

        return null;
    }

    private void OnDestroy()
    {
        // sceneLoaded の購読は外さない。
        // 外すとステージからタイトルへ戻ったときに二度と生成されなくなる。
        if (_instance == this) _instance = null;

        if (_root != null) DOTween.Kill(_root);
    }

    // ─────────────────────────────────────────
    // 切り替え
    // ─────────────────────────────────────────

    /// <summary>自分でボタンを置いた場合は OnClick にこれを割り当てる。</summary>
    public void OnClick()
    {
        Localization.Toggle();
        UISoundPlayer.PlayConfirm();

        Refresh();
        Punch();

        // 押したあと選択が残ると、パッドで決定を押すたびに言語が変わる
        if (EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(null);
    }

    /// <summary>切り替え先の言語名を、その言語のフォントで出す。</summary>
    private void Refresh()
    {
        if (_label == null) return;

        Language target = Localization.Current == Language.English
            ? Language.Japanese
            : Language.English;

        TMP_FontAsset font = Localization.FontFor(target);

        if (font != null)
        {
            _label.font = font;
            _label.text = Localization.Get(LabelKey, target == Language.English ? "English" : "日本語");
            return;
        }

        // フォント設定が無い。借り物のフォントに日本語が無いかもしれないので ASCII だけ
        if (_borrowedFont != null) _label.font = _borrowedFont;
        _label.text = target == Language.English ? "EN" : "JP";
    }

    /// <summary>押した手ごたえ。timeScale に左右されないよう SetUpdate(true)。</summary>
    private void Punch()
    {
        if (_root == null) return;

        DOTween.Kill(_root);

        _root.localScale = Vector3.one;
        _root.DOPunchScale(Vector3.one * 0.12f, 0.24f, 8, 0.6f)
             .SetUpdate(true);
    }
}
