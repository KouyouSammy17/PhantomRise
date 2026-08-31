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
//
// 転送 連携:
//   ReleaseBody() で今のボディを手放してから HijackState へ遷移する。
//   Exit() が通常どおり走って幽霊の姿に戻るので、
//   転送の QTE も Ghost からの乗っ取りと同じ演出になる。
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

    // ── 一時離脱フラグ ────────────────────────────────
    private bool _leavingForDodge = false;

    public HijackedState(PlayerStateMachine machine) : base(machine) { }

    public void SetEnemy(EnemyController enemy) => _enemy = enemy;

    /// <summary>Dodge へ一時離脱する直前に呼ぶ。Exit() のモデル復元をスキップする。</summary>
    public void PrepareDodge() => _leavingForDodge = true;

    /// <summary>
    /// 今のボディへの参照を手放して返す。転送で乗っ取りに入るときに使う。
    ///
    /// 呼び出し側は状態遷移（＝ Exit() でビジュアルを敵に返す）を済ませてから
    /// 返り値の OnHijackedEnemyDied() を呼ぶこと。
    /// 先に破棄すると、Player の子に付いたままのビジュアルごと消える。
    /// </summary>
    public EnemyController ReleaseBody()
    {
        // 乗っ取る敵が変わるので現在のバフ UI を解除
        Machine.StopDemonBuff();
        Machine.StopSpecterBuff();

        EnemyController body = _enemy;
        _enemy = null;
        return body;
    }

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

        // ② 幽霊タイマー UI を非表示
        Machine.UIManager?.HideGhostTimer();

        // ② 敵のビジュアルルートを取得して Player の Transform の子に移す
        if (_enemy != null)
        {
            _enemyVisual = _enemy.GetVisualRoot();
            _enemyVisualOriginalParent   = _enemyVisual.parent;
            _enemyVisualOriginalLocalPos = _enemyVisual.localPosition;
            _enemyVisualOriginalLocalRot = _enemyVisual.localRotation;

            // Player の子にする（worldPositionStays: false = ローカル座標で配置）
            _enemyVisual.SetParent(Machine.transform, false);

            // 高さのオフセットは敵ごとに違う（例: Demon +2.83 / Mushroom -0.5）。
            // ゼロにすると背の高い敵ほどモデルが地面にめり込むので、
            // 敵として立っていたときのオフセットをそのまま使う。
            // Player と敵の CharacterController は同寸（高さ1・中心0）なので
            // ローカルオフセットはそのまま移し替えられる。
            _enemyVisual.localPosition = _enemyVisualOriginalLocalPos;
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
            _leavingForDodge = false;
            Debug.Log("[Hijacked] Exit (→Dodge) — ビジュアル保持");
            return;
        }

        // 幽霊タイマー UI を再表示（Ghost に戻るとき）
        Machine.UIManager?.ShowGhostTimer();

        // 幽霊モデルを再表示
        if (Machine.PlayerVisual != null)
            Machine.PlayerVisual.SetActive(true);

        // 非表示のあいだ Animator は止まっていて、再表示でデフォルトステートに戻る。
        // GhostAnimation 側のキャッシュを合わせておかないと、
        // 次の CrossFade が「同じステート」と判定されて無視される。
        Machine.GhostAnim?.ResetToIdle();

        // 敵ビジュアルを元の親に戻す
        // HP 0 による遷移の場合は直後に OnHijackedEnemyDied → Destroy されるので
        // ここで親に返しておけば一緒に消える
        RestoreEnemyVisual();

        Debug.Log("[Hijacked] Exit");
    }

    /// <summary>
    /// 敵ビジュアルを元の親・元の位置に戻す。
    /// </summary>
    private void RestoreEnemyVisual()
    {
        if (_enemyVisual == null) return;

        _enemyVisual.SetParent(_enemyVisualOriginalParent);
        _enemyVisual.localPosition = _enemyVisualOriginalLocalPos;
        _enemyVisual.localRotation = _enemyVisualOriginalLocalRot;
        _enemyVisual = null;
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

    // ─────────────────────────────────────────
    // 自発的な離脱 → 敵を破棄して Ghost に戻る
    // ─────────────────────────────────────────

    public void DisposeBody()
    {
        Debug.Log("[Hijacked] 自発的に身体を捨てた → Ghost へ");
        EnemyController dying = _enemy;
        _enemy = null;
        Machine.TransitionTo(Machine.Ghost);  // Exit() が呼ばれビジュアルが戻る
        dying?.OnHijackedEnemyDied();          // 敵 GameObject を破棄
    }
}
