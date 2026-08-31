// ============================================================
// GhostAnimation.cs
// 幽霊（プレイヤー）のアニメーション制御
// Animator パラメーター不要 — CrossFade でステートを直接再生する
// PlayerStateMachine と同じ GameObject にアタッチ（未アタッチでも自動追加される）
// ============================================================

using UnityEngine;

public class GhostAnimation : MonoBehaviour
{
    // PlayerAnimator.controller 内のステート名
    private static readonly int HashIdle   = Animator.StringToHash("Idle");
    private static readonly int HashMove   = Animator.StringToHash("Move");
    private static readonly int HashHijack = Animator.StringToHash("Hijack");
    private static readonly int HashAttack = Animator.StringToHash("Attack");
    private static readonly int HashDie    = Animator.StringToHash("Die");

    [SerializeField] private Animator _animator;   // 未設定なら自動検索
    [SerializeField] private float _crossFadeTime = 0.1f;

    [Tooltip("乗っ取り成功時の攻撃アニメーションの再生時間（ghost_attack ≒ 0.83s）")]
    [SerializeField] private float _attackAnimTime = 0.7f;

    /// <summary>攻撃アニメーション完了待ちに使う時間（秒）</summary>
    public float AttackAnimTime => _attackAnimTime;

    [Tooltip("死亡（ディゾルブ）アニメーションの再生時間（ghost_dissolve ≒ 1.33s）")]
    [SerializeField] private float _dieAnimTime = 1.33f;

    /// <summary>ディゾルブ完了待ちに使う時間（秒）</summary>
    public float DieAnimTime => _dieAnimTime;

    private int _currentState;

    // Hijack / Die 再生中はロコモーション（Idle/Move）で上書きしない
    private bool _isPlayingAction;

    private void Awake()
    {
        if (_animator == null)
        {
            // 1) PlayerVisual（幽霊モデルのルート）配下 → 2) 自分の子、の順で探す
            var machine = GetComponent<PlayerStateMachine>();
            if (machine != null && machine.PlayerVisual != null)
                _animator = machine.PlayerVisual.GetComponentInChildren<Animator>(true);

            if (_animator == null)
                _animator = GetComponentInChildren<Animator>(true);

            if (_animator == null)
                Debug.LogWarning("[GhostAnimation] Animator が見つかりません");
        }
    }

    // ─────────────────────────────────────────
    // ロコモーション（GhostState / DodgeState 用）
    // ─────────────────────────────────────────

    /// <summary>移動中なら Move（ghost_run）、停止中なら Idle を再生</summary>
    public void SetMove(bool moving)
    {
        if (_isPlayingAction) return;
        Play(moving ? HashMove : HashIdle);
    }

    // ─────────────────────────────────────────
    // アクション
    // ─────────────────────────────────────────

    /// <summary>乗っ取り QTE 開始時 — 構え（ghost_attack_shift）</summary>
    public void PlayHijack()
    {
        _isPlayingAction = true;
        Play(HashHijack);
    }

    /// <summary>乗っ取り QTE 成功時 — 攻撃（ghost_attack）</summary>
    public void PlayAttack()
    {
        _isPlayingAction = true;
        Play(HashAttack);
    }

    /// <summary>
    /// スタート時に表示する乗っ取り成功時の攻撃アニメーション
    /// </summary>
    public void StartPlayAttackAnimation()
    {
        _isPlayingAction = true;
        Play(HashAttack);

        CancelInvoke(nameof(ResetToIdle));

        Invoke(nameof(ResetToIdle), _attackAnimTime);
    }

    /// <summary>死亡時（ghost_dissolve）</summary>
    public void PlayDie()
    {
        _isPlayingAction = true;
        Play(HashDie);
    }

    /// <summary>
    /// アクション終了 → Idle に戻す。
    /// QTE 失敗で Ghost に戻るときや、乗っ取り解除で幽霊が再表示されるときに呼ぶ。
    /// </summary>
    public void ResetToIdle()
    {
        _isPlayingAction = false;
        Play(HashIdle);
    }

    // ─────────────────────────────────────────
    // 内部処理
    // ─────────────────────────────────────────

    private void Play(int stateHash)
    {
        if (_animator == null || !_animator.isActiveAndEnabled) return;
        if (_currentState == stateHash) return;   // 同一ステートへの再クロスフェード防止

        _animator.CrossFadeInFixedTime(stateHash, _crossFadeTime);
        _currentState = stateHash;
    }
}
