// ============================================================
// LocalizedText.cs
// TextMeshPro に付けて、表示文字列とフォントを言語に合わせて差し替える。
//
// ・Key に Strings.csv のキーを入れるだけ。
//   有効になった瞬間と、言語が変わった瞬間に貼り替える。
//
// ・Key が空、またはキーが表に無いときは文字列は触らない。
//   プレハブに直接書いてある文がそのまま残るので、
//   訳し忘れても画面が空にならない。
//   （フォントだけは言語に合わせて切り替える）
//
// ・フォントは通常 LocalizationSettings のもの
//   （日本語: Noto Sans JP / 英語: Lilita One）を使う。
//   この文字だけ別のフォントにしたいときだけ
//   japaneseFont / englishFont を入れる。
//
// ・matchJapaneseLayout（既定 ON）
//   英語フォント（Lilita One）は日本語フォント（Noto Sans JP）より
//   行の高さが約 2 割低く、1 行目の上端も高い。
//   そのままだと行が下に行くほど日本語版の位置からずれ、
//   アイコンや背景に合わせて組んだ文が合わなくなる。
//   ON のときは英語表示時に lineSpacing と上マージンを補正して、
//   各行の縦位置が日本語版と一致するようにする。
//   横位置は Strings.csv の英語側で <align=left><pos=…> を使って合わせる。
//
// 【注意】
//   実行時にこのコンポーネントが text を上書きする。
//   シーン側で m_text を直接書き換えていても、こちらが勝つ。
// ============================================================

using TMPro;
using UnityEngine;

[DisallowMultipleComponent]
[AddComponentMenu("PhantomRise/Localized Text")]
public class LocalizedText : MonoBehaviour
{
    [Tooltip("Assets/Resources/Localization/Strings.csv のキー")]
    [SerializeField] private string key;

    [Header("=== フォントの上書き（任意）===")]
    [Tooltip("空なら LocalizationSettings の日本語フォントを使う")]
    [SerializeField] private TMP_FontAsset japaneseFont;

    [Tooltip("空なら LocalizationSettings の英語フォントを使う")]
    [SerializeField] private TMP_FontAsset englishFont;

    [Header("=== レイアウト補正 ===")]
    [Tooltip("ON: 英語のとき行間と上マージンを補正し、各行の縦位置を日本語フォントと同じにする\n"
           + "（アイコンや背景に合わせて行を組んでいる文はこれが必要）")]
    [SerializeField] private bool matchJapaneseLayout = true;

    private TMP_Text _target;

    // 補正の基準になるプレハブ側の値（Awake で 1 度だけ取る）
    private float   _baseLineSpacing;
    private Vector4 _baseMargin;
    private bool    _baseCaptured;

    /// <summary>コードからキーを差し替えたいとき。</summary>
    public string Key
    {
        get => key;
        set { key = value; Apply(); }
    }

    /// <summary>コードからフォントの上書きを入れたいとき（null で共通設定に戻る）。</summary>
    public TMP_FontAsset JapaneseFont
    {
        get => japaneseFont;
        set { japaneseFont = value; Apply(); }
    }

    public TMP_FontAsset EnglishFont
    {
        get => englishFont;
        set { englishFont = value; Apply(); }
    }

    private void Awake()
    {
        _target = GetComponent<TMP_Text>();

        if (_target == null)
            Debug.LogWarning($"[LocalizedText] {name} に TextMeshPro がありません。", this);

        CaptureBase();
    }

    /// <summary>プレハブに書かれた行間・マージンを覚えておく（補正はこれに足す）。</summary>
    private void CaptureBase()
    {
        if (_baseCaptured || _target == null) return;
        _baseLineSpacing = _target.lineSpacing;
        _baseMargin      = _target.margin;
        _baseCaptured    = true;
    }

    private void OnEnable()
    {
        Localization.OnLanguageChanged += Apply;
        Apply();
    }

    private void OnDisable()
    {
        Localization.OnLanguageChanged -= Apply;
    }

    /// <summary>今の言語の文字列とフォントを貼る。</summary>
    public void Apply()
    {
        if (_target == null) _target = GetComponent<TMP_Text>();
        if (_target == null) return;

        // 見つからないキーは fallback（今出ている文）を残す
        if (!string.IsNullOrEmpty(key))
            _target.text = Localization.Get(key, _target.text);

        TMP_FontAsset font = ResolveFont(Localization.Current);
        if (font != null && _target.font != font) _target.font = font;

        ApplyLayoutCompensation(font);
    }

    // ─────────────────────────────────────────
    // 縦位置の補正
    // ─────────────────────────────────────────

    /// <summary>
    /// 今のフォントの行の高さ・上端を日本語フォントに合わせる。
    /// 日本語表示のときはプレハブの値に戻す。
    /// </summary>
    private void ApplyLayoutCompensation(TMP_FontAsset current)
    {
        CaptureBase();
        if (!_baseCaptured) return;

        float   lineSpacing = _baseLineSpacing;
        Vector4 margin      = _baseMargin;

        TMP_FontAsset reference = ResolveFont(Language.Japanese);

        if (matchJapaneseLayout
            && current != null && reference != null && current != reference
            && current.faceInfo.pointSize > 0 && reference.faceInfo.pointSize > 0)
        {
            // 1em あたりの行の高さ / 上端（アセントライン）
            float refLine = reference.faceInfo.lineHeight / reference.faceInfo.pointSize;
            float curLine = current.faceInfo.lineHeight   / current.faceInfo.pointSize;
            float refAsc  = reference.faceInfo.ascentLine / reference.faceInfo.pointSize;
            float curAsc  = current.faceInfo.ascentLine   / current.faceInfo.pointSize;

            // TMP の lineSpacing は 1/100 em 単位
            lineSpacing += (refLine - curLine) * 100f;

            // 上揃えのときだけ 1 行目の上端も合わせる
            // （中央・下揃えは行の高さが揃えばほぼ一致する）
            if (_target.verticalAlignment == VerticalAlignmentOptions.Top)
                margin.y += (refAsc - curAsc) * _target.fontSize;
        }

        if (!Mathf.Approximately(_target.lineSpacing, lineSpacing)) _target.lineSpacing = lineSpacing;
        if (_target.margin != margin) _target.margin = margin;
    }

    /// <summary>この文字用の上書き → 共通設定 の順に探す。</summary>
    private TMP_FontAsset ResolveFont(Language language)
    {
        TMP_FontAsset own = language == Language.English ? englishFont : japaneseFont;
        return own != null ? own : Localization.FontFor(language);
    }
}
