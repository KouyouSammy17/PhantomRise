// ============================================================
// LocalizationSettings.cs
// 言語ごとのフォントを 1 か所で決める ScriptableObject。
//
// 置き場所: Assets/Resources/Localization/LocalizationSettings.asset
//   （Localization が起動時に Resources.Load で読む。動かさないこと）
//
// ・日本語 … Noto Sans JP
// ・英語   … Lilita One
//
// LocalizedText はフォント欄が空ならここのフォントを使う。
// LanguageSwitchUI のボタンもここのフォントで描く。
//
// フォントを変えたいときはこのアセットの欄を差し替えるだけでよい。
// プレハブを 1 つずつ開いて直す必要はない。
// ============================================================

using TMPro;
using UnityEngine;

[CreateAssetMenu(fileName = "LocalizationSettings",
                 menuName = "PhantomRise/Localization Settings")]
public class LocalizationSettings : ScriptableObject
{
    [Header("=== 言語ごとのフォント ===")]
    [Tooltip("日本語のとき使うフォント（Noto Sans JP）")]
    [SerializeField] private TMP_FontAsset japaneseFont;

    [Tooltip("英語のとき使うフォント（Lilita One）")]
    [SerializeField] private TMP_FontAsset englishFont;

    public TMP_FontAsset JapaneseFont => japaneseFont;
    public TMP_FontAsset EnglishFont  => englishFont;

    /// <summary>その言語のフォント。未設定なら null。</summary>
    public TMP_FontAsset FontFor(Language language)
    {
        return language == Language.English ? englishFont : japaneseFont;
    }
}
