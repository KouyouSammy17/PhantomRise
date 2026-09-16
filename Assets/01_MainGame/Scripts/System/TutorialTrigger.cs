// ============================================================
// TutorialTrigger.cs
// プレイヤーが範囲に入ったらチュートリアルのパネルを表示する。
//
// TutorialTriggerを複数設置している場合でも、
// どれか1つのスキップ確認で「はい」を押すと、
// 以降のTutorialTriggerもすべてチュートリアルを表示しない。
//
// スキップボタンを押しただけではスキップしない。
//   ↓
// 「本当にスキップしますか？」を表示
//   ↓
// 「はい」   → チュートリアル全体をスキップ
// 「いいえ」 → 元のチュートリアルパネルに戻る
//
// 表示中:
//   ・Time.timeScale = 0 でゲームを止める
//   ・操作を UI アクションマップに切り替える
//   ・パネルのボタンを自動選択する
//
// パネルの出入りは DOTween でフェードする。スキップ確認画面も同じ。
// timeScale = 0 で止めているので、Tween は必ず SetUpdate(true)。
//
// 【配線について】
//   スキップボタン / 確認画面 / はい / いいえ は Inspector で
//   設定しなくてもよい。空のときは Start で Tutorial プレハブから
//   名前で探す（Skip*, Skipconfimpanel, YesButton, NoButton）。
//   確認画面の文字は Strings.csv の
//   tutorial.skip_confirm / tutorial.skip_yes / tutorial.skip_no を使う。
//
//   パネル送り（決定）は、各パネルに置いてある「Close」ボタン
//   （Button.prefab のインスタンス）が受ける。これを選択しておくので
//   ButtonAnimator が効き、選ばれているのが見た目でわかる。
//   スキップボタンは別の Button なので、決定で進む・スキップで確認、と
//   役割が混ざらない。
//   Close ボタンが無いパネルだけ、保険としてパネル自身に
//   見えない Button を付けて決定を受ける。
//
// 【選択（Navigation）について】
//   同じ画面に「送りボタン（ルート・見えない）」「スキップボタン」
//   「はい」「いいえ」が同時に居るので、Navigation を自動のままにすると
//   スティックを倒しただけで選択が別のボタンへ逃げてしまう。
//   （画面いっぱいのルートボタンは中心が近いので特に拾われやすい）
//   そこで、
//     ・送りボタン / スキップボタン … Navigation = None（選択を奪わせない）
//     ・はい ⇔ いいえ              … Explicit で相互に結ぶ（上下左右どれでも）
//   としている。スキップはマウスクリックと START ボタンから開く。
//
// 【START ボタン】
//   チュートリアル表示中に Xbox の START（/ Esc）を押すと、
//   スキップボタンを押したのと同じ確認画面が出る。
//   もう一度押すと確認画面を閉じて元のページに戻る。
//   PauseManager の CanPause() は timeScale == 0 のとき false を返すので、
//   チュートリアル中の START でポーズ画面が開くことはない。
// ============================================================

