// ============================================================
// DodgeState.cs
// 回避状態：一定時間だけダッシュし、元の状態へ戻る
// Ghost / Hijacked どちらからでも呼び出せる
// ============================================================

using UnityEngine;

public class DodgeState : PlayerBaseState
{
    private PlayerBaseState _callerState;   // 回避後に戻る状態
    private Vector3 _dodgeDir;
    private float _elapsed;
    private float _cooldownTimer;

    public bool CanDodge => _cooldownTimer <= 0f;

    /// <summary>ダッジ後に HijackedState へ戻る場合 true（UI 表示維持用）</summary>
    public bool IsReturningToHijacked => _callerState is HijackedState;

    public DodgeState(PlayerStateMachine machine) : base(machine) { }

    /// <summary>TransitionTo(Dodge) の前に必ず呼ぶ</summary>
    public void SetCaller(PlayerBaseState caller)
    {
        _callerState = caller;
    }

    public override void Enter()
    {
        _elapsed = 0f;
        _dodgeDir = Machine.GetMoveDirection();

        // 入力なし → キャラの後方へ
        if (_dodgeDir.sqrMagnitude < 0.01f)
            _dodgeDir = -Machine.transform.forward;

        Machine.VelocityY = 0f;

        // 幽霊状態からの回避なら走りアニメーション（乗っ取り中はモデル非表示なので無害）
        Machine.GhostAnim?.SetMove(true);

        Debug.Log($"[Dodge] Enter — 方向 {_dodgeDir}");
    }

    /// <summary>
    /// クールダウンを進める。
    ///
    /// Update() は Dodge 状態のあいだしか呼ばれないが、
    /// クールダウンは回避が終わった後に進める必要があるので、
    /// PlayerStateMachine.Update から毎フレーム呼ぶ。
    /// </summary>
    public void TickCooldown(float deltaTime)
    {
        if (_cooldownTimer <= 0f) return;

        _cooldownTimer = Mathf.Max(0f, _cooldownTimer - deltaTime);
    }

    public override void Update(float deltaTime)
    {
        // ダッシュ移動（重力なし）
        Machine.CC.Move(_dodgeDir * Machine.DodgeSpeed * deltaTime);

        _elapsed += deltaTime;
        if (_elapsed >= Machine.DodgeDuration)
        {
            _cooldownTimer = Machine.DodgeCooldown;

            // Ghost から来た場合はタイマーを引き継ぐ
            if (_callerState == Machine.Ghost)
                Machine.Ghost.Resume();

            Machine.TransitionTo(_callerState ?? Machine.Ghost);
        }
    }

    public override void Exit()
    {
        Debug.Log("[Dodge] Exit");
    }
}