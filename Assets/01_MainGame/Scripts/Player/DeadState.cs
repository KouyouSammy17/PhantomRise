// ============================================================
// DeadState.cs
// 死亡状態：入力を受け付けず OnPlayerDead を発火する
// GameManager がこのイベントを受けてゲームオーバー処理をする
// ============================================================

using UnityEngine;

public class DeadState : PlayerBaseState
{
    public DeadState(PlayerStateMachine machine) : base(machine) { }

    public override void Enter()
    {
        Debug.Log("[Dead] Enter — ゲームオーバー");
        Machine.OnPlayerDead?.Invoke();
    }

    public override void Update(float deltaTime)
    {
        // 何もしない（入力無効・移動停止）
    }

    public override void Exit()
    {
        // リスタート時に呼ばれる（今後実装）
    }
}