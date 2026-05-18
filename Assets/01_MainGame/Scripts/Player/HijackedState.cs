// ============================================================
// HijackedState.cs
// 乗っ取り状態：移動・攻撃・スキル入力受付
// HP 0 で GhostState へ戻る（PlayerHP から呼ぶ）
// ============================================================

using UnityEngine;
using UnityEngine.Events;

public class HijackedState : PlayerBaseState
{
    // 攻撃・スキル入力は PlayerStateMachine の UnityEvent 経由で
    // PlayerCombat など別コンポーネントへ通知する（後で実装）

    public HijackedState(PlayerStateMachine machine) : base(machine) { }

    public override void Enter()
    {
        Machine.VelocityY = 0f;
        Debug.Log("[Hijacked] Enter");
    }

    public override void Update(float deltaTime)
    {
        Machine.ApplyMovement(deltaTime);
    }

    public override void Exit()
    {
        Debug.Log("[Hijacked] Exit");
    }
}