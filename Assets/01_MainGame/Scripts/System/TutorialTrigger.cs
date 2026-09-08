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
// パネルの出入りは DOTween でフェードする。
// timeScale = 0 で止めているので、Tween は必ず SetUpdate(true)。
// ============================================================

using DG.Tweening;
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

    [Header("=== 効果音 ===")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip enterSound;

    [Header("=== フェード ===")]
    [SerializeField] private float fadeInDuration = 0.25f;
    [SerializeField] private float fadeOutDuration = 0.18f;

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
    private static bool _tutorialSkipped;

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

    // スキップ確認画面を開く前に
    // 選択されていたボタンを記憶しておく
    private GameObject _previousSelectedButton;

    // ─────────────────────────────────────────
    // Unityライフサイクル
    // ─────────────────────────────────────────

    private void Start()
    {
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
        if (_isSkipConfirmOpen) return;
        if (_selectedButton == null) return;
        if (EventSystem.current == null) return;

        if (EventSystem.current.currentSelectedGameObject != null)
            return;

        EventSystem.current.SetSelectedGameObject(_selectedButton);
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

        // 確認画面を表示
        if (skipConfirmPanel != null)
        {
            skipConfirmPanel.SetActive(true);

            skipConfirmPanel.transform.SetAsLastSibling();
        }

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

        // 確認画面を非表示
        if (skipConfirmPanel != null)
        {
            skipConfirmPanel.SetActive(false);
        }

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

        // 確認画面を閉じる
        if (skipConfirmPanel != null)
        {
            skipConfirmPanel.SetActive(false);
        }

        // 元のチュートリアルパネルを再び操作可能にする
        if (panels != null &&
            _currentIndex >= 0 &&
            _currentIndex < panels.Length)
        {
            GameObject currentPanel =
                panels[_currentIndex];

            if (currentPanel != null)
            {
                CanvasGroup group =
                    GetCanvasGroup(currentPanel);

                group.interactable = true;
                group.blocksRaycasts = true;
            }
        }

        // 元のパネルのボタンを選択
        if (EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(null);

            if (_previousSelectedButton != null)
            {
                EventSystem.current.SetSelectedGameObject(
                    _previousSelectedButton);
            }
            else if (_selectedButton != null)
            {
                EventSystem.current.SetSelectedGameObject(
                    _selectedButton);
            }
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

        // 確認画面を閉じる
        if (skipConfirmPanel != null)
        {
            skipConfirmPanel.SetActive(false);
        }

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
        Button button =
            panel.GetComponentInChildren<Button>(true);

        if (button == null)
        {
            button = panel.AddComponent<Button>();
            button.transition =
                Selectable.Transition.None;
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
    // ヘルパー
    // ─────────────────────────────────────────

    private static CanvasGroup GetCanvasGroup(
        GameObject panel)
    {
        return panel.GetComponent<CanvasGroup>()
            ?? panel.AddComponent<CanvasGroup>();
    }
}

