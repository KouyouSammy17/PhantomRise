// ============================================================
// HijackState.cs
// エルデンリング風バックスタブ乗っ取り
//
// ① TryStart  : 背後判定 → 即座に敵の真後ろにスナップ → QTE 開始
// ② Enter     : 敵の AI をフリーズ・OnQTEStart イベント発火
// ③ OnQTESuccess : 乗っ取り成立
// ④ OnQTEFail    : 失敗 → 敵が気づく
// ============================================================

using UnityEngine;

public class HijackState : PlayerBaseState
{
    public EnemyController TargetEnemy { get; private set; }
    public event System.Action<EnemyController> OnQTEStart;

    public HijackState(PlayerStateMachine machine) : base(machine) { }

    // ─────────────────────────────────────────
    // 背後判定 → スナップ → QTE 開始
    // ─────────────────────────────────────────

    public bool TryStart(float range, float behindAngle)
    {
        EnemyController target = FindTargetBehind(range, behindAngle);
        if (target == null) return false;

        TargetEnemy = target;

        // エルデンリング風: プレイヤーを敵の真後ろへ即スナップ
        SnapBehindEnemy();

        Machine.TransitionTo(this);
        return true;
    }

    public override void Enter()
    {
        Machine.VelocityY = 0f;

        // QTE 中は敵の AI を止めておく
        TargetEnemy?.FreezeForQTE();

        Debug.Log($"[Hijack] QTE 開始 → {TargetEnemy?.name}");
        OnQTEStart?.Invoke(TargetEnemy);
    }

    public override void Update(float deltaTime) { }
    public override void Exit() { }

    // ─────────────────────────────────────────
    // QTE 結果（HijackQTEUI から呼ぶ）
    // ─────────────────────────────────────────

    public void OnQTESuccess()
    {
        Debug.Log("[Hijack] 成功！");

        // 敵の AI を永続停止・コライダー無効
        TargetEnemy.BecomeHijacked();

        // プレイヤーを敵の位置へ（乗っ取りボディとして使う）
        Machine.CC.enabled = false;
        Machine.transform.position = TargetEnemy.transform.position;
        Machine.CC.enabled = true;

        // 敵の HP を PlayerHP に転写
        Machine.PlayerHP.Initialize(TargetEnemy.MaxHP, TargetEnemy.CurrentHP);
        Machine.Hijacked.SetEnemy(TargetEnemy);
        Machine.TransitionTo(Machine.Hijacked);
    }

    public void OnQTEFail()
    {
        Debug.Log("[Hijack] 失敗 → 敵が気づいた");
        TargetEnemy.AlertChase();   // FreezeForQTE 解除 + Chase 状態

        Machine.Ghost.Resume();
        Machine.TransitionTo(Machine.Ghost);
    }

    // ─────────────────────────────────────────
    // エルデンリング風: 敵の真後ろにスナップ
    // ─────────────────────────────────────────

    private void SnapBehindEnemy()
    {
        if (TargetEnemy == null) return;

        // 敵の背後 0.8m の位置（地面 Y に合わせる）
        Vector3 behindPos = TargetEnemy.transform.position
                            - TargetEnemy.transform.forward * 0.8f;
        behindPos.y = TargetEnemy.transform.position.y;

        Machine.CC.enabled = false;
        Machine.transform.position = behindPos;
        Machine.CC.enabled = true;

        // 敵の方向を向く
        Vector3 lookDir = TargetEnemy.transform.position - Machine.transform.position;
        lookDir.y = 0;
        if (lookDir.sqrMagnitude > 0.01f)
            Machine.transform.rotation = Quaternion.LookRotation(lookDir);
    }

    // ─────────────────────────────────────────
    // 背後判定ロジック
    // ─────────────────────────────────────────

    private EnemyController FindTargetBehind(float range, float behindAngle)
    {
        Collider[] hits = Physics.OverlapSphere(Machine.transform.position, range);
        Debug.Log($"[Hijack] OverlapSphere hit {hits.Length} colliders (range={range})");

        EnemyController best = null;
        float bestDist = float.MaxValue;

        // 有効な背後アーク: BehindAngle の幅を敵の背後に投影
        // angle >= (180 - BehindAngle/2) であれば "背後ゾーン" に入っている
        float threshold = 180f - behindAngle * 0.5f;

        foreach (Collider col in hits)
        {
            EnemyController enemy = col.GetComponentInParent<EnemyController>();
            if (enemy == null) continue;

            if (enemy.IsHijacked)
            {
                Debug.Log($"[Hijack] SKIP {enemy.name}: すでに乗っ取り済み");
                continue;
            }

            if (enemy.Rank == EnemyController.EnemyRank.D)
            {
                // ── D ランク: 背後からのバックスタブのみ ──────────────
                Vector3 enemyToPlayer = (Machine.transform.position - enemy.transform.position).normalized;
                float angle = Vector3.Angle(enemy.transform.forward, enemyToPlayer);

                Debug.Log($"[Hijack] {enemy.name}(D): 角度={angle:F1}° 閾値={threshold:F1}°");

                if (angle < threshold)
                {
                    Debug.Log($"[Hijack] SKIP {enemy.name}: 背後ゾーン外");
                    continue;
                }
            }
            else
            {
                // ── C / B / A ランク: スタン中のみ・方向不問 ──────────
                if (!enemy.IsStunned)
                {
                    Debug.Log($"[Hijack] SKIP {enemy.name}: ランク {enemy.rank} かつスタン中でない");
                    continue;
                }
                Debug.Log($"[Hijack] {enemy.name}({enemy.rank}): スタン中 → 乗っ取り可能");
            }

            float dist = Vector3.Distance(Machine.transform.position, enemy.transform.position);
            if (dist < bestDist) { bestDist = dist; best = enemy; }
        }

        if (best == null)
            Debug.Log("[Hijack] ターゲットが見つかりませんでした");
        else
            Debug.Log($"[Hijack] ターゲット決定: {best.name}");

        return best;
    }
}
