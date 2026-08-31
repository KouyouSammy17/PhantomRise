// ============================================================
// HijackState.cs
// エルデンリング風バックスタブ乗っ取り
//
// ① TryStart    : Ghost から → 背後判定・スナップ・QTE 開始
// ② TryTransfer : HijackedState から → 今のボディを捨ててから同じ QTE へ
// ③ Enter       : 敵の AI をフリーズ・OnQTEStart イベント発火
// ④ OnQTESuccess: 幽霊の成功アニメーション → 乗っ取り成立
// ⑤ OnQTEFail  : 失敗 → Ghost に戻る
//
// 転送でもボディを先に捨てるので、QTE 中は必ず幽霊の姿になる。
// （乗っ取り中は幽霊モデルが非表示で Animator も止まっており、
//   そのままでは成功アニメーションが再生されないため）
// ============================================================

using System;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class HijackState : PlayerBaseState
{
    public EnemyController TargetEnemy { get; private set; }
    public event System.Action<EnemyController> OnQTEStart;

    // QTE 終了後に戻る状態（null = Ghost / Machine.Hijacked = 転送）
    private PlayerBaseState _callerState;

    // 攻撃アニメーション再生待ち中の二重実行防止
    private bool _successRunning;

    public HijackState(PlayerStateMachine machine) : base(machine) { }

    // ─────────────────────────────────────────
    // Ghost から: 背後判定 → スナップ → QTE 開始
    // ─────────────────────────────────────────

    public bool TryStart(float range, float behindAngle)
    {
        EnemyController target = FindTargetBehind(range, behindAngle);
        if (target == null) return false;

        TargetEnemy   = target;
        _callerState  = null;   // 通常フロー: 失敗時は Ghost に戻る

        SnapBehindEnemy();
        Machine.TransitionTo(this);
        return true;
    }

    // ─────────────────────────────────────────
    // HijackedState から: 転送対象を探して QTE 開始
    // ─────────────────────────────────────────

    public bool TryTransfer(float range, float behindAngle)
    {
        EnemyController target = FindTargetBehind(range, behindAngle);
        if (target == null) return false;

        TargetEnemy  = target;
        _callerState = Machine.Hijacked;   // 「ボディを捨てて来た」目印（失敗時のタイマー用）

        // 乗っ取りに入る時点で今のボディを捨てる。
        // 以降は幽霊の姿なので、Ghost からの通常フローと同じ演出になる。
        // （乗っ取り中は幽霊モデルが非表示 ＝ Animator も止まっていて
        //   アニメーションが再生されないため）
        EnemyController oldBody = Machine.Hijacked.ReleaseBody();

        SnapBehindEnemy();

        // Hijacked.Exit() が走ってビジュアルが敵に返り、幽霊モデルが復活する
        Machine.TransitionTo(this);

        // ビジュアルを返した後に破棄する（先に消すと付いたままのモデルごと消える）
        oldBody?.OnHijackedEnemyDied();
        return true;
    }

    public override void Enter()
    {
        Machine.VelocityY = 0f;

        // QTE 中は敵の AI を止めておく
        TargetEnemy?.FreezeForQTE();

        // 乗っ取りアニメーション（ghost_attack_shift）
        Machine.GhostAnim?.PlayHijack();

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
        if (_successRunning) return;
        SuccessAsync().Forget();
    }

    /// <summary>
    /// QTE 成功 → 攻撃アニメーション（ghost_attack）を再生し、
    /// 完了を待ってから乗っ取りを成立させる。
    ///
    /// 転送でもボディは QTE 開始時に捨てているので、
    /// どちらも幽霊の姿で同じアニメーションが流れる。
    /// </summary>
    private async UniTaskVoid SuccessAsync()
    {
        _successRunning = true;

        Machine.GhostAnim?.PlayAttack();

        float wait = Machine.GhostAnim != null ? Machine.GhostAnim.AttackAnimTime : 0f;
        if (wait > 0f)
        {
            await UniTask.Delay(
                TimeSpan.FromSeconds(wait),
                cancellationToken: Machine.GetCancellationTokenOnDestroy());
        }

        _successRunning = false;

        // アニメーション中に敵が消えた場合は Ghost に戻る
        if (TargetEnemy == null)
        {
            Debug.Log("[Hijack] ターゲット消失 → Ghost に戻る");
            ReturnToGhost();
            return;
        }

        Debug.Log("[Hijack] 成功！");

        TargetEnemy.BecomeHijacked();

        //スキルのクールダウンをリセットして即座に使えるようにする
        EnemySkillBase skill =
    TargetEnemy.GetComponent<EnemySkillBase>();

        if (skill != null)
        {
            skill.ResetSkillImmediately();
        }

        Machine.CC.enabled = false;
        Machine.transform.position = TargetEnemy.transform.position;
        Machine.CC.enabled = true;

        Machine.PlayerHP.Initialize(TargetEnemy.MaxHP, TargetEnemy.CurrentHP);

        // 転送も通常フローも、この時点では幽霊なので扱いは同じ
        Machine.Hijacked.SetEnemy(TargetEnemy);
        Machine.TransitionTo(Machine.Hijacked);

        _callerState = null;
    }

    public void OnQTEFail()
    {
        TargetEnemy?.AlertChase();

        Debug.Log(_callerState == Machine.Hijacked
            ? "[Hijack] 転送失敗 → ボディは捨てているので Ghost へ"
            : "[Hijack] 失敗 → 敵が気づいた");

        ReturnToGhost();
    }

    /// <summary>
    /// Ghost に戻す。
    ///
    /// 通常フローはもともと幽霊だったのでタイマーを引き継ぐ（Resume）。
    /// 転送フローは直前にボディを捨てたところなので、
    /// 身体を捨てたときと同じくタイマーを 0 から数え直す。
    /// </summary>
    private void ReturnToGhost()
    {
        bool cameFromBody = _callerState == Machine.Hijacked;
        _callerState = null;

        if (!cameFromBody) Machine.Ghost.Resume();
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
                    Debug.Log($"[Hijack] SKIP {enemy.name}: ランク {enemy.Rank} かつスタン中でない");
                    continue;
                }
                Debug.Log($"[Hijack] {enemy.name}({enemy.Rank}): スタン中 → 乗っ取り可能");
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
