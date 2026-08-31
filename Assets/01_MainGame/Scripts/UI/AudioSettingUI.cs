// ============================================================
// AudioSettingUI.cs
// 設定パネルの BGM / SE スライダー。
//
// 値の保存・復元・dB 変換は AudioVolumeSettings に任せる。
//
// パネルは非表示で始まり、開かれたときに初めて有効になるので、
// スライダーの同期は Start ではなく OnEnable で行う
// （開くたびに保存値へ合わせ直す）。
// ============================================================

using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

public class AudioSettingsUI : MonoBehaviour
{
    [Header("Audio Mixer")]
    [SerializeField] private AudioMixer audioMixer;

    [Header("Sliders")]
    [SerializeField] private Slider bgmSlider;
    [SerializeField] private Slider seSlider;

    private void Awake()
    {
        // 登録は一度だけ（OnEnable でやると開くたびに増える）
        if (bgmSlider != null) bgmSlider.onValueChanged.AddListener(SetBGMVolume);
        if (seSlider  != null) seSlider.onValueChanged.AddListener(SetSEVolume);
    }

    private void OnEnable()
    {
        // 保存されている値にスライダーを合わせる。
        // SetValueWithoutNotify にしないと onValueChanged が走り、
        // 開いただけで保存し直してしまう。
        if (bgmSlider != null) bgmSlider.SetValueWithoutNotify(AudioVolumeSettings.Bgm);
        if (seSlider  != null) seSlider.SetValueWithoutNotify(AudioVolumeSettings.Se);

        AudioVolumeSettings.Apply(audioMixer);
    }

    private void OnDisable()
    {
        // 閉じるタイミングでまとめてディスクに書き出す
        AudioVolumeSettings.Flush();
    }

    // ─────────────────────────────────────────
    // スライダー
    // ─────────────────────────────────────────

    public void SetBGMVolume(float value)
    {
        AudioVolumeSettings.SetBgm(value);

        if (audioMixer != null)
            audioMixer.SetFloat(AudioVolumeSettings.BgmParam,
                                AudioVolumeSettings.LinearToDecibel(value));
    }

    public void SetSEVolume(float value)
    {
        AudioVolumeSettings.SetSe(value);

        if (audioMixer != null)
            audioMixer.SetFloat(AudioVolumeSettings.SeParam,
                                AudioVolumeSettings.LinearToDecibel(value));
    }
}
