// ============================================================
// AudioVolumeSettings.cs
// BGM / SE の音量を PlayerPrefs に保存・復元する。
//
// AudioMixer の値はゲームを再起動すると既定値に戻るので、
// 保存した値を起動時に一度流し込む必要がある。
//
// 設定パネル（Popup_Setting）は非表示で始まるため、
// AudioSettingsUI の Start / OnEnable はパネルを開くまで走らない。
// そのため復元は「常に有効なオブジェクト」から Apply() を呼ぶ。
// （UISoundPlayer が UI Manager 上で担当している）
//
// クラス名は UnityEngine.AudioSettings と衝突しないようにしてある。
// ============================================================

using UnityEngine;
using UnityEngine.Audio;

public static class AudioVolumeSettings
{
    // AudioMixer で公開しているパラメーター名
    public const string BgmParam = "BGMVolume";
    public const string SeParam  = "SEVolume";

    private const string BgmKey = "Audio.BGMVolume";
    private const string SeKey  = "Audio.SEVolume";

    private const float DefaultVolume = 1f;

    /// <summary>保存されている BGM 音量（0〜1）</summary>
    public static float Bgm => PlayerPrefs.GetFloat(BgmKey, DefaultVolume);

    /// <summary>保存されている SE 音量（0〜1）</summary>
    public static float Se => PlayerPrefs.GetFloat(SeKey, DefaultVolume);

    // ─────────────────────────────────────────
    // 保存
    // スライダーを動かすたびにディスクへ書くと重いので、
    // ここでは値を入れるだけ。Flush() で書き出す。
    // ─────────────────────────────────────────

    public static void SetBgm(float value) => PlayerPrefs.SetFloat(BgmKey, Mathf.Clamp01(value));
    public static void SetSe(float value)  => PlayerPrefs.SetFloat(SeKey,  Mathf.Clamp01(value));

    /// <summary>PlayerPrefs をディスクに書き出す。パネルを閉じるときなどに呼ぶ。</summary>
    public static void Flush() => PlayerPrefs.Save();

    // ─────────────────────────────────────────
    // 適用
    // ─────────────────────────────────────────

    /// <summary>保存されている音量を AudioMixer に流し込む。</summary>
    public static void Apply(AudioMixer mixer)
    {
        if (mixer == null) return;

        mixer.SetFloat(BgmParam, LinearToDecibel(Bgm));
        mixer.SetFloat(SeParam,  LinearToDecibel(Se));
    }

    /// <summary>
    /// 0〜1 の値をデシベルに変換する。
    /// AudioMixer の音量は dB なので、そのまま入れると聞こえ方が合わない。
    /// </summary>
    public static float LinearToDecibel(float value)
    {
        if (value <= 0.0001f) return -80f;   // 実質ミュート

        return Mathf.Log10(value) * 20f;
    }
}
