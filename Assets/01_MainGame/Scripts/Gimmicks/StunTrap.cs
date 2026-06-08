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
    [Tooltip("スタン持続時間（秒）")]
    [SerializeField] private float stunDuration = 3f;

    [Tooltip("トラップ再発動までのクールダウン（秒）。0 なら無制限に発動。")]
    [SerializeField] private float trapCooldown = 5f;

    [Header("=== 演出（任意）===")]
    [Tooltip("発動中に有効化するビジュアル（エフェクト等）")]
    [SerializeField] private GameObject activatedVisual;

    [Tooltip("待機中のデフォルトビジュアル")]
    [SerializeField] private GameObject idleVisual;

    // ─────────────────────────────────────────
    // 内部
    // ─────────────────────────────────────────

    private bool _onCooldown = false;

    // ─────────────────────────────────────────
    // トリガー判定
    // ─────────────────────────────────────────

    private void OnTriggerEnter(Collider other)
    {
        if (_onCooldown) return;
        if (!other.CompareTag("Enemy")) return;

        EnemyController enemy = other.GetComponent<EnemyController>()
                             ?? other.GetComponentInParent<EnemyController>();
        if (enemy == null) return;

        enemy.ApplyStun(stunDuration);
        Debug.Log($"[StunTrap] {enemy.name} をスタン！");

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
