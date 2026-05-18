// ============================================================
// PlayerBaseState.cs
// 全状態の抽象基底クラス
// ============================================================

public abstract class PlayerBaseState
{
    protected PlayerStateMachine Machine { get; private set; }

    public PlayerBaseState(PlayerStateMachine machine)
    {
        Machine = machine;
    }

    public abstract void Enter();
    public abstract void Update(float deltaTime);
    public abstract void Exit();
}