using DG.Tweening;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class TutorialTrigger : MonoBehaviour
{
    // ─────────────────────────────────────────
    // Inspector
    // ─────────────────────────────────────────

    [SerializeField] private GameObject[] panels;

    [Header("=== スキップボタン ===")]
    [Tooltip("このTutorialTriggerが担当するパネルのスキップボタンを設定")]
    [SerializeField] private Button[] skipButtons;

    [Header("=== スキップ確認画面 ===")]
    [Tooltip("「本当にスキップしますか？」と表示するパネル")]
    [SerializeField] private GameObject skipConfirmPanel;

    [Tooltip("スキップ確認画面の「はい」ボタン")]
    [SerializeField] private Button yesButton;

    [Tooltip("スキップ確認画面の「いいえ」ボタン")]
    [SerializeField] private Button noButton;

    [Header("=== START ボタンでスキップ ===")]
    [Tooltip("チュートリアル中に START / Esc を押したらスキップ確認を出す")]
    [SerializeField] private bool skipWithStartButton = true;

    [Header("=== 効果音 ===")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip enterSound;

    [Header("=== フェード ===")]
    [SerializeField] private float fadeInDuration = 0.25f;
    [SerializeField] private float fadeOutDuration = 0.18f;

    [Tooltip("スキップ確認画面のフェード時間（出入りとも）")]
    [SerializeField] private float confirmFadeDuration = 0.15f;

    // ─────────────────────────────────────────
    // アクションマップ名
    // ─────────────────────────────────────────

    private const string UIMapName = "UI";
    private const string PlayerMapName = "Player";

    // ─────────────────────────────────────────
    // チュートリアル全体で共有する状態
    // ─────────────────────────────────────────

    // 「はい」を押した場合のみ true になる。
    //
    // staticなので、4つのTutorialTriggerで共有される。
    // シーンを読み直しても static は残るので、
    // 「やり直し」では GameManager から ResetSkipFlag() で明示的に戻す。
    private static bool _tutorialSkipped;

    /// <summary>
    /// スキップ状態を忘れる（＝次にトリガーに入ったらまたチュートリアルが出る）。
    /// ステージを最初からやり直すときに GameManager から呼ぶ。
    /// </summary>
    public static void ResetSkipFlag() => _tutorialSkipped = false;

    // ─────────────────────────────────────────
    // 内部
    // ─────────────────────────────────────────

    private bool _isShown;
    private bool _isTutorialActive;
    private bool _isTransitioning;
    private bool _isSkipConfirmOpen;

    private int _currentIndex;

    private PlayerInput _playerInput;
    private string _previousMap;

    private GameObject _selectedButton;

    /// <summary>START / Esc。PauseManager と同じ作り方（コードで組む）。</summary>
    private InputAction _skipAction;

    // スキップ確認画面を開く前に
    // 選択されていたボタンを記憶しておく
    private GameObject _previousSelectedButton;

    // ─────────────────────────────────────────
    // Unityライフサイクル
    // ─────────────────────────────────────────

    private void Awake()
    {
        if (!skipWithStartButton) return;

        _skipAction = new InputAction("TutorialSkip", InputActionType.Button);
        _skipAction.AddBinding("<Gamepad>/start");    // Xbox の START ボタン
        _skipAction.AddBinding("<Keyboard>/escape");
        _skipAction.performed += OnSkipButtonPerformed;
    }

    private void OnEnable()  => _skipAction?.Enable();
    private void OnDisable() => _skipAction?.Disable();

    private void Start()
    {
        AutoWireSkipUI();

        // チュートリアルパネルを初期化
        if (panels != null)
        {
            foreach (GameObject panel in panels)
            {
                if (panel == null) continue;

                CanvasGroup group = GetCanvasGroup(panel);

                group.alpha = 0f;
                group.interactable = false;
                group.blocksRaycasts = false;

                panel.SetActive(false);
            }
        }

        // スキップ確認画面を初期化
        if (skipConfirmPanel != null)
        {
            CanvasGroup group = GetCanvasGroup(skipConfirmPanel);

            group.alpha = 0f;
            group.interactable = false;
            group.blocksRaycasts = false;

            skipConfirmPanel.SetActive(false);
        }

        // スキップボタンを登録
        if (skipButtons != null)
        {
            foreach (Button skipButton in skipButtons)
            {
                if (skipButton == null) continue;

                skipButton.onClick.RemoveListener(OpenSkipConfirm);
                skipButton.onClick.AddListener(OpenSkipConfirm);
            }
        }

        // 「はい」ボタン
        if (yesButton != null)
        {
            yesButton.onClick.RemoveListener(ConfirmSkip);
            yesButton.onClick.AddListener(ConfirmSkip);
        }

        // 「いいえ」ボタン
        if (noButton != null)
        {
            noButton.onClick.RemoveListener(CancelSkip);
            noButton.onClick.AddListener(CancelSkip);
        }
    }

    private void OnDestroy()
    {
        if (_skipAction != null)
        {
            _skipAction.performed -= OnSkipButtonPerformed;
            _skipAction.Dispose();
            _skipAction = null;
        }

        // DOTweenを停止
        if (panels != null)
        {
            foreach (GameObject panel in panels)
            {
                if (panel == null) continue;

                CanvasGroup group = panel.GetComponent<CanvasGroup>();

                if (group != null)
                    DOTween.Kill(group);
            }
        }

        if (skipConfirmPanel != null)
        {
            CanvasGroup confirmGroup = skipConfirmPanel.GetComponent<CanvasGroup>();

            if (confirmGroup != null) DOTween.Kill(confirmGroup);
        }

        // スキップボタンのイベント解除
        if (skipButtons != null)
        {
            foreach (Button skipButton in skipButtons)
            {
                if (skipButton == null) continue;

                skipButton.onClick.RemoveListener(OpenSkipConfirm);
            }
        }

        if (yesButton != null)
            yesButton.onClick.RemoveListener(ConfirmSkip);

        if (noButton != null)
            noButton.onClick.RemoveListener(CancelSkip);
    }

    private void Update()
    {
        if (!_isTutorialActive) return;
        if (EventSystem.current == null) return;

        // 確認画面が出ている間は、選択を「はい」「いいえ」の中に留める
        if (_isSkipConfirmOpen)
        {
            KeepSelectionInsideConfirm();
            return;
        }

        if (_selectedButton == null) return;

        if (EventSystem.current.currentSelectedGameObject != null)
            return;

        EventSystem.current.SetSelectedGameObject(_selectedButton);
    }

    /// <summary>
    /// 確認画面の外に選択が出てしまったら「はい」に戻す。
    /// （マウスで画面のどこかを押したときなど、選択が外れることがある）
    /// </summary>
    private void KeepSelectionInsideConfirm()
    {
        if (skipConfirmPanel == null) return;

        GameObject current = EventSystem.current.currentSelectedGameObject;

        if (current != null &&
            current.activeInHierarchy &&
            current.transform.IsChildOf(skipConfirmPanel.transform))
            return;

        GameObject fallback = yesButton != null ? yesButton.gameObject
                            : noButton  != null ? noButton.gameObject
                            : null;

        if (fallback != null)
            EventSystem.current.SetSelectedGameObject(fallback);
    }

    // ─────────────────────────────────────────
    // トリガー
    // ─────────────────────────────────────────

    private void OnTriggerEnter(Collider other)
    {
        // すでに「はい」でスキップされていたら何もしない
        if (_tutorialSkipped) return;

        if (_isShown) return;
        if (!other.CompareTag("Player")) return;
        if (panels == null || panels.Length == 0) return;

        _isShown = true;
        _isTutorialActive = true;
        _isSkipConfirmOpen = false;

        _currentIndex = 0;

        Time.timeScale = 0f;

        SwitchToUIControls();

        ShowPanel(_currentIndex);
    }

    // ─────────────────────────────────────────
    // パネル表示
    // ─────────────────────────────────────────

    private void ShowPanel(int index)
    {
        if (!_isTutorialActive) return;
        if (_tutorialSkipped) return;
        if (_isSkipConfirmOpen) return;

        if (index < 0 || index >= panels.Length)
        {
            Finish();
            return;
        }

        GameObject panel = panels[index];

        if (panel == null)
        {
            Next();
            return;
        }

        panel.SetActive(true);
        panel.transform.SetAsLastSibling();

        CanvasGroup group = GetCanvasGroup(panel);

        group.alpha = 0f;
        group.interactable = true;
        group.blocksRaycasts = true;

        _isTransitioning = true;

        DOTween.Kill(group);

        group.DOFade(1f, fadeInDuration)
             .SetEase(Ease.OutQuad)
             .SetUpdate(true)
             .OnComplete(() =>
             {
                 if (!_isTutorialActive ||
                     _tutorialSkipped ||
                     _isSkipConfirmOpen)
                     return;

                 _isTransitioning = false;
             });

        SelectPanelButton(panel);
    }

    // ─────────────────────────────────────────
    // パネル送り
    // ─────────────────────────────────────────

    private void OnPanelSubmit()
    {
        if (!_isTutorialActive) return;
        if (_isTransitioning) return;
        if (_tutorialSkipped) return;
        if (_isSkipConfirmOpen) return;

        if (audioSource != null && enterSound != null)
            audioSource.PlayOneShot(enterSound);

        Next();
    }

    private void Next()
    {
        if (!_isTutorialActive) return;
        if (_tutorialSkipped) return;
        if (_isSkipConfirmOpen) return;

        if (_currentIndex < 0 ||
            _currentIndex >= panels.Length)
        {
            Finish();
            return;
        }

        GameObject current = panels[_currentIndex];

        _currentIndex++;

        bool hasNext = _currentIndex < panels.Length;

        FadeOut(current, () =>
        {
            if (!_isTutorialActive) return;
            if (_tutorialSkipped) return;
            if (_isSkipConfirmOpen) return;

            if (hasNext)
                ShowPanel(_currentIndex);
            else
                Finish();
        });
    }

    private void FadeOut(GameObject panel, System.Action onComplete)
    {
        if (panel == null)
        {
            onComplete?.Invoke();
            return;
        }

        if (!_isTutorialActive) return;
        if (_tutorialSkipped) return;
        if (_isSkipConfirmOpen) return;

        _isTransitioning = true;

        CanvasGroup group = GetCanvasGroup(panel);

        group.interactable = false;
        group.blocksRaycasts = false;

        DOTween.Kill(group);

        group.DOFade(0f, fadeOutDuration)
             .SetEase(Ease.InQuad)
             .SetUpdate(true)
             .OnComplete(() =>
             {
                 panel.SetActive(false);

                 if (!_isTutorialActive) return;
                 if (_tutorialSkipped) return;
                 if (_isSkipConfirmOpen) return;

                 onComplete?.Invoke();
             });
    }

    // ─────────────────────────────────────────
    // スキップ確認画面を開く
    // ─────────────────────────────────────────

    /// <summary>
    /// START / Esc。
    /// チュートリアル表示中だけ効く（他の TutorialTrigger は _isTutorialActive が
    /// false なので何もしない）。確認画面が出ていればそれを閉じる。
    /// </summary>
    private void OnSkipButtonPerformed(InputAction.CallbackContext ctx)
    {
        if (!_isTutorialActive) return;
        if (_tutorialSkipped) return;

        if (_isSkipConfirmOpen) CancelSkip();
        else                    OpenSkipConfirm();
    }

    private void OpenSkipConfirm()
    {
        if (!_isTutorialActive) return;
        if (_tutorialSkipped) return;
        if (_isSkipConfirmOpen) return;

        Debug.Log("=== スキップ確認画面を表示 ===");

        _isSkipConfirmOpen = true;

        // 現在選択されているボタンを保存
        if (EventSystem.current != null)
        {
            _previousSelectedButton =
                EventSystem.current.currentSelectedGameObject;
        }

        // 裏のチュートリアルパネルを触れないようにする。
        // 残しておくと、左右キーで裏のボタンへ選択が逃げてしまう。
        SetCurrentPanelInteractable(false);

        // 確認画面を表示（フェードイン）
        ShowSkipConfirm();

        // 「はい」を自動選択
        if (EventSystem.current != null && yesButton != null)
        {
            EventSystem.current.SetSelectedGameObject(null);
            EventSystem.current.SetSelectedGameObject(yesButton.gameObject);
        }
    }

    // ─────────────────────────────────────────
    // 「はい」
    // ─────────────────────────────────────────

    private void ConfirmSkip()
    {
        if (!_isTutorialActive) return;
        if (!_isSkipConfirmOpen) return;

        Debug.Log("=== チュートリアルをスキップしました ===");

        // ★ここで初めてtrueにする
        _tutorialSkipped = true;

        _isSkipConfirmOpen = false;
        _isTutorialActive = false;
        _isTransitioning = false;

        _currentIndex =
            panels != null ? panels.Length : 0;

        // 確認画面を閉じる（フェードアウト）
        HideSkipConfirm();

        // このTutorialTriggerのパネルをすべて非表示
        if (panels != null)
        {
            foreach (GameObject panel in panels)
            {
                if (panel == null) continue;

                CanvasGroup group =
                    panel.GetComponent<CanvasGroup>();

                if (group != null)
                {
                    DOTween.Kill(group);

                    group.alpha = 0f;
                    group.interactable = false;
                    group.blocksRaycasts = false;
                }

                panel.SetActive(false);
            }
        }

        _selectedButton = null;
        _previousSelectedButton = null;

        if (EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(null);
        }

        RestoreControls();

        Time.timeScale = 1f;
    }

    // ─────────────────────────────────────────
    // 「いいえ」
    // ─────────────────────────────────────────

    private void CancelSkip()
    {
        if (!_isTutorialActive) return;
        if (!_isSkipConfirmOpen) return;

        Debug.Log("=== スキップをキャンセル ===");

        _isSkipConfirmOpen = false;

        // 確認画面を閉じる（フェードアウト）
        HideSkipConfirm();

        // 元のチュートリアルパネルを再び操作可能にする
        SetCurrentPanelInteractable(true);

        // 送りボタンに選択を戻す。
        // 直前の選択（スキップボタンのことがある）に戻すと、
        // そのまま決定を押したときにまた確認画面が出てしまう。
        if (EventSystem.current != null)
        {
            GameObject next = _selectedButton != null
                            ? _selectedButton
                            : _previousSelectedButton;

            EventSystem.current.SetSelectedGameObject(null);

            if (next != null)
                EventSystem.current.SetSelectedGameObject(next);
        }

        _previousSelectedButton = null;
    }

    // ─────────────────────────────────────────
    // 通常終了
    // ─────────────────────────────────────────

    private void Finish()
    {
        _isTutorialActive = false;
        _isSkipConfirmOpen = false;
        _isTransitioning = false;

        _selectedButton = null;
        _previousSelectedButton = null;

        // 確認画面を閉じる（フェードアウト）
        HideSkipConfirm();

        if (panels != null)
        {
            foreach (GameObject panel in panels)
            {
                if (panel == null) continue;

                CanvasGroup group =
                    panel.GetComponent<CanvasGroup>();

                if (group != null)
                {
                    DOTween.Kill(group);

                    group.alpha = 0f;
                    group.interactable = false;
                    group.blocksRaycasts = false;
                }

                panel.SetActive(false);
            }
        }

        if (EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(null);
        }

        RestoreControls();

        Time.timeScale = 1f;
    }

    // ─────────────────────────────────────────
    // 選択
    // ─────────────────────────────────────────

    private void SelectPanelButton(GameObject panel)
    {
        // パネルに置いてある「Close」ボタンを決定ボタンにする。
        // 見えるボタンなので、選択すると ButtonAnimator が動く。
        Button button = FindPanelCloseButton(panel);

        if (button == null)
        {
            // Close ボタンが無いパネル用の保険。
            // パネル自身を決定で送れるようにする（見た目には出ない）。
            button = panel.GetComponent<Button>();

            if (button == null)
            {
                button = panel.AddComponent<Button>();
                button.transition =
                    Selectable.Transition.None;
            }

            // スティックを倒しても選択が動かないようにする
            // （Close ボタン側はプレハブで Navigation = None にしてある）
            SetNavigationNone(button);
        }

        button.onClick.RemoveListener(OnPanelSubmit);
        button.onClick.AddListener(OnPanelSubmit);

        _selectedButton = button.gameObject;

        if (EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(null);

            EventSystem.current.SetSelectedGameObject(
                _selectedButton);
        }
    }

    // ─────────────────────────────────────────
    // 操作切り替え
    // ─────────────────────────────────────────

    private void SwitchToUIControls()
    {
        if (_playerInput == null)
        {
            PlayerStateMachine player =
                FindAnyObjectByType<PlayerStateMachine>();

            if (player != null)
            {
                _playerInput =
                    player.GetComponent<PlayerInput>();
            }
        }

        if (_playerInput == null) return;

        _previousMap =
            _playerInput.currentActionMap != null
                ? _playerInput.currentActionMap.name
                : PlayerMapName;

        _playerInput.SwitchCurrentActionMap(
            UIMapName);
    }

    private void RestoreControls()
    {
        if (_playerInput == null) return;

        _playerInput.SwitchCurrentActionMap(
            string.IsNullOrEmpty(_previousMap)
                ? PlayerMapName
                : _previousMap);

        _playerInput.actions
            ?.FindActionMap(UIMapName)
            ?.Enable();
    }

    // ─────────────────────────────────────────
    // 選択（Navigation）まわり
    // ─────────────────────────────────────────

    /// <summary>
    /// パネルを送る「Close」ボタンを探す。
    /// スキップボタンと、保険でパネル自身に付けた Button は除く。
    /// </summary>
    private Button FindPanelCloseButton(GameObject panel)
    {
        foreach (Button b in panel.GetComponentsInChildren<Button>(true))
        {
            if (b.gameObject == panel) continue;   // ルートに付けた保険の Button
            if (IsSkipButton(b)) continue;         // スキップボタン

            return b;
        }

        return null;
    }

    private bool IsSkipButton(Button button)
    {
        if (button.name.StartsWith(SkipButtonPrefix)) return true;

        if (skipButtons != null)
        {
            foreach (Button b in skipButtons)
            {
                if (b == button) return true;
            }
        }

        return false;
    }

    /// <summary>スティックやキーで選択が移ってこないようにする。</summary>
    private static void SetNavigationNone(Selectable selectable)
    {
        if (selectable == null) return;

        Navigation nav = selectable.navigation;
        nav.mode = Navigation.Mode.None;
        selectable.navigation = nav;
    }

    /// <summary>2 つのボタンを、上下左右どの方向でも行き来できるように結ぶ。</summary>
    private static void LinkTwoWay(Selectable a, Selectable b)
    {
        if (a == null || b == null) return;

        a.navigation = ExplicitTo(a.navigation, b);
        b.navigation = ExplicitTo(b.navigation, a);
    }

    private static Navigation ExplicitTo(Navigation nav, Selectable target)
    {
        nav.mode          = Navigation.Mode.Explicit;
        nav.selectOnLeft  = target;
        nav.selectOnRight = target;
        nav.selectOnUp    = target;
        nav.selectOnDown  = target;
        return nav;
    }

    /// <summary>
    /// 今出しているチュートリアルパネルを操作できる／できないにする。
    /// blocksRaycasts はそのままにして、クリックが裏へ抜けないようにしておく。
    /// </summary>
    private void SetCurrentPanelInteractable(bool value)
    {
        if (panels == null) return;
        if (_currentIndex < 0 || _currentIndex >= panels.Length) return;

        GameObject panel = panels[_currentIndex];

        if (panel == null) return;

        GetCanvasGroup(panel).interactable = value;
    }

    // ─────────────────────────────────────────
    // スキップ確認画面のフェード
    // ─────────────────────────────────────────

    /// <summary>確認画面をフェードインで出す。</summary>
    private void ShowSkipConfirm()
    {
        if (skipConfirmPanel == null) return;

        skipConfirmPanel.SetActive(true);
        skipConfirmPanel.transform.SetAsLastSibling();

        CanvasGroup group = GetCanvasGroup(skipConfirmPanel);

        DOTween.Kill(group);

        // フェード中でも「はい」を押せるようにしておく
        group.interactable = true;
        group.blocksRaycasts = true;

        group.DOFade(1f, confirmFadeDuration)
             .SetEase(Ease.OutQuad)
             .SetUpdate(true);
    }

    /// <summary>確認画面をフェードアウトで閉じる。</summary>
    private void HideSkipConfirm()
    {
        if (skipConfirmPanel == null) return;

        CanvasGroup group = GetCanvasGroup(skipConfirmPanel);

        DOTween.Kill(group);

        group.interactable = false;
        group.blocksRaycasts = false;

        if (!skipConfirmPanel.activeSelf)
        {
            group.alpha = 0f;
            return;
        }

        group.DOFade(0f, confirmFadeDuration)
             .SetEase(Ease.InQuad)
             .SetUpdate(true)
             .OnComplete(() =>
             {
                 // 消し終わる前に開き直されていたら、そのまま出しておく
                 if (_isSkipConfirmOpen) return;

                 skipConfirmPanel.SetActive(false);
             });
    }

    // ─────────────────────────────────────────
    // スキップ UI の自動配線
    // ─────────────────────────────────────────

    private const string SkipButtonPrefix   = "Skip";
    private const string SkipConfirmName    = "Skipconfimpanel";
    private const string YesButtonName      = "YesButton";
    private const string NoButtonName       = "NoButton";

    /// <summary>
    /// Inspector で未設定のスキップ関連をプレハブの名前から探して埋める。
    /// 確認画面の文字には LocalizedText を付けて日英を切り替える。
    /// </summary>
    private void AutoWireSkipUI()
    {
        if (panels == null || panels.Length == 0) return;

        // スキップボタン: 各パネルの子で "Skip" から始まる Button
        if (skipButtons == null || skipButtons.Length == 0)
        {
            var found = new List<Button>();

            foreach (GameObject panel in panels)
            {
                if (panel == null) continue;

                foreach (Button b in panel.GetComponentsInChildren<Button>(true))
                {
                    if (b.gameObject == panel) continue;
                    if (b.name.StartsWith(SkipButtonPrefix))
                        found.Add(b);
                }
            }

            skipButtons = found.ToArray();
        }

        // 確認画面: パネルの親（Tutorial ルート）直下の Skipconfimpanel
        if (skipConfirmPanel == null)
        {
            Transform root = null;

            foreach (GameObject panel in panels)
            {
                if (panel != null) { root = panel.transform.parent; break; }
            }

            if (root != null)
            {
                Transform t = root.Find(SkipConfirmName);

                if (t == null)
                {
                    // 名前が少し違っても拾えるように
                    foreach (Transform child in root)
                    {
                        string n = child.name.ToLowerInvariant();
                        if (n.Contains("skip") && n.Contains("conf")) { t = child; break; }
                    }
                }

                if (t != null) skipConfirmPanel = t.gameObject;
            }
        }

        if (skipConfirmPanel == null)
        {
            Debug.LogWarning("[TutorialTrigger] スキップ確認画面が見つかりません。スキップは動きません。", this);
            return;
        }

        // はい / いいえ
        foreach (Button b in skipConfirmPanel.GetComponentsInChildren<Button>(true))
        {
            if (yesButton == null && b.name == YesButtonName) yesButton = b;
            if (noButton  == null && b.name == NoButtonName)  noButton  = b;
        }

        // スキップボタンは選択を奪わない（マウスと START から開く）
        if (skipButtons != null)
        {
            foreach (Button b in skipButtons) SetNavigationNone(b);
        }

        // 「はい」⇔「いいえ」を上下左右どれでも行き来できるように結ぶ
        LinkTwoWay(yesButton, noButton);

        // 文字の翻訳
        LocalizeChildText(skipConfirmPanel, "tutorial.skip_confirm", exclude: new[] { yesButton, noButton });
        if (yesButton != null) LocalizeChildText(yesButton.gameObject, "tutorial.skip_yes");
        if (noButton  != null) LocalizeChildText(noButton.gameObject,  "tutorial.skip_no");
    }

    /// <summary>
    /// root 以下の最初の TMP_Text に LocalizedText を付けてキーを入れる。
    /// exclude の下にある TMP は飛ばす（ボタンの文字と本文を分けるため）。
    /// </summary>
    private static void LocalizeChildText(GameObject root, string key, Button[] exclude = null)
    {
        foreach (TMP_Text tmp in root.GetComponentsInChildren<TMP_Text>(true))
        {
            bool skip = false;

            if (exclude != null)
            {
                foreach (Button ex in exclude)
                {
                    if (ex != null && tmp.transform.IsChildOf(ex.transform)) { skip = true; break; }
                }
            }

            if (skip) continue;

            LocalizedText loc = tmp.GetComponent<LocalizedText>()
                             ?? tmp.gameObject.AddComponent<LocalizedText>();
            loc.Key = key;
            return;
        }
    }

    // ─────────────────────────────────────────
    // ヘルパー
    // ─────────────────────────────────────────

    private static CanvasGroup GetCanvasGroup(
        GameObject panel)
    {
        return panel.GetComponent<CanvasGroup>()
            ?? panel.AddComponent<CanvasGroup>();
    }
}

