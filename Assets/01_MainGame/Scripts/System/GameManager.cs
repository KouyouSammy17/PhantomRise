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
    [SerializeField] private string titleSceneName = "TitleScene";
    [SerializeField] private string tutorialSceneName = "Tutorials";
    [SerializeField] private string stage2SceneName = "Stage2";

    // ─────────────────────────────────────────
    // UnityEvents（UIManager などへ通知）
    // ─────────────────────────────────────────

    [Header("=== イベント ===")]
    public UnityEvent OnGameClear;
    public UnityEvent OnGameOver;

    //ゲームクリアとゲームオーバーの音を鳴らす
    [SerializeField] private AudioClip gameClearSound;
    [SerializeField] private AudioClip gameOverSound;

    [SerializeField] private AudioClip UISound;
   [SerializeField] private AudioSource audioSource;

    [SerializeField] private BossRoomTrigger bossRoomTrigger;
    [SerializeField] private StageBGM stageBGM;

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
        bossRoomTrigger.StopBossBGM();
        stageBGM.StopStageBGM();
        audioSource.PlayOneShot(gameClearSound); // ゲームクリア音を再生

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
        bossRoomTrigger.StopBossBGM();
        stageBGM.StopStageBGM();
        audioSource.PlayOneShot(gameOverSound); // ゲームオーバー音を再生
        OnGameOver?.Invoke();
    }

    // ─────────────────────────────────────────
    // リスタート
    // ─────────────────────────────────────────

    public void Restart()
    {
        stageBGM.StopStageBGM();
        //audioSource.PlayOneShot(UISound);
        //Time.timeScale = 1f;
        string scene = string.IsNullOrEmpty(gameSceneName)
            ? SceneManager.GetActiveScene().name
            : gameSceneName;
        //SceneManager.LoadScene(scene);

        LoadSceneWithUISound(scene);
    }


    /// <summary>
    /// タイトル画面からゲームシーンへ遷移する。
    /// TitleManager などから呼ぶ。
    /// </summary>
    public void LoadGameScene()
    {
        stageBGM.StopStageBGM();
        //audioSource.PlayOneShot(UISound);
        //Time.timeScale = 1f;
        string scene = string.IsNullOrEmpty(gameSceneName)
            ? SceneManager.GetActiveScene().name
            : gameSceneName;
        // SceneManager.LoadScene(scene);
        LoadSceneWithUISound(scene);
    }

    /// <summary>
    /// タイトル画面へ戻る。
    /// </summary>
    public void GoToTitle()
    {
        stageBGM.StopStageBGM();
        //audioSource.PlayOneShot(UISound);
        //Time.timeScale = 1f;
        // SceneManager.LoadScene(titleSceneName);
        LoadSceneWithUISound(titleSceneName);    
    }

    /// <summary>
    /// チュートリアルシーンへ遷移する。
    /// </summary>
    public void GoToTutorial()
    {
        stageBGM.StopStageBGM();
        //audioSource.PlayOneShot(UISound);
        //Time.timeScale = 1f;
        //SceneManager.LoadScene(tutorialSceneName);
        LoadSceneWithUISound(tutorialSceneName);
    }

    /// <summary>
    /// ステージ2へ遷移する。
    /// ステージ1クリア後の「次へ」ボタンなどから呼ぶ。
    /// </summary>
    public void GoToStage2()
    {
        stageBGM.StopStageBGM();
        //audioSource.PlayOneShot(UISound);
        //Time.timeScale = 1f;
       // SceneManager.LoadScene(stage2SceneName);
        LoadSceneWithUISound(stage2SceneName);
    }

    private void LoadSceneWithUISound(string sceneName)
    {
        StartCoroutine(LoadSceneWithUISoundCoroutine(sceneName));
    }

    private System.Collections.IEnumerator LoadSceneWithUISoundCoroutine(string sceneName)
    {
        audioSource.PlayOneShot(UISound);

        yield return new WaitForSecondsRealtime(0.2f);

        Time.timeScale = 1f;
        SceneManager.LoadScene(sceneName);
    }


}