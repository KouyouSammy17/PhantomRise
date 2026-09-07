// ============================================================
// StunTrap.cs
// スタントラップ：踏んだ敵を一時的にスタン（行動停止）させる
//
// 使い方:
//   1. 空の GameObject に BoxCollider or SphereCollider (Is Trigger: true) をアタッチ
//   2. このスクリプトをアタッチ
//   3. 敵の GameObject に "Enemy" タグを設定しておく
//   4. stunDuration でスタン時間、trapCooldown で再発動までの待機時間を調整
// ============================================================

using UnityEngine;

public class StunTrap : MonoBehaviour
{
    // ─────────────────────────────────────────
    // Inspector
    // ─────────────────────────────────────────

    [Header("=== スタン設定 ===")]
    [Tooltip("プレイヤーがトラップを踏んだときのスタン持続時間（秒）")]
    [SerializeField] private float stunDurationPlayer = 3f;

    [Tooltip("ランクD（最弱）のスタン持続時間（秒）。ランクが上がるほど短くなる。")]
    [SerializeField] private float stunDurationRankD = 6f;

    [Tooltip("ランクCのスタン持続時間（秒）")]
    [SerializeField] private float stunDurationRankC = 4f;

    [Tooltip("ランクBのスタン持続時間（秒）")]
    [SerializeField] private float stunDurationRankB = 2.5f;

    [Tooltip("ランクA（最強）のスタン持続時間（秒）")]
    [SerializeField] private float stunDurationRankA = 1f;

    [Tooltip("トラップ再発動までのクールダウン（秒）。0 なら無制限に発動。")]
    [SerializeField] private float trapCooldown = 8f;

    [Header("=== 演出（任意）===")]
    [Tooltip("発動中に有効化するビジュアル（エフェクト等）")]
    [SerializeField] private GameObject activatedVisual;

    [Tooltip("待機中のデフォルトビジュアル")]
    [SerializeField] private GameObject idleVisual;

    // ─────────────────────────────────────────
    // 内部
    // ─────────────────────────────────────────

    private bool _onCooldown = false;

    private Collider _collider;
    private readonly Collider[] _overlapBuffer = new Collider[16];

    private void Awake()
    {
        _collider = GetComponent<Collider>();
    }

    // ─────────────────────────────────────────
    // 重なり判定（毎フレーム）
    //
    // 敵は NavMeshAgent が Transform を直接動かしていて、
    // Rigidbody も CharacterController.Move() も通らないため
    // OnTriggerEnter が飛んでこない。
    // （プレイヤーは CharacterController.Move() で動くので飛んでくる）
    // トラップ側から重なりを探しに行けば、動かし方に関係なく反応できる。
    // ─────────────────────────────────────────

    private void Update()
    {
        if (_onCooldown || _collider == null) return;

        int count = OverlapTrap(_overlapBuffer);

        for (int i = 0; i < count; i++)
        {
            if (TryTrigger(_overlapBuffer[i])) return;   // 発動したら終わり
        }
    }

    private int OverlapTrap(Collider[] results)
    {
        Vector3 scale = transform.lossyScale;

        if (_collider is SphereCollider sphere)
        {
            float radius = sphere.radius * Mathf.Max(scale.x, Mathf.Max(scale.y, scale.z));

            return Physics.OverlapSphereNonAlloc(
                transform.TransformPoint(sphere.center), radius, results,
                ~0, QueryTriggerInteraction.Ignore);
        }

        if (_collider is BoxCollider box)
        {
            return Physics.OverlapBoxNonAlloc(
                transform.TransformPoint(box.center),
                Vector3.Scale(box.size, scale) * 0.5f, results,
                transform.rotation, ~0, QueryTriggerInteraction.Ignore);
        }

        // その他の形状は bounds で近似する
        Bounds bounds = _collider.bounds;

        return Physics.OverlapBoxNonAlloc(
            bounds.center, bounds.extents, results,
            Quaternion.identity, ~0, QueryTriggerInteraction.Ignore);
    }

    // ─────────────────────────────────────────
    // トリガー判定
    // ─────────────────────────────────────────

    //private void OnTriggerEnter(Collider other)
    //{
    //    if (_onCooldown) return;

    //    // ─── プレイヤー判定 ───
    //    if (other.CompareTag("Player"))
    //    {
    //        PlayerStateMachine player = other.GetComponent<PlayerStateMachine>()
    //                                 ?? other.GetComponentInParent<PlayerStateMachine>();
    //        if (player == null) return;

