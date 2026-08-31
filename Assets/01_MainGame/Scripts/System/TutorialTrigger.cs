// ============================================================
// TutorialTrigger.cs
// プレイヤーが範囲に入ったらチュートリアルのパネルを順番に表示する。
//
// 表示中:
//   ・Time.timeScale = 0 でゲームを止める
//   ・操作を UI アクションマップに切り替える（ゲーム側の入力を止める）
//   ・パネルのボタンを自動選択する
//     → パッドの決定ボタン（Submit）だけで読み進められる
//
// パネルの出入りは DOTween でフェードする。
// timeScale = 0 で止めているので、Tween は必ず SetUpdate(true)。
//
// パネルに Button が無い場合はパネル自体をボタン化する
// （見た目を変えないよう Transition は None）。
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
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip enterSound;

    [Header("=== フェード ===")]
    [SerializeField] private float fadeInDuration  = 0.25f;
    [SerializeField] private float fadeOutDuration = 0.18f;

    // ─────────────────────────────────────────
    // アクションマップ名（.inputactions と合わせる）
    // ─────────────────────────────────────────

    private const string UIMapName     = "UI";
    private const string PlayerMapName = "Player";

    // ─────────────────────────────────────────
    // 内部
    // ─────────────────────────────────────────

    private bool _isShown;            // 一度出したら二度目は出さない
    private bool _isTutorialActive;
    private bool _isTransitioning;    // フェード中は決定を受け付けない
    private int  _currentIndex;

    private PlayerInput _playerInput;
    private string _previousMap;
    private GameObject _selectedButton;

    // ─────────────────────────────────────────
    // Unity ライフサイクル
    // ─────────────────────────────────────────

    private void Start()
    {
        if (panels == null) return;

        foreach (GameObject panel in panels)
        {
            if (panel == null) continue;

            GetCanvasGroup(panel).alpha = 0f;
            panel.SetActive(false);
        }
    }

    private void OnDestroy()
    {
        if (panels == null) return;

        foreach (GameObject panel in panels)
        {
            if (panel == null) continue;

            CanvasGroup group = panel.GetComponent<CanvasGroup>();
            if (group != null) DOTween.Kill(group);
        }
    }

    /// <summary>
    /// 背景をクリックすると選択が外れ、パッドの決定が効かなくなる。
    /// チュートリアル表示中だけ見張って選択を戻す。
    /// </summary>
    private void Update()
    {
        if (!_isTutorialActive || _selectedButton == null) return;
        if (EventSystem.current == null) return;
        if (EventSystem.current.currentSelectedGameObject != null) return;

        EventSystem.current.SetSelectedGameObject(_selectedButton);
    }

    private void OnTriggerEnter(Collider other)
    {
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
    // パネルの表示・送り
    // ─────────────────────────────────────────

    private void ShowPanel(int index)
    {
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
        group.interactable   = true;
        group.blocksRaycasts = true;

        // 開いた瞬間の入力で飛ばされないよう、フェード中はロックする
        _isTransitioning = true;

        DOTween.Kill(group);
        group.DOFade(1f, fadeInDuration)
             .SetEase(Ease.OutQuad)
             .SetUpdate(true)
             .OnComplete(() => _isTransitioning = false);

        SelectPanelButton(panel);
    }

    /// <summary>
    /// 決定が押されたとき。ボタンの onClick から呼ばれる。
    /// 次のパネルへ、最後なら閉じる。
    /// </summary>
    private void OnPanelSubmit()
    {
        if (!_isTutorialActive || _isTransitioning) return;

        if (audioSource != null && enterSound != null)
            audioSource.PlayOneShot(enterSound);

        Next();
    }

    private void Next()
    {
        GameObject current = panels[_currentIndex];
        _currentIndex++;

        bool hasNext = _currentIndex < panels.Length;

        FadeOut(current, () =>
        {
            if (hasNext) ShowPanel(_currentIndex);
            else         Finish();
        });
    }

    private void FadeOut(GameObject panel, System.Action onComplete)
    {
        if (panel == null)
        {
            onComplete?.Invoke();
            return;
        }

        _isTransitioning = true;

        CanvasGroup group = GetCanvasGroup(panel);
        group.interactable   = false;
        group.blocksRaycasts = false;

        DOTween.Kill(group);
        group.DOFade(0f, fadeOutDuration)
             .SetEase(Ease.InQuad)
             .SetUpdate(true)
             .OnComplete(() =>
             {
                 panel.SetActive(false);
                 onComplete?.Invoke();
             });
    }

    private void Finish()
    {
        _isTutorialActive = false;
        _isTransitioning  = false;
        _selectedButton   = null;

        if (EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(null);

        RestoreControls();
        Time.timeScale = 1f;
    }

    // ─────────────────────────────────────────
    // 選択（パッド操作）
    // ─────────────────────────────────────────

    /// <summary>
    /// パネル内のボタンを選択させる。
    /// これが無いと、開いた直後は決定ボタンを押しても何も起きない。
    /// </summary>
    private void SelectPanelButton(GameObject panel)
    {
        Button button = panel.GetComponentInChildren<Button>(true);

        if (button == null)
        {
            // ボタンが無いパネルなので、パネル自体をボタンにする。
            // 見た目を変えたくないので Transition は None。
            button = panel.AddComponent<Button>();
            button.transition = Selectable.Transition.None;
        }

        // 同じパネルを開き直しても二重登録にならないようにする
        button.onClick.RemoveListener(OnPanelSubmit);
        button.onClick.AddListener(OnPanelSubmit);

        _selectedButton = button.gameObject;

        if (EventSystem.current != null)
        {
            // 一度 null を挟まないと、同じ相手を選び直したとき OnSelect が飛ばない
            EventSystem.current.SetSelectedGameObject(null);
            EventSystem.current.SetSelectedGameObject(_selectedButton);
        }
    }

    // ─────────────────────────────────────────
    // 操作の切り替え
    // ─────────────────────────────────────────

    private void SwitchToUIControls()
    {
        if (_playerInput == null)
        {
            PlayerStateMachine player = FindAnyObjectByType<PlayerStateMachine>();
            if (player != null) _playerInput = player.GetComponent<PlayerInput>();
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
            string.IsNullOrEmpty(_previousMap) ? PlayerMapName : _previousMap);

        // SwitchCurrentActionMap は切り替え前のマップ（UI）を無効化する。
        // EventSystem の InputSystemUIInputModule は同じアセットの UI マップを
        // 参照しているので、戻しておかないとポーズ UI などの操作が効かなくなる。
        _playerInput.actions?.FindActionMap(UIMapName)?.Enable();
    }

    // ─────────────────────────────────────────
    // 内部ヘルパー
    // ─────────────────────────────────────────

    /// <summary>フェード用の CanvasGroup。無ければ足す。</summary>
    private static CanvasGroup GetCanvasGroup(GameObject panel)
    {
        return panel.GetComponent<CanvasGroup>() ?? panel.AddComponent<CanvasGroup>();
    }
}
