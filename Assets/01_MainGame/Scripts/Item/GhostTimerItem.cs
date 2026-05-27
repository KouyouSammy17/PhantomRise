// ============================================================
// GhostTimerItem.cs
// ゴーストタイマーを延長するアイテム
//
// 使い方:
//   1. 空の GameObject に SphereCollider (Is Trigger: true) をアタッチ
//   2. このスクリプトをアタッチ
//   3. Inspector で TimeBonus（秒数）を設定
//   4. プレイヤーが Ghost または Hijacked 状態で踏むとタイマーが延長される
// ============================================================

using UnityEngine;

public class GhostTimerItem : MonoBehaviour
{
    [Header("=== アイテム設定 ===")]
    [Tooltip("延長する秒数")]
    [SerializeField] private float TimeBonus = 15f;

    [Tooltip("取得後に自身を Destroy するか（false にすると繰り返し使える）")]
    [SerializeField] private bool DestroyOnPickup = true;

    [Header("=== エフェクト（任意）===")]
    [Tooltip("取得時に生成するエフェクト Prefab（省略可）")]
    [SerializeField] private GameObject PickupEffectPrefab;

    // ─────────────────────────────────────────
    // トリガー判定
    // ─────────────────────────────────────────

    private void OnTriggerEnter(Collider other)
    {
        // Player タグが付いた GameObject のみ反応
        if (!other.CompareTag("Player")) return;

        PlayerStateMachine machine = other.GetComponent<PlayerStateMachine>()
                                  ?? other.GetComponentInParent<PlayerStateMachine>();
        if (machine == null) return;

        // Ghost 状態または Hijacked 状態のときだけ受け取れる
        string state = machine.CurrentStateName;
        if (state != nameof(GhostState) && state != nameof(HijackedState)) return;

        // タイマー延長
        machine.AddGhostTime(TimeBonus);
        Debug.Log($"[GhostTimerItem] ゴーストタイマー +{TimeBonus}s");

        // エフェクト生成
        if (PickupEffectPrefab != null)
            Instantiate(PickupEffectPrefab, transform.position, Quaternion.identity);

        if (DestroyOnPickup)
            Destroy(gameObject);

    }

#if UNITY_EDITOR
    // エディタ上での視認性のため Gizmo を表示
    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(0.4f, 0.8f, 1f, 0.4f);
        Gizmos.DrawSphere(transform.position, 0.5f);
        Gizmos.color = new Color(0.4f, 0.8f, 1f, 1f);
        Gizmos.DrawWireSphere(transform.position, 0.5f);
    }
#endif
}