    //        player.ApplyStun(stunDurationPlayer);
    //        Debug.Log($"[StunTrap] プレイヤーを {stunDurationPlayer}秒スタン！");

    //        ActivateTrap();
    //        return;
    //    }

    //    // ─── 敵判定 ───
    //    if (!other.CompareTag("Enemy")) return;

    //    EnemyController enemy = other.GetComponent<EnemyController>()
    //                         ?? other.GetComponentInParent<EnemyController>();
    //    if (enemy == null) return;

    //    // ボスはスタントラップ無効
    //    if (enemy is BossController)
    //    {
    //        Debug.Log($"[StunTrap] {enemy.name} はボスのためスタン無効");
    //        return;
    //    }

    //    float stunDuration = GetStunDurationForRank(enemy.Rank);
    //    enemy.ApplyStun(stunDuration);
    //    Debug.Log($"[StunTrap] {enemy.name}（ランク{enemy.Rank}）を {stunDuration}秒スタン！");

    //    ActivateTrap();
    //}

    private void OnTriggerEnter(Collider other) => TryTrigger(other);

    /// <summary>
    /// 相手がプレイヤー／敵ならスタンさせる。
    /// 発動したら true。
    /// </summary>
    private bool TryTrigger(Collider other)
    {
        if (_onCooldown || other == null) return false;

        // =========================================
        // プレイヤー判定
        // =========================================

        PlayerStateMachine player = other.GetComponent<PlayerStateMachine>()
                                 ?? other.GetComponentInParent<PlayerStateMachine>();

        if (player != null)
        {
            player.ApplyStun(stunDurationPlayer);

            Debug.Log($"[StunTrap] プレイヤーを {stunDurationPlayer}秒スタン！");

            ActivateTrap();
            return true;
        }

        // =========================================
        // 敵判定
        // =========================================

        if (!other.CompareTag("Enemy"))
            return false;

        EnemyController enemy = other.GetComponent<EnemyController>()
                             ?? other.GetComponentInParent<EnemyController>();

        if (enemy == null)
            return false;

        // 乗っ取り中の敵＝プレイヤー本体なので、敵としては扱わない
        if (enemy.IsHijacked)
            return false;

        // ボスはスタントラップ無効
        if (enemy is BossController)
        {
            Debug.Log($"[StunTrap] {enemy.name} はボスのためスタン無効");
            return false;
        }

        float stunDuration = GetStunDurationForRank(enemy.Rank);

        enemy.ApplyStun(stunDuration);

        Debug.Log(
            $"[StunTrap] {enemy.name}（ランク{enemy.Rank}）を {stunDuration}秒スタン！"
        );

        ActivateTrap();
        return true;
    }

    private void ActivateTrap()
    {
        // ビジュアル切り替え
        if (activatedVisual != null) activatedVisual.SetActive(true);
        if (idleVisual      != null) idleVisual.SetActive(false);

        // クールダウン開始
        if (trapCooldown > 0f)
        {
            _onCooldown = true;
            Invoke(nameof(ResetTrap), trapCooldown);
        }
    }

    private float GetStunDurationForRank(EnemyController.EnemyRank rank)
    {
        return rank switch
        {
            EnemyController.EnemyRank.A => stunDurationRankA,
            EnemyController.EnemyRank.B => stunDurationRankB,
            EnemyController.EnemyRank.C => stunDurationRankC,
            _                           => stunDurationRankD,  // D（デフォルト）
        };
    }

    private void ResetTrap()
    {
        _onCooldown = false;

        if (activatedVisual != null) activatedVisual.SetActive(false);
        if (idleVisual      != null) idleVisual.SetActive(true);

        Debug.Log("[StunTrap] リセット完了");
    }

    // ─────────────────────────────────────────
    // エディタ Gizmo
    // ─────────────────────────────────────────

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        Collider col = GetComponent<Collider>();
        if (col == null) return;

        Gizmos.color = new Color(1f, 0.8f, 0f, 0.25f);   // 黄色
        Gizmos.matrix = transform.localToWorldMatrix;

        if (col is BoxCollider box)
            Gizmos.DrawCube(box.center, box.size);
        else if (col is SphereCollider sphere)
            Gizmos.DrawSphere(sphere.center, sphere.radius);

        Gizmos.color = new Color(1f, 0.6f, 0f, 0.9f);
        if (col is BoxCollider box2)
            Gizmos.DrawWireCube(box2.center, box2.size);
        else if (col is SphereCollider sphere2)
            Gizmos.DrawWireSphere(sphere2.center, sphere2.radius);
    }
#endif
}
