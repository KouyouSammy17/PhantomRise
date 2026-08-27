// ============================================================
// HijackHealItem.cs
// 乗っ取り中（HijackedState）にのみ使用できる回復アイテム
//
// 使い方:
//   1. 空の GameObject に SphereCollider (Is Trigger: true) をアタッチ
//   2. このスクリプトをアタッチ
//   3. Inspector で HealAmount（回復量）を設定
//   4. 乗っ取り中のプレイヤーが踏むと HP を回復する
//      （Ghost 状態では取得できない — 乗っ取り専用）
// ============================================================

using UnityEngine;

public class HijackHealItem : MonoBehaviour
{
    [Header("=== アイテム設定 ===")]
    [Tooltip("回復する HP 量")]
    public int HealAmount = 2;

    [Tooltip("取得後に自身を Destroy するか（false にすると繰り返し使える）")]
    public bool DestroyOnPickup = true;

    [Header("=== エフェクト（任意）===")]
    [Tooltip("取得時に生成するエフェクト Prefab（省略可）")]
    public GameObject PickupEffectPrefab;

    [SerializeField] private AudioSource HealItemAudio;


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

        // 乗っ取り中（HijackedState）のときだけ有効
        if (machine.CurrentStateName != nameof(HijackedState))
        {
            Debug.Log("[HijackHealItem] 乗っ取り中でないため取得できません");
            return;
        }

        HealItemAudio.Play();

        // HP 回復
        machine.PlayerHP.Heal(HealAmount);
        Debug.Log($"[HijackHealItem] HP +{HealAmount} 回復");

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
        Gizmos.color = new Color(0.2f, 1f, 0.4f, 0.4f);
        Gizmos.DrawSphere(transform.position, 0.5f);
        Gizmos.color = new Color(0.2f, 1f, 0.4f, 1f);
        Gizmos.DrawWireSphere(transform.position, 0.5f);
    }
#endif
}
