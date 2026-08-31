// ============================================================
// UISoundPlayer.cs
// UI の選択音（Select）と決定音（Confirm）をまとめて鳴らす。
//
// UI Manager プレハブにアタッチしてあるので、
// UI Manager が置いてあるシーンならどこでも鳴る。
//
// ボタン 1 つずつにクリップを持たせると設定が大変なので、
// ここに 1 か所だけ置いて ButtonAnimator から静的に呼ぶ。
//
// Time.timeScale = 0（ポーズ・クリア・ゲームオーバー）でも
// AudioSource は影響を受けないのでそのまま鳴る。
// ============================================================

using UnityEngine;
using UnityEngine.Audio;

public class UISoundPlayer : MonoBehaviour
{
    public static UISoundPlayer Instance { get; private set; }

    // ─────────────────────────────────────────
    // Inspector
    // ─────────────────────────────────────────

    [Tooltip("未設定なら同じ GameObject の AudioSource を使う")]
    [SerializeField] private AudioSource audioSource;

    [Header("=== クリップ ===")]
    [Tooltip("ボタンを選択／カーソルを合わせたとき")]
    [SerializeField] private AudioClip selectSE;

    [Tooltip("ボタンを決定したとき")]
    [SerializeField] private AudioClip confirmSE;

    [Range(0f, 1f)]
    [SerializeField] private float volume = 1f;

    [Header("=== 音量設定の復元 ===")]
    // 設定パネル（Popup_Setting）は非表示で始まるので、
    // そこにある AudioSettingsUI は開くまで動かない。
    // 保存した音量を起動時に戻すのは、常に有効なここが担当する。
    [Tooltip("保存された BGM / SE 音量を起動時に流し込む AudioMixer")]
    [SerializeField] private AudioMixer audioMixer;

    // ─────────────────────────────────────────
    // 内部
    // ─────────────────────────────────────────

    // ホバーと選択が同じフレームに来ると二重に鳴るので、
    // 選択音は 1 フレームに 1 回までにする
    private int _lastSelectFrame = -1;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }
        Instance = this;

        if (audioSource == null) audioSource = GetComponent<AudioSource>();

        // 保存されている音量を復元する
        AudioVolumeSettings.Apply(audioMixer);
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    // ─────────────────────────────────────────
    // 静的 API（ButtonAnimator などから呼ぶ）
    // UI Manager が無いシーンでも落ちないよう null 許容
    // ─────────────────────────────────────────

    /// <summary>選択音。同じフレームでは 1 回しか鳴らない。</summary>
    public static void PlaySelect()
    {
        if (Instance == null) return;
        if (Instance._lastSelectFrame == Time.frameCount) return;

        Instance._lastSelectFrame = Time.frameCount;
        Instance.Play(Instance.selectSE);
    }

    /// <summary>決定音。</summary>
    public static void PlayConfirm()
    {
        Instance?.Play(Instance.confirmSE);
    }

    private void Play(AudioClip clip)
    {
        if (audioSource == null || clip == null) return;

        audioSource.PlayOneShot(clip, volume);
    }
}
