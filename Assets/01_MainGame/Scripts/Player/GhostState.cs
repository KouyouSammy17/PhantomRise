// ============================================================
// GhostState.cs
// 幽霊状態：移動可能・60秒でDead遷移
// ============================================================

using UnityEngine;

public class GhostState : PlayerBaseState
{
    private float _timer;

    // Dodge から戻ってきたとき true にする
    // true のとき Enter() でタイマーをリセットしない
    private bool _resuming;

    public GhostState(PlayerStateMachine machine) : base(machine) { }

    /// <summary>
    /// Dodge など「一時的に離脱する状態」から戻るときに呼ぶ。
    /// タイマーを引き継ぐために Enter() のリセットをスキップする。
    /// DodgeState の Exit より前に呼ぶこと。
    /// </summary>
    public void Resume() => _resuming = true;

    public override void Enter()
    {
        Machine.VelocityY = 0f;

        // アクション（Hijack/Die）のロックを解除して Idle に戻す
        Machine.GhostAnim?.ResetToIdle();

        if (_resuming)
        {
            // Dodge から戻ってきた → タイマーをそのまま引き継ぐ
            _resuming = false;
            Debug.Log($"[Ghost] Resume — タイマー継続 {_timer:F1}s 経過");
        }
        else
        {
            // Ghost に初めて入る（ゲーム開始 or 乗っ取り解除）→ リセット
            _timer = 0f;
            Debug.Log("[Ghost] Enter — タイマー開始");
        }
    }

    public override void Update(float deltaTime)
    {
        // 移動
        Machine.ApplyMovement(deltaTime);

        // タイマー加算
        _timer += deltaTime;
        float remaining = Mathf.Max(0f, Machine.GhostTimeLimit - _timer);
        Machine.OnGhostTimerUpdate?.Invoke(remaining);

        // タイムアウト → 死亡
        if (_timer >= Machine.GhostTimeLimit)
        {
            Machine.TransitionTo(Machine.Dead);
        }
    }

    public override void Exit()
    {
        Debug.Log($"[Ghost] Exit — 経過 {_timer:F1}s");
    }

    /// <summary>
    /// 敵の攻撃が当たったとき PlayerStateMachine から呼ぶ。
    /// Ghost 状態中のみ有効。即 Dead に遷移する。
    /// </summary>
    public void OnHit()
    {
        Machine.TransitionTo(Machine.Dead); // 即死
    }

    /// <summary>残り時間（外部参照用）</summary>
    public float TimeRemaining => Mathf.Max(0f, Machine.GhostTimeLimit - _timer);

    /// <summary>
    /// タイマーを延長する（アイテム取得時などに呼ぶ）。
    /// _timer を減算することで残り時間を増やす。
    /// </summary>
    public void AddTime(float seconds)
    {
        _timer = Mathf.Max(0f, _timer - seconds);
        Debug.Log($"[Ghost] タイマー延長 +{seconds}s → 残り {TimeRemaining:F1}s");
    }
}