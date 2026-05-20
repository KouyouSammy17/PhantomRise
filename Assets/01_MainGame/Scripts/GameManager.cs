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

    [Header("=== リスタート ===")]
    [SerializeField] private string gameSceneName = "";  // 空なら現在のシーンをリロード

    // ─────────────────────────────────────────
    // UnityEvents（UIManager などへ通知）
    // ─────────────────────────────────────────

    [Header("=== イベント ===")]
    public UnityEvent OnGameClear;
    public UnityEvent OnGameOver;

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
}