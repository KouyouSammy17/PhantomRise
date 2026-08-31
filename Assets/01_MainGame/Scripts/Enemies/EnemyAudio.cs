// ============================================================
// EnemyAudio.cs
// 敵の効果音をまとめて再生する。
// EnemyController と同じ GameObject にアタッチする。
//
// クリップは未設定でも落ちない / エラーを出さないようにしてある
// （PlayOneShot に null を渡すと毎回エラーログが出るため）。
// ============================================================

using UnityEngine;

public class EnemyAudio : MonoBehaviour
{
    [SerializeField] private AudioSource e_AudioSource;

    [Header("=== 戦闘 ===")]
    [SerializeField] private AudioClip eAttackSE;
    [SerializeField] private AudioClip eSkillSE;
    [SerializeField] private AudioClip eDeathSE;
    [SerializeField] private AudioClip eHitSE;

    [Header("=== 乗っ取り ===")]
    [SerializeField] private AudioClip eQTEFailureSE;
    [SerializeField] private AudioClip eQTESuccessSE;

    [Tooltip("乗っ取りが成立して体を乗っ取った瞬間")]
    [SerializeField] private AudioClip eTakeOverSE;

    [Tooltip("乗っ取り可能になった瞬間（インジケーターが出るとき）")]
    [SerializeField] private AudioClip eHijackableSE;

    [Header("=== 状態 ===")]
    [Tooltip("プレイヤーを発見したとき")]
    [SerializeField] private AudioClip eAlertSE;

    [Tooltip("スタンしたとき")]
    [SerializeField] private AudioClip eStunSE;

    // ─────────────────────────────────────────
    // 再生
    // ─────────────────────────────────────────

    public void PlayAttackSE()     => Play(eAttackSE);
    public void PlaySkillSE()      => Play(eSkillSE);
    public void PlayDeathSE()      => Play(eDeathSE);
    public void PlayHitSE()        => Play(eHitSE);

    public void PlayQTEFSE()       => Play(eQTEFailureSE);
    public void PlayQTESSE()       => Play(eQTESuccessSE);
    public void PlayTakeOverSE()   => Play(eTakeOverSE);
    public void PlayHijackableSE() => Play(eHijackableSE);

    public void PlayAlertSE()      => Play(eAlertSE);
    public void PlayStunSE()       => Play(eStunSE);

    /// <summary>
    /// AudioSource / クリップが未設定でも黙って何もしない。
    /// PlayOneShot(null) はエラーログを出すので必ずここを通す。
    /// </summary>
    private void Play(AudioClip clip)
    {
        if (e_AudioSource == null || clip == null) return;

        e_AudioSource.PlayOneShot(clip);
    }
}
