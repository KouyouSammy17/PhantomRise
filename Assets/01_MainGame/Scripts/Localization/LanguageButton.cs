// ============================================================
// LanguageButton.cs
// タイトルの言語切り替えボタン（Assets/00_WIP/Button_Language.prefab）。
//
// ・押すと Localization.Toggle()。画面の LocalizedText はその場で貼り替わる。
// ・今の言語の国旗だけを出す（子の Flag_jp / Flag_eng を名前で探す）。
// ・文字は 2 つ。どちらも子の TMP に LocalizedText を付けて切り替える。
//     ボタンの中の Text (TMP) … "言語" / "Language"   （lang.button）
//     Language_txt            … "日本語" / "English"  （lang.current＝今の言語名）
//
// Inspector で何も設定しなくてよい。名前で探せないときだけ
// 下の欄に手で入れる。
//
// 以前の自動生成ボタン（LanguageSwitchUI）は AutoCreate = false にして止めた。
// ============================================================

using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
[AddComponentMenu("PhantomRise/Language Button")]
public class LanguageButton : MonoBehaviour
{
    private const string LabelKey        = "lang.button";
    private const string CurrentKey      = "lang.current";
    private const string CurrentTxtName  = "Language_txt";
    private const string FlagJpName  = "Flag_jp";
    private const string FlagEngName = "Flag_eng";

    [Header("=== 任意（空なら名前で探す）===")]
    [SerializeField] private GameObject flagJapanese;
    [SerializeField] private GameObject flagEnglish;
    [SerializeField] private TMP_Text   label;
    [SerializeField] private TMP_Text   currentLanguageLabel;

    [Tooltip("押したあと選択を外す（パッドで決定連打すると言語が往復するのを防ぐ）")]
    [SerializeField] private bool deselectAfterClick = true;

    private Button _button;

    private void Awake()
    {
        _button = GetComponent<Button>();

        if (flagJapanese == null) flagJapanese = FindChild(FlagJpName);
        if (flagEnglish  == null) flagEnglish  = FindChild(FlagEngName);
        if (currentLanguageLabel == null)
        {
            Transform t = transform.Find(CurrentTxtName);
            if (t != null) currentLanguageLabel = t.GetComponent<TMP_Text>();
        }

        if (label == null)
        {
            foreach (TMP_Text tmp in GetComponentsInChildren<TMP_Text>(true))
            {
                if (tmp != currentLanguageLabel) { label = tmp; break; }
            }
        }

        Localize(label,                LabelKey);
        Localize(currentLanguageLabel, CurrentKey);
    }

    /// <summary>TMP に LocalizedText を付けてキーを入れる（フォントは共通設定）。</summary>
    private static void Localize(TMP_Text tmp, string key)
    {
        if (tmp == null) return;

        LocalizedText loc = tmp.GetComponent<LocalizedText>()
                         ?? tmp.gameObject.AddComponent<LocalizedText>();
        loc.Key = key;
    }

    private void OnEnable()
    {
        _button.onClick.AddListener(OnClick);
        Localization.OnLanguageChanged += Refresh;
        Refresh();
    }

    private void OnDisable()
    {
        _button.onClick.RemoveListener(OnClick);
        Localization.OnLanguageChanged -= Refresh;
    }

    /// <summary>ボタンの onClick。Inspector から割り当ててもよい（二重登録はしない）。</summary>
    public void OnClick()
    {
        Localization.Toggle();

        if (deselectAfterClick && EventSystem.current != null &&
            EventSystem.current.currentSelectedGameObject == gameObject)
        {
            EventSystem.current.SetSelectedGameObject(null);
        }
    }

    private void Refresh()
    {
        bool jp = Localization.Current == Language.Japanese;

        if (flagJapanese != null) flagJapanese.SetActive(jp);
        if (flagEnglish  != null) flagEnglish.SetActive(!jp);
    }

    private GameObject FindChild(string childName)
    {
        Transform t = transform.Find(childName);
        return t != null ? t.gameObject : null;
    }
}
