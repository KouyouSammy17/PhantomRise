// ============================================================
// DeadState.cs
// 死亡状態：入力を受け付けない
// ディゾルブアニメーション（ghost_dissolve）の完了を待ってから
// OnPlayerDead を発火し、GameManager がゲームオーバー UI を表示する
// ============================================================

using System;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class DeadState : PlayerBaseState
{
    public DeadState(PlayerStateMachine machine) : base(machine) { }

    public override void Enter()
    {
        DieAsync().Forget();
    }

    private async UniTaskVoid DieAsync()
    {
        // ディゾルブアニメーション再生
        Machine.GhostAnim?.PlayDie();

        // 幽霊モデルが表示されているときだけアニメーション完了を待つ
        // （非表示＝乗っ取り中などの死亡は即ゲームオーバー）
        bool visible = Machine.PlayerVisual != null
                       && Machine.PlayerVisual.activeInHierarchy;

        float wait = (visible && Machine.GhostAnim != null)
            ? Machine.GhostAnim.DieAnimTime
            : 0f;

        // シェーダーディゾルブ（_Dissolve 1→0）をアニメーションと同時に再生
        if (visible && wait > 0f)
            Machine.DissolveFx?.PlayDissolve(wait);

        if (wait > 0f)
        {
            await UniTask.Delay(
                TimeSpan.FromSeconds(wait),
                cancellationToken: Machine.GetCancellationTokenOnDestroy());
        }

        // ディゾルブ完了 → モデルを非表示
        if (Machine.PlayerVisual != null)
            Machine.PlayerVisual.SetActive(false);

        // ゲームオーバー UI
        // ボスに倒された場合は、チュートリアルだけ負けイベント扱いになる
        Machine.OnPlayerDead?.Invoke();

        if (Machine.ConsumeKilledByBoss()) GameManager.Instance.TriggerGameOverByBoss();
        else                               GameManager.Instance.TriggerGameOver();
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
