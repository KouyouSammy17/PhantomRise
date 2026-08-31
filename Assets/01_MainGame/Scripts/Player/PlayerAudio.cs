// ============================================================
// PlayerAudio.cs
// プレイヤーの効果音をまとめて再生する。
// PlayerStateMachine と同じ GameObject にアタッチする。
//
// EnemyAudio と同じ作りにしてある。
// クリップ未設定でもエラーを出さない。
// ============================================================

using UnityEngine;

public class PlayerAudio : MonoBehaviour
{
    [SerializeField] private AudioSource p_AudioSource;

    [Tooltip("回避（ダッジ）したとき")]
    [SerializeField] private AudioClip pDodgeSE;

    private void Awake()
    {
        if (p_AudioSource == null) p_AudioSource = GetComponent<AudioSource>();
    }

    public void PlayDodgeSE() => Play(pDodgeSE);

    /// <summary>
    /// AudioSource / クリップが未設定でも黙って何もしない。
    /// PlayOneShot(null) はエラーログを出すので必ずここを通す。
    /// </summary>
    private void Play(AudioClip clip)
    {
        if (p_AudioSource == null || clip == null) return;

        p_AudioSource.PlayOneShot(clip);
    }
}
