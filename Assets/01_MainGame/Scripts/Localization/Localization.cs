// ============================================================
// Localization.cs
// 日本語 / 英語の文字列を 1 か所で管理する。
//
// ・シーンに何も置かなくてよい。
//   RuntimeInitializeOnLoadMethod で、最初のシーンが読み込まれる前に
//   Resources/Localization/Strings.csv と LocalizationSettings.asset を読み込み、
//   PlayerPrefs に保存された言語を復元する。
//
// ・表示側は LocalizedText を TextMeshPro に付けるだけ。
//   コードから出す文字列は Localization.Get("key") を通す。
//
// ・フォントは LocalizationSettings.asset で決める
//   （日本語: Noto Sans JP / 英語: Lilita One）。
//   Localization.FontFor(lang) / Localization.CurrentFont で取れる。
//
// ・言語を変えると OnLanguageChanged が飛ぶので、
//   表示中の LocalizedText はその場で貼り替わる。
//
// 【使い方】
//   string s = Localization.Get("tutorial.ghost");
//   Localization.SetLanguage(Language.English);
//   Localization.Toggle();
//
// 【翻訳の追加】
//   Assets/Resources/Localization/Strings.csv に 1 行足すだけ。
//   列は Key,Japanese,English。
//   セル内の改行は "..." で囲むか \n と書く。
//   カンマを含むときは必ず "..." で囲むこと。
//
// 【注意】
//   キーが表に無いときは Get() がキー文字列をそのまま返す。
//   画面に "tutorial.ghost" と出たら CSV の行が抜けている。
// ============================================================

using System;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;

public enum Language
{
    Japanese = 0,
    English  = 1,
}

public static class Localization
{
    // ─────────────────────────────────────────
    // 設定
    // ─────────────────────────────────────────

    /// <summary>Resources 以下の CSV のパス（拡張子なし）</summary>
    private const string TableResourcePath = "Localization/Strings";

    /// <summary>選んだ言語の保存先</summary>
    private const string PrefsKey = "PhantomRise.Language";

    /// <summary>言語ごとのフォント設定（Resources 以下、拡張子なし）</summary>
    private const string SettingsResourcePath = "Localization/LocalizationSettings";

    // ─────────────────────────────────────────
    // 状態
    // ─────────────────────────────────────────

    /// <summary>key → [日本語, 英語]</summary>
    private static readonly Dictionary<string, string[]> Table =
        new Dictionary<string, string[]>(StringComparer.Ordinal);

    private static bool _loaded;

    /// <summary>言語ごとのフォント。無ければ null（フォントは切り替えない）</summary>
    private static LocalizationSettings _settings;

    /// <summary>今の言語。</summary>
    public static Language Current { get; private set; } = Language.Japanese;

    /// <summary>言語が切り替わったときに飛ぶ。LocalizedText が購読している。</summary>
    public static event Action OnLanguageChanged;

    // ─────────────────────────────────────────
    // 起動
    // ─────────────────────────────────────────

