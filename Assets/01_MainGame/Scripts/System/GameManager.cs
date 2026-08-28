// ============================================================
// GameManager.cs
// ゲームの状態管理（Playing / GameClear / GameOver）とシーン遷移
//
// 各シーンに1つずつ配置する。DontDestroyOnLoad にはしない。
// シーン内のオブジェクト（StageBGM / BossRoomTrigger）を参照するので、
// シーンと一緒に破棄されるのが正しい。
//
// シーン名は Scenes.cs の定数に集約する。
// シーンごとに設定するのは nextScene と bossRoomTrigger だけ。
//
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
    // Singleton（シーンスコープ）
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

        // ゲームオーバーやチュートリアルで 0 にしたまま
        // シーンが切り替わっても止まらないよう、必ず戻す
        Time.timeScale = 1f;
    }

    private void OnDestroy()
    {
        // 破棄済みオブジェクトを Instance が指し続けないようにする
        if (Instance == this) Instance = null;
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
    [Tooltip("クリア後に進むシーン。空ならタイトルへ戻る（＝最終ステージ）")]
    [SerializeField] private string nextScene = "";

    /// <summary>クリア画面に「次へ」ボタンを出すかどうか。</summary>
    public bool HasNextScene => !string.IsNullOrEmpty(nextScene);

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

    [Header("=== BGM（シーンにある物だけ割り当てる。空でもよい）===")]
    [SerializeField] private BossRoomTrigger bossRoomTrigger;
    [SerializeField] private StageBGM stageBGM;

    // ─────────────────────────────────────────
    // 内部
    // ─────────────────────────────────────────

    /// <summary>
    /// BGM を止める。チュートリアルやタイトルにはボスがいないので、
    /// 未割り当てでも落ちないように必ず null チェックする。
    /// </summary>
    private void StopAllBgm()
    {
        if (bossRoomTrigger != null) bossRoomTrigger.StopBossBGM();
        if (stageBGM != null)        stageBGM.StopStageBGM();
    }

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
        StopAllBgm();
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
        StopAllBgm();
        audioSource.PlayOneShot(gameOverSound); // ゲームオーバー音を再生

        OnGameOver?.Invoke();
    }

    // ─────────────────────────────────────────
    // リスタート
    // ─────────────────────────────────────────

    /// <summary>
    /// 今いるシーンをそのまま読み直す。
    /// シーン名を持たないので、どのシーンでも設定なしで動く。
    /// </summary>
    public void Restart()
    {
        StopAllBgm();
        LoadSceneWithUISound(SceneManager.GetActiveScene().buildIndex);
    }

    // ─────────────────────────────────────────
    // シーン遷移
    // ─────────────────────────────────────────

    /// <summary>
    /// クリア画面の「次へ」ボタンから呼ぶ。
    /// nextScene が空（最終ステージ）ならタイトルへ戻る。
    /// </summary>
    public void GoToNextStage()
    {
        StopAllBgm();
        LoadSceneWithUISound(HasNextScene ? nextScene : Scenes.Title);
    }

    /// <summary>
    /// タイトル画面からゲーム本編へ遷移する。
    /// TitleManager（TitlePanel.prefab）から呼ぶ。
    /// </summary>
    public void LoadGameScene()
    {
        GoToTutorial();
    }

    /// <summary>
    /// タイトル画面へ戻る。
    /// </summary>
    public void GoToTitle()
    {
        StopAllBgm();
        LoadSceneWithUISound(Scenes.Title);
    }

    /// <summary>
    /// チュートリアルシーンへ遷移する。
    /// </summary>
    public void GoToTutorial()
    {
        StopAllBgm();
        LoadSceneWithUISound(Scenes.Tutorial);
    }

    /// <summary>
    /// ステージ2へ遷移する。
    /// </summary>
    public void GoToStage2()
    {
        StopAllBgm();
        LoadSceneWithUISound(Scenes.Stage2);
    }

    // ─────────────────────────────────────────
    // 読み込み（UI音を鳴らしてから遷移する）
    // ─────────────────────────────────────────

    private void LoadSceneWithUISound(string sceneName)
    {
        StartCoroutine(LoadSceneRoutine(() => SceneManager.LoadScene(sceneName)));
    }

    private void LoadSceneWithUISound(int buildIndex)
    {
        StartCoroutine(LoadSceneRoutine(() => SceneManager.LoadScene(buildIndex)));
    }

    private System.Collections.IEnumerator LoadSceneRoutine(System.Action load)
    {
        audioSource.PlayOneShot(UISound);

        // timeScale が 0 でも待てるよう Realtime を使う
        yield return new WaitForSecondsRealtime(0.2f);

        Time.timeScale = 1f;
        load();
    }
}
