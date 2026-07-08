// ============================================================
// GameManager.cs
// ゲームの状態管理（Playing / GameClear / GameOver）
//
// シーンに1つだけ配置する。
// PlayerStateMachine と BossEnemy から通知を受け取り
// ゲームクリア / ゲームオーバーに遷移する。
//
// 他スクリプトからのアクセス: GameManager.Instance
// ============================================================

using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    // ─────────────────────────────────────────
    // Singleton
    // ─────────────────────────────────────────

    public static GameManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    // ─────────────────────────────────────────
    // ゲーム状態
    // ─────────────────────────────────────────

    public enum GameState { Playing, GameClear, GameOver }

    private GameState _state = GameState.Playing;
    public GameState State => _state;
    public bool IsPlaying => _state == GameState.Playing;

    // ─────────────────────────────────────────
    // Inspector
    // ─────────────────────────────────────────

    [Header("=== シーン ===")]
    [SerializeField] private string gameSceneName = "";   // 空なら現在のシーンをリロード
    [SerializeField] private string titleSceneName = "Title";
    [SerializeField] private string nextStageName = "";   // 空なら次ステージなし（ゲームエンディング扱い）

    // ─────────────────────────────────────────
    // UnityEvents（UIManager などへ通知）
    // ─────────────────────────────────────────

    [Header("=== イベント ===")]
    public UnityEvent OnGameClear;
    public UnityEvent OnGameOver;
    /// <summary>次ステージが存在するときだけ発火。UIManager の ShowNextStageButton にバインドする。</summary>
    public UnityEvent OnNextStageAvailable;

    // ─────────────────────────────────────────
    // 内部
    // ─────────────────────────────────────────

    // ─────────────────────────────────────────
    // ゲームクリア
    // ─────────────────────────────────────────

    /// <summary>
    /// ボスを倒したとき BossEnemy から呼ぶ。
    /// または Inspector の UnityEvent 経由でも可。
    /// </summary>
    public void TriggerGameClear()
    {
        if (!IsPlaying) return;

        _state = GameState.GameClear;
        Time.timeScale = 0f;

        Debug.Log("[GameManager] ゲームクリア！");
        OnGameClear?.Invoke();

        if (!string.IsNullOrEmpty(nextStageName))
            OnNextStageAvailable?.Invoke();
    }

    // ─────────────────────────────────────────
    // 次ステージへ
    // ─────────────────────────────────────────

    /// <summary>次ステージが存在するかどうか</summary>
    public bool HasNextStage => !string.IsNullOrEmpty(nextStageName);

    /// <summary>
    /// ゲームクリア後に次ステージへ進む。
    /// UIManager の NextStage ボタンからバインドする。
    /// </summary>
    public void LoadNextStage()
    {
        if (string.IsNullOrEmpty(nextStageName))
        {
            Debug.LogWarning("[GameManager] nextStageName が未設定です。タイトルへ戻ります。");
            GoToTitle();
            return;
        }

        Time.timeScale = 1f;
        SceneManager.LoadScene(nextStageName);
    }

    // ─────────────────────────────────────────
    // ゲームオーバー
    // ─────────────────────────────────────────

    /// <summary>
    /// PlayerStateMachine の OnPlayerDead UnityEvent にバインドする。
    /// </summary>
    public void TriggerGameOver()
    {
        if (!IsPlaying) return;

        _state = GameState.GameOver;
        Time.timeScale = 0f;

        Debug.Log("[GameManager] ゲームオーバー");
        OnGameOver?.Invoke();
    }

    // ─────────────────────────────────────────
    // リスタート
    // ─────────────────────────────────────────

    public void Restart()
    {
        Time.timeScale = 1f;
        string scene = string.IsNullOrEmpty(gameSceneName)
            ? SceneManager.GetActiveScene().name
            : gameSceneName;
        SceneManager.LoadScene(scene);
    }

    /// <summary>
    /// タイトル画面からゲームシーンへ遷移する。
    /// TitleManager などから呼ぶ。
    /// </summary>
    public void LoadGameScene()
    {
        Time.timeScale = 1f;
        string scene = string.IsNullOrEmpty(gameSceneName)
            ? SceneManager.GetActiveScene().name
            : gameSceneName;
        SceneManager.LoadScene(scene);
    }

    /// <summary>
    /// タイトル画面へ戻る。
    /// </summary>
    public void GoToTitle()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(titleSceneName);
    }
}