    /// <summary>
    /// 最初のシーンが読み込まれる前に走る。
    /// これがあるので「言語マネージャーをシーンに置き忘れる」が起きない。
    /// </summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        EnsureLoaded();
        Current = (Language)PlayerPrefs.GetInt(PrefsKey, (int)Language.Japanese);
    }

    private static void EnsureLoaded()
    {
        if (_loaded) return;
        _loaded = true;

        _settings = Resources.Load<LocalizationSettings>(SettingsResourcePath);
        if (_settings == null)
        {
            Debug.LogWarning(
                $"[Localization] Resources/{SettingsResourcePath}.asset が見つかりません。" +
                "言語を変えてもフォントは切り替わりません。");
        }

        TextAsset csv = Resources.Load<TextAsset>(TableResourcePath);
        if (csv == null)
        {
            Debug.LogError(
                $"[Localization] Resources/{TableResourcePath}.csv が見つかりません。" +
                "英語表示は効きません。");
            return;
        }

        Parse(csv.text);
    }

    // ─────────────────────────────────────────
    // 取得
    // ─────────────────────────────────────────

    /// <summary>今の言語の文字列を返す。無いキーはキーをそのまま返す。</summary>
    public static string Get(string key)
    {
        return Get(key, key);
    }

    /// <summary>
    /// 今の言語の文字列を返す。
    /// キーが空、または表に無いときは fallback を返す。
    /// （プレハブに直接書いてある文だけ差し替えたいときに使う）
    /// </summary>
    public static string Get(string key, string fallback)
    {
        if (string.IsNullOrEmpty(key)) return fallback;

        EnsureLoaded();

        if (!Table.TryGetValue(key, out string[] row)) return fallback;

        int index = (int)Current;
        if (index < 0 || index >= row.Length) index = 0;

        string value = row[index];

        // 英語欄が空なら日本語で出す（訳し忘れでも空白にならないように）
        if (string.IsNullOrEmpty(value)) value = row[0];

        return string.IsNullOrEmpty(value) ? fallback : value;
    }

    /// <summary>そのキーが表にあるか。</summary>
    public static bool Has(string key)
    {
        if (string.IsNullOrEmpty(key)) return false;
        EnsureLoaded();
        return Table.ContainsKey(key);
    }

    // ─────────────────────────────────────────
    // フォント
    // ─────────────────────────────────────────

    /// <summary>
    /// その言語のフォント（日本語: Noto Sans JP / 英語: Lilita One）。
    /// LocalizationSettings が無ければ null。
    /// </summary>
    public static TMP_FontAsset FontFor(Language language)
    {
        EnsureLoaded();
        return _settings != null ? _settings.FontFor(language) : null;
    }

    /// <summary>今の言語のフォント。</summary>
    public static TMP_FontAsset CurrentFont => FontFor(Current);

    /// <summary>デバッグ用。読み込めた行数。</summary>
    public static int EntryCount
    {
        get { EnsureLoaded(); return Table.Count; }
    }

    // ─────────────────────────────────────────
    // 切り替え
    // ─────────────────────────────────────────

    public static void SetLanguage(Language language)
    {
        if (Current == language) return;

        Current = language;

        PlayerPrefs.SetInt(PrefsKey, (int)language);
        PlayerPrefs.Save();

        OnLanguageChanged?.Invoke();
    }

    /// <summary>日本語 ⇔ 英語。タイトルの言語ボタンから呼ぶ。</summary>
    public static void Toggle()
    {
        SetLanguage(Current == Language.Japanese ? Language.English : Language.Japanese);
    }

    // ─────────────────────────────────────────
    // CSV パース（RFC 4180）
    //
    // ・"..." で囲まれたセルは中に , と改行を持てる
    // ・囲みの中の "" は " 1文字
    // ・囲まれていないセルの \n \t は改行 / タブに直す
    //   （Excel で編集しやすいように、どちらの書き方も許す）
    // ─────────────────────────────────────────

    private static void Parse(string text)
    {
        Table.Clear();

        List<string> cells = new List<string>(3);
        StringBuilder cell = new StringBuilder();

        bool quoted    = false;   // いま "..." の中か
        bool wasQuoted = false;   // このセルは "..." だったか
        bool firstRow  = true;

        for (int i = 0; i < text.Length; i++)
        {
            char c = text[i];

            if (quoted)
            {
                if (c != '"') { cell.Append(c); continue; }

                // "" は " 1文字。それ以外は囲みの終わり
                if (i + 1 < text.Length && text[i + 1] == '"') { cell.Append('"'); i++; }
                else quoted = false;

                continue;
            }

            if (c == '"' && cell.Length == 0)
            {
                quoted = true;
                wasQuoted = true;
                continue;
            }

            if (c == ',')
            {
                cells.Add(Finish(cell, wasQuoted));
                wasQuoted = false;
                continue;
            }

            if (c == '\r') continue;   // CRLF の CR は捨てる

            if (c == '\n')
            {
                cells.Add(Finish(cell, wasQuoted));
                wasQuoted = false;

                if (firstRow) firstRow = false;   // 1行目はヘッダー
                else AddRow(cells);

                cells.Clear();
                continue;
            }

            cell.Append(c);
        }

        // 末尾に改行が無いファイルの最終行
        if (cell.Length > 0 || cells.Count > 0)
        {
            cells.Add(Finish(cell, wasQuoted));
            if (!firstRow) AddRow(cells);
        }
    }

    private static string Finish(StringBuilder cell, bool wasQuoted)
    {
        string s = cell.ToString();
        cell.Length = 0;

        if (!wasQuoted)
        {
            s = s.Trim();
            s = s.Replace("\\n", "\n").Replace("\\t", "\t");
        }

        return s;
    }

    private static void AddRow(List<string> cells)
    {
        if (cells.Count == 0) return;

        string key = cells[0].Trim();

        // 空行と # で始まるコメント行は飛ばす
        if (string.IsNullOrEmpty(key) || key.StartsWith("#")) return;

        string ja = cells.Count > 1 ? cells[1] : string.Empty;
        string en = cells.Count > 2 ? cells[2] : string.Empty;

        Table[key] = new[] { ja, en };
    }
}
