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
    private const string RestartName  = "Button_ReStart";
    private const string TitleName    = "Button_GiveUp";

    // ─────────────────────────────────────────
    // Inspector
    // ─────────────────────────────────────────

    [Header("=== ポーズ UI（未割り当てなら名前で自動検索）===")]
    [Tooltip("UI Manager の子にある Play_Pause")]
    [SerializeField] private GameObject pausePanel;
    [SerializeField] private Button continueButton;   // Button_Continue → 再開
    [SerializeField] private Button restartButton;    // Button_ReStart  → やり直し
    [SerializeField] private Button titleButton;      // Button_GiveUp   → タイトルへ

    [Header("=== パッド操作 ===")]
    [Tooltip("ポーズを開いたとき最初に選択するオブジェクト。空なら Button_Continue")]
    [SerializeField] private GameObject firstSelected;

    [Tooltip("背景クリックなどで選択が外れたとき、選択を戻す")]
    [SerializeField] private bool keepSelection = true;

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

        if (continueButton == null) continueButton = FindButton(ContinueName);
        if (restartButton  == null) restartButton  = FindButton(RestartName);
        if (titleButton    == null) titleButton    = FindButton(TitleName);

        pausePanel.SetActive(false);

        continueButton?.onClick.AddListener(ClosePause);
        restartButton?.onClick.AddListener(OnRestartClicked);
        titleButton?.onClick.AddListener(OnTitleClicked);
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

    /// <summary>ポーズ中なら閉じる、そうでなければ開く。</summary>
    public void TogglePause()
    {
        if (IsPaused) ClosePause();
        else if (CanPause()) OpenPause();
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

        // ゲーム側の入力を止める（ポーズ用アクションは別なので効き続ける）
        _playerInput?.DeactivateInput();

        PlaySound(openSound);
        SelectFirstButton();
    }

    /// <summary>再開。Button_Continue の onClick にも登録している。</summary>
    public void ClosePause()
    {
        if (!IsPaused) return;

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

    // ─────────────────────────────────────────
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
