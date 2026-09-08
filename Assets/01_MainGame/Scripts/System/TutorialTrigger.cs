// ============================================================
// TutorialTrigger.cs
// プレイヤーが範囲に入ったらチュートリアルのパネルを表示する。
//
// TutorialTriggerを複数設置している場合でも、
// どれか1つのスキップボタンを押すと、
// 以降のTutorialTriggerもすべてチュートリアルを表示しない。
//
// 表示中:
//   ・Time.timeScale = 0 でゲームを止める
//   ・操作を UI アクションマップに切り替える
//   ・パネルのボタンを自動選択する
//   ・スキップボタンを押すとチュートリアル全体を終了
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

    // どれか1つのTutorialTriggerでスキップしたら、
    // すべてのTutorialTriggerがチュートリアルを表示しない。
    private static bool _tutorialSkipped;

    // ─────────────────────────────────────────
    // 内部
    // ─────────────────────────────────────────

    private bool _isShown;
    private bool _isTutorialActive;
    private bool _isTransitioning;
    private int _currentIndex;

    private PlayerInput _playerInput;
    private string _previousMap;
    private GameObject _selectedButton;

    // ─────────────────────────────────────────
    // Unityライフサイクル
    // ─────────────────────────────────────────

    private void Start()
    {
        // このTutorialTriggerが担当するパネルを初期化
        if (panels != null)
        {
            foreach (GameObject panel in panels)
            {
                if (panel == null) continue;

                GetCanvasGroup(panel).alpha = 0f;
                panel.SetActive(false);
            }
        }

        // このTutorialTriggerが担当するスキップボタンを登録
        if (skipButtons != null)
        {
            foreach (Button skipButton in skipButtons)
            {
                if (skipButton == null) continue;

                skipButton.onClick.RemoveListener(SkipTutorial);
                skipButton.onClick.AddListener(SkipTutorial);
            }
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

        // スキップボタンのイベントを解除
        if (skipButtons != null)
        {
            foreach (Button skipButton in skipButtons)
            {
                if (skipButton == null) continue;

                skipButton.onClick.RemoveListener(SkipTutorial);
            }
        }
    }

    private void Update()
    {
        if (!_isTutorialActive || _selectedButton == null) return;
        if (EventSystem.current == null) return;
        if (EventSystem.current.currentSelectedGameObject != null) return;

        EventSystem.current.SetSelectedGameObject(_selectedButton);
    }

    // ─────────────────────────────────────────
    // トリガー
    // ─────────────────────────────────────────

    private void OnTriggerEnter(Collider other)
    {
        // すでにチュートリアル全体がスキップされていたら、
        // このTutorialTriggerでは何もしない。
        if (_tutorialSkipped) return;

        if (_isShown) return;
        if (!other.CompareTag("Player")) return;
        if (panels == null || panels.Length == 0) return;

        _isShown = true;
        _isTutorialActive = true;
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
        // スキップされた後なら絶対に表示しない
        if (!_isTutorialActive || _tutorialSkipped) return;

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
                 // フェード中にスキップされていたら何もしない
                 if (!_isTutorialActive || _tutorialSkipped)
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
        if (!_isTutorialActive || _isTransitioning) return;
        if (_tutorialSkipped) return;

        if (audioSource != null && enterSound != null)
            audioSource.PlayOneShot(enterSound);

        Next();
    }

    private void Next()
    {
        if (!_isTutorialActive) return;
        if (_tutorialSkipped) return;

        if (_currentIndex < 0 || _currentIndex >= panels.Length)
        {
            Finish();
            return;
        }

        GameObject current = panels[_currentIndex];

        _currentIndex++;

        bool hasNext = _currentIndex < panels.Length;

        FadeOut(current, () =>
        {
            // スキップされた場合は次のパネルを表示しない
            if (!_isTutorialActive || _tutorialSkipped)
                return;

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

        if (!_isTutorialActive || _tutorialSkipped)
            return;

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

                 // スキップされた場合は次を表示しない
                 if (!_isTutorialActive || _tutorialSkipped)
                     return;

                 onComplete?.Invoke();
             });
    }

    // ─────────────────────────────────────────
    // スキップ
    // ─────────────────────────────────────────

    /// <summary>
    /// どのTutorialTriggerのスキップボタンを押しても、
    /// チュートリアル全体をスキップする。
    /// </summary>
    private void SkipTutorial()
    {
        if (!_isTutorialActive) return;

        Debug.Log("=== チュートリアル全体をスキップ ===");

        // ★重要
        // staticなので、他のTutorialTriggerとも共有される。
        _tutorialSkipped = true;

        _isTutorialActive = false;
        _isTransitioning = false;
        _currentIndex = panels != null ? panels.Length : 0;

        // このTutorialTriggerが担当している
        // すべてのパネルを非表示にする。
        if (panels != null)
        {
            foreach (GameObject panel in panels)
            {
                if (panel == null) continue;

                CanvasGroup group = panel.GetComponent<CanvasGroup>();

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

        if (EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(null);

        RestoreControls();

        Time.timeScale = 1f;
    }

    // ─────────────────────────────────────────
    // 通常終了
    // ─────────────────────────────────────────

    private void Finish()
    {
        _isTutorialActive = false;
        _isTransitioning = false;
        _selectedButton = null;

        if (panels != null)
        {
            foreach (GameObject panel in panels)
            {
                if (panel == null) continue;

                CanvasGroup group = panel.GetComponent<CanvasGroup>();

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
            EventSystem.current.SetSelectedGameObject(null);

        RestoreControls();

        Time.timeScale = 1f;
    }

    // ─────────────────────────────────────────
    // 選択
    // ─────────────────────────────────────────

    private void SelectPanelButton(GameObject panel)
    {
        Button button = panel.GetComponentInChildren<Button>(true);

        if (button == null)
        {
            button = panel.AddComponent<Button>();
            button.transition = Selectable.Transition.None;
        }

        button.onClick.RemoveListener(OnPanelSubmit);
        button.onClick.AddListener(OnPanelSubmit);

        _selectedButton = button.gameObject;

        if (EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(null);
            EventSystem.current.SetSelectedGameObject(_selectedButton);
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
                _playerInput = player.GetComponent<PlayerInput>();
        }

        if (_playerInput == null) return;

        _previousMap = _playerInput.currentActionMap != null
            ? _playerInput.currentActionMap.name
            : PlayerMapName;

        _playerInput.SwitchCurrentActionMap(UIMapName);
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

    private static CanvasGroup GetCanvasGroup(GameObject panel)
    {
        return panel.GetComponent<CanvasGroup>()
            ?? panel.AddComponent<CanvasGroup>();
    }
}

