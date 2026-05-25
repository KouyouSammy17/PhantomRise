// ============================================================
// UIManager.cs
// ゲームクリア / ゲームオーバー UI の表示を管理する
//
// GameManager の OnGameClear / OnGameOver に
// Inspector からバインドする。
// ============================================================

using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UIManager : MonoBehaviour
{
    // ─────────────────────────────────────────
    // Inspector
    // ─────────────────────────────────────────

    [Header("=== ゲームクリア UI ===")]
    [SerializeField] private GameObject gameClearPanel;
    [SerializeField] private TextMeshProUGUI gameClearText;
    [SerializeField] private Button gameClearRestartButton;   // リスタートボタン

    [Header("=== ゲームオーバー UI ===")]
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private TextMeshProUGUI gameOverText;
    [SerializeField] private Button gameOverRestartButton;    // リスタートボタン

    [Header("=== 幽霊タイマー UI ===")]
    [SerializeField] private TextMeshProUGUI ghostTimerText;

    // ─────────────────────────────────────────
    // Unity ライフサイクル
    // ─────────────────────────────────────────

    private void Start()
    {
        gameClearPanel?.SetActive(false);
        gameOverPanel?.SetActive(false);

        // ボタンに GameManager.Restart を登録
        // Inspector でバインドしても OK だが、コードで登録した方が確実
        gameClearRestartButton?.onClick.AddListener(OnRestartClicked);
        gameOverRestartButton?.onClick.AddListener(OnRestartClicked);
    }

    // ─────────────────────────────────────────
    // GameManager のイベントにバインドするメソッド
    // ─────────────────────────────────────────

    /// <summary>GameManager.OnGameClear にバインドする</summary>
    public void ShowGameClear()
    {
        gameClearPanel?.SetActive(true);

        if (gameClearText != null)
            gameClearText.text = "STAGE CLEAR!";
    }

    /// <summary>GameManager.OnGameOver にバインドする</summary>
    public void ShowGameOver()
    {
        gameOverPanel?.SetActive(true);

        if (gameOverText != null)
            gameOverText.text = "GAME OVER";
    }

    // ─────────────────────────────────────────
    // リスタートボタン
    // ─────────────────────────────────────────

    /// <summary>
    /// リスタートボタンの onClick にコードで登録済み。
    /// Inspector から別のボタンにバインドしても使える。
    /// </summary>
    public void OnRestartClicked()
    {
        GameManager.Instance?.Restart();
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