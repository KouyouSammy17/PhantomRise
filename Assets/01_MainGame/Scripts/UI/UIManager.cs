// ============================================================
// UIManager.cs
// ゲームクリア / ゲームオーバー UI の表示を管理する
//
// GameManager の OnGameClear / OnGameOver を「コードで」購読する。
//
// Inspector の UnityEvent でつなごうとすると、Game Manager プレハブから
// シーン側の UI Manager を参照できず m_Target が空のままになる。
// （実際 OnGameClear / OnGameOver の m_Target は {fileID: 0} だった）
// シーンごとに手で挿し直す運用は壊れやすいので、コード購読にする。
//
// パネル内のボタン（Play_Result / Play_Continue の中身）は
// 未割り当てなら名前で探して結線する。
// ============================================================

using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

public class UIManager : MonoBehaviour
{
    // ─────────────────────────────────────────
    // パネル内のボタン名（未割り当てのときの自動検索用）
    // ─────────────────────────────────────────

    private const string ClearRestartName = "RestartButton";   // Play_Result
    private const string ClearNextName    = "NextButton";
    private const string ClearTitleName   = "TitleButton";     // Play_Result
    private const string OverRestartName  = "Button_Restart";  // Play_Continue
    private const string OverTitleName    = "Button_Title";    // Play_Continue

    // ─────────────────────────────────────────
    // Inspector
    // ─────────────────────────────────────────

    [Header("=== ゲームクリア UI ===")]
    [SerializeField] private GameObject gameClearPanel;
    [SerializeField] private TextMeshProUGUI gameClearText;
    [SerializeField] private Button gameClearRestartButton;   // リスタートボタン
    [SerializeField] private Button gameClearNextButton;       // 次のステージへ進むボタン
    [SerializeField] private Button gameClearTitleButton;      // タイトルへ戻るボタン

    [Header("=== ゲームオーバー UI ===")]
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private TextMeshProUGUI gameOverText;
    [SerializeField] private Button gameOverRestartButton;    // リスタートボタン
    [SerializeField] private Button gameOverTitleButton;      // タイトルへ戻るボタン

    [Header("=== クリア時のミッション表示 ===")]
    [Tooltip("未割り当てなら同じ GameObject から探す")]
    [SerializeField] private StageClearAchievements achievements;

    [Header("=== 幽霊タイマー UI ===")]
    [SerializeField] private TextMeshProUGUI ghostTimerText;

    [Header("=== パッド操作 ===")]
    [Tooltip("パネルを開いたとき選択が外れていたら戻す")]
    [SerializeField] private bool keepSelection = true;

    [Header("=== 表示アニメーション ===")]
    [Tooltip("フェードインにかける時間")]
    [SerializeField] private float panelFadeDuration = 0.28f;

    [Tooltip("出てくるときの開始スケール（1 で拡大なし）")]
    [SerializeField] private float panelPopScale = 0.85f;

    [SerializeField] private float panelPopDuration = 0.42f;

    // ─────────────────────────────────────────
    // 内部
    // ─────────────────────────────────────────

    /// <summary>今開いているパネルで最初に選ばせるボタン</summary>
    private GameObject _selectedButton;

    // プレハブで設定されたスケール。決め打ちで 1 に戻すと
    // 拡大して置いてあるパネルが縮んでしまうので記録しておく
    private Vector3 _clearBaseScale = Vector3.one;
    private Vector3 _overBaseScale  = Vector3.one;

    // ─────────────────────────────────────────
    // Unity ライフサイクル
    // ─────────────────────────────────────────

