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

    private void Start()
    {
        // 初期値
        bgmSlider.value = 1f;
        seSlider.value = 1f;

        // スライダーが動いたときに呼ぶ
        bgmSlider.onValueChanged.AddListener(SetBGMVolume);
        seSlider.onValueChanged.AddListener(SetSEVolume);

        // 初期音量を設定
        SetBGMVolume(bgmSlider.value);
        SetSEVolume(seSlider.value);
    }

    public void SetBGMVolume(float value)
    {
        audioMixer.SetFloat("BGMVolume", LinearToDecibel(value));
    }

    public void SetSEVolume(float value)
    {
        audioMixer.SetFloat("SEVolume", LinearToDecibel(value));
    }

    private float LinearToDecibel(float value)
    {
        if (value <= 0.0001f)
            return -80f;

        return Mathf.Log10(value) * 20f;
    }
}