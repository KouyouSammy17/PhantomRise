// ============================================================
// PauseManager.cs
// Esc（キーボード）/ START（Xbox コントローラー）でポーズ UI を開閉する
//
// UI Manager プレハブにアタッチしてある。
// UI Manager が置いてあるシーンならどこでも動く。
//
// パネルは UI Manager の子にある Play_Pause をそのまま使う。
// Inspector で未割り当てのときは名前で探して自動で拾うので、
// 通常は設定なしで動く。別のパネルを使いたいときだけ割り当てる。
//
// ポーズ中は Time.timeScale = 0 にして PlayerInput を止める。
// EventSystem に Button_Continue を選択させるので、
// コントローラーだけでも操作できる。
// ============================================================

using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class PauseManager : MonoBehaviour
{
    /// <summary>ポーズ中かどうか。他スクリプトから参照する用。</summary>
    public static bool IsPaused { get; private set; }

    // ─────────────────────────────────────────
    // Play_Pause プレハブ内のオブジェクト名
    // 自動検索に使うので、リネームしたらここも直す
    // ─────────────────────────────────────────

    private const string PanelName    = "Play_Pause";
    private const string ContinueName = "Button_Continue";
    private const string RestartName  = "Button_Restart";
    private const string TitleName    = "Button_GiveUp";
    private const string SettingName  = "Button_Setting";

    // 設定パネル（UI Manager の子にある Popup_Setting）
    private const string SettingsPanelName = "Popup_Setting";
    private const string SettingsBackName  = "BackButton";    // Popup_Setting 直下の戻るボタン
  

    // ─────────────────────────────────────────
    // Inspector
    // ─────────────────────────────────────────

    [Header("=== ポーズ UI（未割り当てなら名前で自動検索）===")]
    [Tooltip("UI Manager の子にある Play_Pause")]
    [SerializeField] private GameObject pausePanel;
    [SerializeField] private Button continueButton;   // Button_Continue → 再開
    [SerializeField] private Button restartButton;    // Button_Restart  → やり直し
    [SerializeField] private Button titleButton;      // Button_GiveUp   → タイトルへ

    [Header("=== 設定パネル（未割り当てなら名前で自動検索）===")]
    [Tooltip("UI Manager の子にある Popup_Setting")]
    [SerializeField] private GameObject settingsPanel;
    [SerializeField] private Button settingsButton;        // Button_Setting → 設定を開く
    [SerializeField] private Button settingsBackButton;    // BackButton     → ポーズメニューへ戻る
    [SerializeField] private Button settingsCloseButton;   // Button_Close   → 設定を閉じる

    [Tooltip("設定を開いたとき最初に選択する項目。空ならパネル内の最初の Selectable")]
    [SerializeField] private Selectable settingsFirstSelected;

    [Header("=== パッド操作 ===")]
    [Tooltip("ポーズを開いたとき最初に選択するオブジェクト。空なら Button_Continue")]
    [SerializeField] private GameObject firstSelected;

    [Tooltip("背景クリックなどで選択が外れたとき、選択を戻す")]
    [SerializeField] private bool keepSelection = true;

    [Header("=== 表示アニメーション ===")]
    [SerializeField] private float panelFadeDuration = 0.22f;

    [Tooltip("出てくるときの開始スケール（1 で拡大なし）")]
    [SerializeField] private float panelPopScale = 0.88f;

    [SerializeField] private float panelPopDuration = 0.34f;

    [Header("=== 効果音（任意）===")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip openSound;
    [SerializeField] private AudioClip closeSound;

    // ─────────────────────────────────────────
    // 入力
    // ─────────────────────────────────────────

    // .inputactions の Player マップに足すと、ポーズ中に
    // マップを止めた時点で閉じられなくなる。
    // ポーズだけはコード側でバインドを持ち、常に有効にしておく。
    private InputAction _pauseAction;

    // ─────────────────────────────────────────
    // 内部
    // ─────────────────────────────────────────

    private PlayerStateMachine _player;
    private PlayerInput _playerInput;
    private float _previousTimeScale = 1f;

    /// <summary>最後に選ばれていたボタン。選択が外れたときここへ戻す</summary>
    private GameObject _lastSelected;

    // プレハブで設定されたスケール。決め打ちで 1 に戻すと
    // 拡大して置いてあるパネルが縮んでしまうので記録しておく
    private Vector3 _pauseBaseScale    = Vector3.one;
    private Vector3 _settingsBaseScale = Vector3.one;

    /// <summary>UI の Cancel（パッドの B / Esc）。設定やポーズを閉じるのに使う</summary>
    private InputAction _cancelAction;

    // ─────────────────────────────────────────
    // Unity ライフサイクル
    // ─────────────────────────────────────────

    private void Awake()
    {
        _pauseAction = new InputAction("Pause", InputActionType.Button);
        _pauseAction.AddBinding("<Keyboard>/escape");
        _pauseAction.AddBinding("<Gamepad>/start");   // Xbox の START ボタン
        _pauseAction.performed += OnPausePerformed;
    }

    private void OnEnable()
    {
        _pauseAction?.Enable();
    }

    private void OnDisable()
    {
        _pauseAction?.Disable();
    }

    private void OnDestroy()
    {
        if (_cancelAction != null) _cancelAction.performed -= OnCancelPerformed;

        KillPanelTweens(pausePanel);
        KillPanelTweens(settingsPanel);

        if (_pauseAction != null)
        {
            _pauseAction.performed -= OnPausePerformed;
            _pauseAction.Dispose();
        }

        // シーンを跨いでポーズ扱いのまま残らないようにする
        IsPaused = false;
    }

    private void Start()
    {
        // プレイヤーのいないシーン（タイトルなど）ではポーズさせない
        _player = FindAnyObjectByType<PlayerStateMachine>();
        if (_player != null) _playerInput = _player.GetComponent<PlayerInput>();

        if (pausePanel == null)
            pausePanel = FindDeep(transform, PanelName)?.gameObject;

        if (pausePanel == null)
        {
            Debug.LogWarning(
                $"[PauseManager] '{PanelName}' が見つかりません。ポーズ UI は開きません。");
            enabled = false;
            return;
        }

        // 非表示にする前に、置かれているままのスケールを控えておく
        _pauseBaseScale = pausePanel.transform.localScale;

        // パッドの B / Esc で閉じられるようにする。
        // UI マップは EventSystem 側が常に有効にしているのでそのまま購読できる。
        _cancelAction = _playerInput != null
            ? _playerInput.actions?.FindAction("UI/Cancel")
            : null;

        if (_cancelAction != null) _cancelAction.performed += OnCancelPerformed;

        if (continueButton == null) continueButton = FindButton(ContinueName);
        if (restartButton  == null) restartButton  = FindButton(RestartName);
        if (titleButton    == null) titleButton    = FindButton(TitleName);
        if (settingsButton == null) settingsButton = FindButton(SettingName);

        pausePanel.SetActive(false);

        continueButton?.onClick.AddListener(ClosePause);
        restartButton?.onClick.AddListener(OnRestartClicked);
        titleButton?.onClick.AddListener(OnTitleClicked);

        ResolveSettingsPanel();
    }

    /// <summary>
    /// 背景をクリックすると EventSystem の選択が外れてしまい、
    /// そのままではスティックで何も動かせなくなる。
    /// ポーズ中だけ見張って選択を戻す。
    /// </summary>
    private void Update()
    {
        if (!IsPaused || !keepSelection) return;
        if (EventSystem.current == null) return;

        GameObject current = EventSystem.current.currentSelectedGameObject;

        if (current != null && current.activeInHierarchy)
        {
            _lastSelected = current;
            return;
        }

        Select(_lastSelected != null && _lastSelected.activeInHierarchy
             ? _lastSelected
             : FirstSelected());
    }

    // ─────────────────────────────────────────
    // 入力コールバック
    // ─────────────────────────────────────────

    private void OnPausePerformed(InputAction.CallbackContext ctx) => TogglePause();

    /// <summary>
    /// パッドの B / Esc。開いているものを 1 段階だけ閉じる。
    ///
    /// Esc は Pause（自前）と UI/Cancel の両方に反応するが、
    /// 各メソッドが「開いていなければ何もしない」ので二重には効かない。
    /// </summary>
    private void OnCancelPerformed(InputAction.CallbackContext ctx)
    {
        if (_settingsOpen) CloseSettings();
        else if (IsPaused) ClosePause();
    }

    /// <summary>ポーズ中なら閉じる、そうでなければ開く。</summary>
    public void TogglePause()
    {
        // 設定を開いているときは、まず設定だけ閉じる
        // （いきなりゲームに戻ると設定を閉じたつもりが再開してしまう）
        if (_settingsOpen)
        {
            CloseSettings();
            return;
        }

        if (IsPaused) ClosePause();
        else if (CanPause()) OpenPause();
    }

    // ─────────────────────────────────────────
    // 設定パネル
    // ─────────────────────────────────────────

    private bool _settingsOpen;

    private void ResolveSettingsPanel()
    {
        if (settingsPanel == null)
            settingsPanel = FindDeep(transform, SettingsPanelName)?.gameObject;

        if (settingsPanel == null)
        {
            Debug.LogWarning($"[PauseManager] '{SettingsPanelName}' が見つかりません。設定は開けません。");
            return;
        }

        if (settingsBackButton == null)
            settingsBackButton = FindDeep(settingsPanel.transform, SettingsBackName)?.GetComponent<Button>();


        // 非表示にする前に、置かれているままのスケールを控えておく
        _settingsBaseScale = settingsPanel.transform.localScale;

        settingsPanel.SetActive(false);

        settingsButton?.onClick.AddListener(OpenSettings);
        settingsBackButton?.onClick.AddListener(CloseSettings);
        settingsCloseButton?.onClick.AddListener(CloseSettings);
    }

    /// <summary>Button_Setting の onClick に登録済み。</summary>
    public void OpenSettings()
    {
        if (settingsPanel == null || _settingsOpen) return;

        _settingsOpen = true;


        settingsPanel.SetActive(true);
        settingsPanel.transform.SetAsLastSibling();   // ポーズ画面より手前
        PlayPanelIntro(settingsPanel, _settingsBaseScale);

        PlaySound(openSound);

        SelectFirstInSettings();
    }

    /// <summary>Button_Close の onClick に登録済み。</summary>
    public void CloseSettings()
    {
        if (!_settingsOpen) return;

        _settingsOpen = false;
        settingsPanel.SetActive(false);

        // ポーズメニューを操作できる状態に戻す
        PlaySound(closeSound);

        // 元いた「設定」ボタンに戻す
        Select(settingsButton != null ? settingsButton.gameObject : FirstSelected());
    }

    /// <summary>
    /// 設定パネル内の Selectable を、画面の上から順に縦一列でつなぎ直す。
    ///
    /// 既定の Automatic / Vertical ナビゲーションは
    /// 「シーン内の有効な Selectable 全部」から距離と方向で探すため、
    /// レイアウトや裏のメニュー次第で Back に届かないことがある。
    /// 明示指定（Explicit）にすれば必ず順番どおりに移動できる。
    ///
    /// 左右は空のままにしておく。
    /// Slider は「左右の移動先が無いとき」に値を変えるので、
    private void SelectFirstInSettings()
    {
        if (settingsPanel == null) return;

        Selectable first = settingsFirstSelected != null
            ? settingsFirstSelected
            : settingsPanel.GetComponentInChildren<Selectable>();

        if (first != null) Select(first.gameObject);
    }

    /// <summary>
    /// ポーズしてよい状況か。
    /// 開始演出中・チュートリアル停止中・クリア／ゲームオーバー後は開かない。
    /// </summary>
    private bool CanPause()
    {
        if (_player == null) return false;
        if (!_player.IsStageStarted) return false;
        if (GameManager.Instance != null && !GameManager.Instance.IsPlaying) return false;

        // すでに誰かが時間を止めている（チュートリアルのパネルなど）
        if (Time.timeScale == 0f) return false;

        return true;
    }

    // ─────────────────────────────────────────
    // ポーズ / 再開
    // ─────────────────────────────────────────

    public void OpenPause()
    {
        if (IsPaused) return;

        IsPaused = true;
        _previousTimeScale = Time.timeScale > 0f ? Time.timeScale : 1f;
        Time.timeScale = 0f;

        pausePanel.SetActive(true);
        pausePanel.transform.SetAsLastSibling();   // 他の UI より手前に出す
        PlayPanelIntro(pausePanel, _pauseBaseScale);

        // ゲーム側の入力を止める（ポーズ用アクションは別なので効き続ける）
        _playerInput?.DeactivateInput();

        PlaySound(openSound);
        SelectFirstButton();
    }

    /// <summary>再開。Button_Continue の onClick にも登録している。</summary>
    public void ClosePause()
    {
        if (!IsPaused) return;

        // 設定を開いたままゲームに戻らないようにする
        if (_settingsOpen)
        {
            _settingsOpen = false;
            settingsPanel?.SetActive(false);
        }

        IsPaused = false;
        pausePanel.SetActive(false);
        Time.timeScale = _previousTimeScale;

        _playerInput?.ActivateInput();

        PlaySound(closeSound);

        if (EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(null);
    }

    // ─────────────────────────────────────────
    // ボタン
    // ─────────────────────────────────────────

    public void OnRestartClicked()
    {
        // timeScale は GameManager 側のシーン読み込みで 1 に戻る
        IsPaused = false;
        GameManager.Instance?.Restart();
    }

    public void OnTitleClicked()
    {
        IsPaused = false;
        GameManager.Instance?.GoToTitle();
    }

    // 内部ヘルパー
    // ─────────────────────────────────────────

    private void PlaySound(AudioClip clip)
    {
        if (audioSource != null && clip != null) audioSource.PlayOneShot(clip);
    }

    /// <summary>
    /// パッドで操作できるよう最初のオブジェクトを選択させる。
    /// これが無いと、開いた直後はスティックを倒しても何も動かない。
    /// </summary>
    private void SelectFirstButton()
    {
        _lastSelected = null;
        Select(FirstSelected());
    }

    private GameObject FirstSelected()
    {
        if (firstSelected != null && firstSelected.activeInHierarchy)
            return firstSelected;

        Button fallback = continueButton != null ? continueButton
                        : restartButton != null ? restartButton
                        : titleButton;

        return fallback != null ? fallback.gameObject : null;
    }

    private void Select(GameObject target)
    {
        if (target == null || EventSystem.current == null) return;

        // 一度 null を挟まないと、同じ相手を選び直したとき
        // OnSelect が飛ばずにハイライトが戻らない
        EventSystem.current.SetSelectedGameObject(null);
        EventSystem.current.SetSelectedGameObject(target);
        _lastSelected = target;
    }

    // ─────────────────────────────────────────
    // 表示アニメーション
    // ─────────────────────────────────────────

    /// <summary>
    /// パネルをフェード＋ポップで出す。
    ///
    /// ポーズ中は Time.timeScale = 0 なので、Tween は必ず SetUpdate(true)。
    /// でないと 1 フレームも進まず、透明なまま止まって見える。
    /// </summary>
    private void PlayPanelIntro(GameObject panel, Vector3 baseScale)
    {
        if (panel == null) return;

        Transform t = panel.transform;
        CanvasGroup group = GetCanvasGroup(panel);

        DOTween.Kill(t);
        DOTween.Kill(group);

        group.alpha = 0f;
        group.DOFade(1f, panelFadeDuration)
             .SetEase(Ease.OutQuad)
             .SetUpdate(true);

        if (panelPopScale > 0f && !Mathf.Approximately(panelPopScale, 1f))
        {
            t.localScale = baseScale * panelPopScale;
            t.DOScale(baseScale, panelPopDuration)
             .SetEase(Ease.OutBack)
             .SetUpdate(true);
        }
    }

    /// <summary>
    /// パネル全体の操作可否を切り替える。
    ///
    /// SetActive(false) にしないのは、裏に見えたままにしておきたいから。
    /// CanvasGroup.interactable = false にすると、その配下の Selectable は
    /// IsInteractable() が false になり、方向キー移動の探索対象から外れる。
    /// </summary>
    /// <summary>フェード用の CanvasGroup。無ければ足す。</summary>
    private static CanvasGroup GetCanvasGroup(GameObject panel)
    {
        return panel.GetComponent<CanvasGroup>() ?? panel.AddComponent<CanvasGroup>();
    }

    private static void KillPanelTweens(GameObject panel)
    {
        if (panel == null) return;

        DOTween.Kill(panel.transform);

        CanvasGroup group = panel.GetComponent<CanvasGroup>();
        if (group != null) DOTween.Kill(group);
    }

    private Button FindButton(string name)
    {
        var found = FindDeep(pausePanel.transform, name);
        if (found == null)
        {
            Debug.LogWarning($"[PauseManager] '{name}' が {PanelName} の中に見つかりません。");
            return null;
        }
        return found.GetComponent<Button>();
    }

    /// <summary>非アクティブな子も含めて名前で探す。</summary>
    private static Transform FindDeep(Transform root, string name)
    {
        foreach (var t in root.GetComponentsInChildren<Transform>(true))
            if (t.name == name) return t;

        return null;
    }
}