    private void Start()
    {
        // 非表示にする前に、置かれているままのスケールを控えておく
        if (gameClearPanel != null) _clearBaseScale = gameClearPanel.transform.localScale;
        if (gameOverPanel  != null) _overBaseScale  = gameOverPanel.transform.localScale;

        gameClearPanel?.SetActive(false);
        gameOverPanel?.SetActive(false);

        if (achievements == null) achievements = GetComponent<StageClearAchievements>();

        ResolveAndBindButtons();

        // GameManager の通知をコードで受け取る（Inspector 結線に頼らない）
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnGameClear.AddListener(ShowGameClear);
            GameManager.Instance.OnGameOver.AddListener(ShowGameOver);
        }
        else
        {
            Debug.LogWarning("[UIManager] GameManager が見つかりません。クリア/ゲームオーバー UI は出ません。");
        }
    }

    private void OnDestroy()
    {
        KillPanelTweens(gameClearPanel);
        KillPanelTweens(gameOverPanel);

        if (GameManager.Instance == null) return;

        GameManager.Instance.OnGameClear.RemoveListener(ShowGameClear);
        GameManager.Instance.OnGameOver.RemoveListener(ShowGameOver);
    }

    /// <summary>
    /// 背景クリックなどで選択が外れるとパッドで何も押せなくなる。
    /// パネル表示中だけ見張って選択を戻す。
    /// </summary>
    private void Update()
    {
        if (!keepSelection || _selectedButton == null) return;
        if (EventSystem.current == null) return;
        if (EventSystem.current.currentSelectedGameObject != null) return;

        EventSystem.current.SetSelectedGameObject(_selectedButton);
    }

    // ─────────────────────────────────────────
    // GameManager のイベントにバインドするメソッド
    // ─────────────────────────────────────────

    /// <summary>GameManager.OnGameClear で呼ばれる</summary>
    public void ShowGameClear()
    {
        // Inspector 側でも結線された場合に二重で開かないようにする
        if (gameClearPanel == null || gameClearPanel.activeSelf) return;

        gameClearPanel.SetActive(true);
        gameClearPanel.transform.SetAsLastSibling();
        PlayPanelIntro(gameClearPanel, _clearBaseScale);

        // 最終ステージには「次へ」がないので、GameManager に聞いて出し分ける
        if (gameClearNextButton != null)
        {
            gameClearNextButton.gameObject.SetActive(
                GameManager.Instance != null && GameManager.Instance.HasNextScene);
        }

        if (gameClearText != null)
            gameClearText.text = "STAGE CLEAR!";

        // ミッション達成表示と星の演出
        achievements?.Show(gameClearPanel);

        SelectFirst(gameClearNextButton, gameClearRestartButton, gameClearTitleButton);
    }

    /// <summary>GameManager.OnGameOver で呼ばれる</summary>
    public void ShowGameOver()
    {
        if (gameOverPanel == null || gameOverPanel.activeSelf) return;

        gameOverPanel.SetActive(true);
        gameOverPanel.transform.SetAsLastSibling();
        PlayPanelIntro(gameOverPanel, _overBaseScale);

        if (gameOverText != null)
            gameOverText.text = "GAME OVER";

        SelectFirst(gameOverRestartButton, gameOverTitleButton);
    }

    // ─────────────────────────────────────────
    // ボタン
    // ─────────────────────────────────────────

    /// <summary>リスタート。ボタンの onClick に登録済み。</summary>
    public void OnRestartClicked()
    {
        GameManager.Instance?.Restart();
    }

    /// <summary>
    /// クリア画面の「次へ」ボタン。
    /// nextScene が空の場合は GameManager 側でタイトルへ戻る。
    /// </summary>
    public void OnNextClicked()
    {
        GameManager.Instance?.GoToNextStage();
    }

    /// <summary>タイトルへ戻る。</summary>
    public void OnTitleClicked()
    {
        GameManager.Instance?.GoToTitle();
    }

    // ─────────────────────────────────────────
    // 表示アニメーション
    // ─────────────────────────────────────────

    /// <summary>
    /// パネルをフェード＋ポップで出す。
    ///
    /// クリア / ゲームオーバーは GameManager が Time.timeScale = 0 にしてから
    /// 呼ばれるので、Tween は必ず SetUpdate(true)（未スケール時間）にする。
    /// でないとアニメーションが 1 フレームも進まない。
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

    /// <summary>フェード用の CanvasGroup。無ければ足す。</summary>
    private static CanvasGroup GetCanvasGroup(GameObject panel)
    {
        return panel.GetComponent<CanvasGroup>() ?? panel.AddComponent<CanvasGroup>();
    }

    private void KillPanelTweens(GameObject panel)
    {
        if (panel == null) return;

        DOTween.Kill(panel.transform);

        CanvasGroup group = panel.GetComponent<CanvasGroup>();
        if (group != null) DOTween.Kill(group);
    }

    // ─────────────────────────────────────────
    // ボタンの解決と結線
    // ─────────────────────────────────────────

    private void ResolveAndBindButtons()
    {
        if (gameClearRestartButton == null) gameClearRestartButton = FindButton(gameClearPanel, ClearRestartName);
        if (gameClearNextButton    == null) gameClearNextButton    = FindButton(gameClearPanel, ClearNextName);
        if (gameClearTitleButton   == null) gameClearTitleButton   = FindButton(gameClearPanel, ClearTitleName);

        if (gameOverRestartButton  == null) gameOverRestartButton  = FindButton(gameOverPanel, OverRestartName);
        if (gameOverTitleButton    == null) gameOverTitleButton    = FindButton(gameOverPanel, OverTitleName);

        // Inspector でも結線されていた場合に二重登録しないよう、
        // 一度外してから登録する
        Bind(gameClearRestartButton, OnRestartClicked);
        Bind(gameClearNextButton,    OnNextClicked);
        Bind(gameClearTitleButton,   OnTitleClicked);
        Bind(gameOverRestartButton,  OnRestartClicked);
        Bind(gameOverTitleButton,    OnTitleClicked);
    }

    private static void Bind(Button button, UnityEngine.Events.UnityAction action)
    {
        if (button == null) return;

        button.onClick.RemoveListener(action);
        button.onClick.AddListener(action);
    }

    /// <summary>非アクティブな子も含めて名前でボタンを探す。</summary>
    private static Button FindButton(GameObject panel, string name)
    {
        if (panel == null) return null;

        foreach (Transform t in panel.GetComponentsInChildren<Transform>(true))
            if (t.name == name) return t.GetComponent<Button>();

        return null;
    }

    /// <summary>
    /// パッドで操作できるよう、最初に見つかった有効なボタンを選択させる。
    /// これが無いとクリア / ゲームオーバー画面をパッドで操作できない。
    /// </summary>
    private void SelectFirst(params Button[] candidates)
    {
        _selectedButton = null;

        foreach (Button b in candidates)
        {
            if (b == null || !b.gameObject.activeInHierarchy || !b.interactable) continue;

            _selectedButton = b.gameObject;
            break;
        }

        if (_selectedButton == null || EventSystem.current == null) return;

        // 一度 null を挟まないと、同じ相手を選び直したとき OnSelect が飛ばない
        EventSystem.current.SetSelectedGameObject(null);
        EventSystem.current.SetSelectedGameObject(_selectedButton);
    }

    // ─────────────────────────────────────────
    // 幽霊タイマー表示
    // PlayerStateMachine.OnGhostTimerUpdate にバインドする
    // ─────────────────────────────────────────

    /// <summary>
    /// PlayerStateMachine.OnGhostTimerUpdate(float) にバインドする。
    /// 残り秒数を受け取って表示する。
    /// </summary>
    public void UpdateGhostTimer(float remaining)
    {
        if (ghostTimerText == null) return;

        if (remaining > 0f)
        {
            ghostTimerText.text = $"{remaining:F0}";
            // 残り10秒以下で赤くする
            ghostTimerText.color = remaining <= 10f ? Color.red : Color.white;
        }
        else
        {
            ghostTimerText.text = "0s";
            ghostTimerText.color = Color.red;
        }
    }

    /// <summary>
    /// 乗っ取り状態に入ったときタイマーUIを非表示にする。
    /// PlayerStateMachine などから呼ぶ。
    /// </summary>
    public void HideGhostTimer()
    {
        if (ghostTimerText != null)
            ghostTimerText.gameObject.SetActive(false);
    }

    /// <summary>
    /// 幽霊状態に戻ったときタイマーUIを再表示する。
    /// </summary>
    public void ShowGhostTimer()
    {
        if (ghostTimerText != null)
            ghostTimerText.gameObject.SetActive(true);
    }
}
