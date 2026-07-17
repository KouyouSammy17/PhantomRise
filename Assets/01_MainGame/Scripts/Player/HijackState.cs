// ============================================================
// HijackState.cs
// エルデンリング風バックスタブ乗っ取り
//
// ① TryStart    : Ghost から → 背後判定・スナップ・QTE 開始
// ② TryTransfer : HijackedState から → 背後/スタン判定・スナップ・QTE 開始
// ③ Enter       : 敵の AI をフリーズ・OnQTEStart イベント発火
// ④ OnQTESuccess: 乗っ取り成立（通常 or 転送）
// ⑤ OnQTEFail  : 失敗 → 元の状態に戻る（通常: Ghost / 転送: HijackedState）
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
        _callerState = Machine.Hijacked;   // 失敗時は HijackedState に戻る

        // HijackedState.Exit() でモデル復元をスキップ（ビジュアルは付けたまま）
        Machine.Hijacked.PrepareTransfer();

        SnapBehindEnemy();
        Machine.TransitionTo(this);
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
    /// 転送時は幽霊モデルが非表示なのでアニメーション待ちをスキップする。
    /// </summary>
    private async UniTaskVoid SuccessAsync()
    {
        _successRunning = true;

        bool isTransfer = _callerState == Machine.Hijacked;

        if (!isTransfer)
        {
            Machine.GhostAnim?.PlayAttack();

            float wait = Machine.GhostAnim != null ? Machine.GhostAnim.AttackAnimTime : 0f;
            if (wait > 0f)
            {
                await UniTask.Delay(
                    TimeSpan.FromSeconds(wait),
                    cancellationToken: Machine.GetCancellationTokenOnDestroy());
            }
        }

        _successRunning = false;

        // アニメーション中に敵が消えた場合は Ghost に戻る
        if (TargetEnemy == null)
        {
            Debug.Log("[Hijack] ターゲット消失 → Ghost に戻る");
            _callerState = null;
            Machine.Ghost.Resume();
            Machine.TransitionTo(Machine.Ghost);
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

        if (_callerState == Machine.Hijacked)
        {
            // 転送フロー: 旧ボディを捨てて新ボディへ
            Debug.Log("[Hijack] 転送成功！");
            Machine.Hijacked.TransferBody(TargetEnemy);
            Machine.TransitionTo(Machine.Hijacked);
        }
        else
        {
            // 通常フロー: Ghost から乗っ取り
            Machine.Hijacked.SetEnemy(TargetEnemy);
            Machine.TransitionTo(Machine.Hijacked);
        }

        _callerState = null;
    }

    public void OnQTEFail()
    {
        TargetEnemy.AlertChase();

        PlayerBaseState returnTo = _callerState;
        _callerState = null;

        if (returnTo == Machine.Hijacked)
        {
            // 転送失敗 → 現在のボディのまま HijackedState に戻る
            Debug.Log("[Hijack] 転送失敗 → 現在のボディに戻る");
            Machine.TransitionTo(Machine.Hijacked);
        }
        else
        {
            // 通常失敗 → Ghost に戻る
            Debug.Log("[Hijack] 失敗 → 敵が気づいた");
            Machine.Ghost.Resume();
            Machine.TransitionTo(Machine.Ghost);
        }
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
