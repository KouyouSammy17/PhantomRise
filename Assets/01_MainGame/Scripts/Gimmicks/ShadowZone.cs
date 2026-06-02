// ============================================================
// ShadowZone.cs
// シャドウゾーン：幽霊状態で隠れられる場所
//
// 仕組み:
//   プレイヤーがゾーンに入ると PlayerStateMachine.IsInShadowZone = true
//   EnemyVision.CanSeePlayer() がこのフラグを見て
//   有効視野距離を shadowRangeMultiplier 倍に縮小する
//   → 敵の真正面に立たない限り見つからなくなる
//
// 使い方:
//   1. 空の GameObject に BoxCollider or SphereCollider (Is Trigger: true) をアタッチ
//   2. このスクリプトをアタッチ
//   3. プレイヤーの GameObject に "Player" タグを設定しておく
//   4. 必要に応じて視覚的なメッシュ・エフェクトを子に追加
// ============================================================

using UnityEngine;

public class ShadowZone : MonoBehaviour
{
    [Header("=== 演出（任意）===")]
    [Tooltip("ゾーン内に入ったときに有効化するビジュアル（暗いフォグなど）")]
    public GameObject ZoneVisualActive;

    [Tooltip("ゾーン外のデフォルトビジュアル")]
    public GameObject ZoneVisualIdle;

    // ─────────────────────────────────────────
    // トリガー判定
    // ─────────────────────────────────────────

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        PlayerStateMachine machine = other.GetComponent<PlayerStateMachine>()
                                  ?? other.GetComponentInParent<PlayerStateMachine>();
        if (machine == null) return;

        machine.IsInShadowZone = true;
        Debug.Log("[ShadowZone] プレイヤーがシャドウゾーンに入った");

        // ビジュアル切り替え
        if (ZoneVisualActive != null) ZoneVisualActive.SetActive(true);
        if (ZoneVisualIdle   != null) ZoneVisualIdle.SetActive(false);
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        PlayerStateMachine machine = other.GetComponent<PlayerStateMachine>()
                                  ?? other.GetComponentInParent<PlayerStateMachine>();
        if (machine == null) return;

        machine.IsInShadowZone = false;
        Debug.Log("[ShadowZone] プレイヤーがシャドウゾーンから出た");

        // ビジュアル切り替え
        if (ZoneVisualActive != null) ZoneVisualActive.SetActive(false);
        if (ZoneVisualIdle   != null) ZoneVisualIdle.SetActive(true);
    }

#if UNITY_EDITOR
    // エディタ上での視認性のため Gizmo を表示（暗紫色）
    private void OnDrawGizmos()
    {
        Collider col = GetComponent<Collider>();
        if (col == null) return;

        Gizmos.color = new Color(0.3f, 0f, 0.5f, 0.25f);
        Gizmos.matrix = transform.localToWorldMatrix;

        if (col is BoxCollider box)
            Gizmos.DrawCube(box.center, box.size);
        else if (col is SphereCollider sphere)
            Gizmos.DrawSphere(sphere.center, sphere.radius);

        Gizmos.color = new Color(0.6f, 0.2f, 1f, 0.8f);
        if (col is BoxCollider box2)
            Gizmos.DrawWireCube(box2.center, box2.size);
        else if (col is SphereCollider sphere2)
            Gizmos.DrawWireSphere(sphere2.center, sphere2.radius);
    }
#endif
}
