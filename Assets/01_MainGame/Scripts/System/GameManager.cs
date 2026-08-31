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
using System;
using UnityEngine;
using UnityEngine.Audio;
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

    [Header("=== シーン遷移フェード ===")]
    [Tooltip("暗転しきるまでの秒数。UI 音が鳴っている間に暗くなる")]
    [SerializeField] private float fadeOutDuration = 0.35f;

    [Tooltip("遷移先で明けるまでの秒数。ScreenFader が自動で行う")]
    [SerializeField] private float fadeInDuration = 0.45f;

    [Header("=== BGM（シーンにある物だけ割り当てる。空でもよい）===")]
    [SerializeField] private BossRoomTrigger bossRoomTrigger;
    [SerializeField] private StageBGM stageBGM;


    [SerializeField] private GameObject[] UIs;
    [SerializeField] private GameObject Settingspanel;
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
        if (stageBGM != null) stageBGM.StopStageBGM();
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
        //UIを消す
        foreach (var ui in UIs)
        {
            ui.SetActive(false);
        }
        audioSource.PlayOneShot(gameClearSound); // ゲームクリア音を再生

        OnGameClear?.Invoke();
    }

    // ─────────────────────────────────────────
    // ゲームオーバー
    // ─────────────────────────────────────────

    /// <summary>
    /// PlayerStateMachine の OnPlayerDead UnityEvent にバインドする。
    /// 引数なしのまま残すこと（UnityEvent は引数付きメソッドを void 呼び出しにできない）。
    /// </summary>
    public void TriggerGameOver() => HandlePlayerDeath(false);

    /// <summary>
    /// ボスに倒されたときの死亡。DeadState から呼ぶ。
    /// チュートリアルのボスは勝てない「負けイベント」なので、
    /// この場合だけゲームオーバーにせず Stage2 へ進める。
    /// </summary>
    public void TriggerGameOverByBoss() => HandlePlayerDeath(true);

    private void HandlePlayerDeath(bool killedByBoss)
    {
        if (!IsPlaying) return;

        if (killedByBoss && SceneManager.GetActiveScene().name == Scenes.Tutorial)
        {
            // 二重に遷移しないよう状態だけ進める（ゲームオーバー UI は出さない）
            _state = GameState.GameOver;

            Debug.Log("[GameManager] チュートリアルのボスに敗北 → Stage2 へ");
            GoToStage2();   // 中で BGM 停止・UI 音・シーン遷移まで行う
            return;
        }

        _state = GameState.GameOver;
        Time.timeScale = 0f;

        Debug.Log("[GameManager] ゲームオーバー");
        StopAllBgm();
        foreach (var ui in UIs)
        {
            ui.SetActive(false);
        }

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

    /// <summary>/// 設定画面へ遷移する。/// </summary>
    public void OpenSettings()
    {
        Settingspanel.SetActive(true);
        audioSource.PlayOneShot(UISound);
    }
    public void CloseSettings()
    {
        Settingspanel.SetActive(false);
        audioSource.PlayOneShot(UISound);
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

        // UI 音を鳴らしながら暗転する。
        // 明けるのは ScreenFader 側（この GameManager は
        // シーンと一緒に破棄されるので、遷移先まで面倒を見られない）。
        ScreenFader.Instance.FadeOut(fadeOutDuration, fadeInDuration);

        // timeScale が 0 でも待てるよう Realtime を使う
        yield return new WaitForSecondsRealtime(Mathf.Max(fadeOutDuration, 0.2f));

        Time.timeScale = 1f;
        load();
    }
}
