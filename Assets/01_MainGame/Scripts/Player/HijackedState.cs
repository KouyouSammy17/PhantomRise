// ============================================================
// HijackedState.cs
// 乗っ取り状態：移動・攻撃・スキル入力受付
//
// Enter  : 幽霊モデルを非表示 → 敵モデルを Player の子に付け替え
// Exit   : 幽霊モデルを復元  → 敵モデルを元の親に戻す
//          （その後 OnHijackedEnemyDied で敵ごと Destroy される）
//
// Dodge 連携:
//   PrepareDodge() を呼んでから TransitionTo(Dodge) すると
//   Exit/Enter でモデルスワップをスキップし、
//   敵ビジュアルを Player の子に付けたまま Dodge を実行できる。
// ============================================================

using UnityEngine;

public class HijackedState : PlayerBaseState
{
    private EnemyController _enemy;

    public EnemyController CurrentEnemy => _enemy;

    // ── モデル スワップ用 ──────────────────────────────
    private Transform _enemyVisual;
    private Transform _enemyVisualOriginalParent;
    private Vector3   _enemyVisualOriginalLocalPos;
    private Quaternion _enemyVisualOriginalLocalRot;

    // ── Dodge 一時離脱フラグ ──────────────────────────
    private bool _leavingForDodge = false;

    public HijackedState(PlayerStateMachine machine) : base(machine) { }

    public void SetEnemy(EnemyController enemy) => _enemy = enemy;

    /// <summary>
    /// Dodge へ一時離脱する直前に PlayerStateMachine から呼ぶ。
    /// Exit() でのモデル復元をスキップし、Enter() での再アタッチも省略する。
    /// </summary>
    public void PrepareDodge() => _leavingForDodge = true;

    // ─────────────────────────────────────────
    // Enter: 幽霊モデル OFF、敵モデルを Player の子に
    // ─────────────────────────────────────────

    public override void Enter()
    {
        Machine.VelocityY = 0f;

        if (_enemyVisual != null)
        {
            // Dodge から戻ってきた — ビジュアルは既に Player の子にある
            // 幽霊モデルが万一 ON になっていれば確実に OFF にするだけ
            if (Machine.PlayerVisual != null)
                Machine.PlayerVisual.SetActive(false);
            Debug.Log($"[Hijacked] Re-Enter from Dodge — {_enemy?.name}");
            return;
        }

        // ① 幽霊キャラを非表示
        if (Machine.PlayerVisual != null)
            Machine.PlayerVisual.SetActive(false);

        // ② 敵のビジュアルルートを取得して Player の Transform の子に移す
        if (_enemy != null)
        {
            _enemyVisual = _enemy.GetVisualRoot();
            _enemyVisualOriginalParent   = _enemyVisual.parent;
            _enemyVisualOriginalLocalPos = _enemyVisual.localPosition;
            _enemyVisualOriginalLocalRot = _enemyVisual.localRotation;

            // Player の子にする（worldPositionStays: false = ローカル座標でゼロ配置）
            _enemyVisual.SetParent(Machine.transform, false);
            _enemyVisual.localPosition = Vector3.zero;
            _enemyVisual.localRotation = Quaternion.identity;
        }

        Debug.Log($"[Hijacked] Enter — {_enemy?.name}");
    }

    // ─────────────────────────────────────────
    // Update: 移動のみ（モデルは親である Player.transform に追従）
    // ─────────────────────────────────────────

    public override void Update(float deltaTime)
    {
        Machine.ApplyMovement(deltaTime);
    }

    // ─────────────────────────────────────────
    // Exit: モデルを元に戻す・幽霊モデル ON
    // ─────────────────────────────────────────

    public override void Exit()
    {
        if (_leavingForDodge)
        {
            // Dodge への一時離脱 — モデルスワップをスキップ
            // 敵ビジュアルは Player の子に付けたまま、幽霊モデルも非表示のまま
            _leavingForDodge = false;
            Debug.Log("[Hijacked] Exit (→Dodge) — ビジュアル保持");
            return;
        }

        // 幽霊モデルを再表示
        if (Machine.PlayerVisual != null)
            Machine.PlayerVisual.SetActive(true);

        // 敵ビジュアルを元の親に戻す
        // HP 0 による遷移の場合は直後に OnHijackedEnemyDied → Destroy されるので
        // ここで親に返しておけば一緒に消える
        if (_enemyVisual != null)
        {
            _enemyVisual.SetParent(_enemyVisualOriginalParent);
            _enemyVisual.localPosition = _enemyVisualOriginalLocalPos;
            _enemyVisual.localRotation = _enemyVisualOriginalLocalRot;
            _enemyVisual = null;
        }

        Debug.Log("[Hijacked] Exit");
    }

    // ─────────────────────────────────────────
    // 攻撃・スキル
    // ─────────────────────────────────────────

    public void TryAttack()
    {
        if (_enemy == null) return;
        _enemy.PerformAttack();
    }

    public void TrySkill()
    {
        if (_enemy == null) return;
        _enemy.PerformSkill();
    }

    // ─────────────────────────────────────────
    // HP 0 → 敵を破棄して Ghost に戻る
    // ─────────────────────────────────────────

    public void OnHPZero()
    {
        Debug.Log("[Hijacked] HP 0 → 敵消滅・幽霊状態へ");
        EnemyController dying = _enemy;
        _enemy = null;
        Machine.TransitionTo(Machine.Ghost);  // Exit() が呼ばれビジュアルが戻る
        dying?.OnHijackedEnemyDied();          // Destroy → ビジュアルも一緒に消える
    }
}
