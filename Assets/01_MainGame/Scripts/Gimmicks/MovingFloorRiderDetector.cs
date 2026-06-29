// ============================================================
// MovingFloorRiderDetector.cs
// MovingFloor の子オブジェクトにアタッチして使う補助コンポーネント。
// 床上面の薄い Trigger Collider でプレイヤーの乗り降りを検出し、
// CharacterController の参照を MovingFloor に渡す。
//
// 使い方:
//   1. MovingFloor GameObject の子に "RiderDetector" という空の GameObject を作成
//   2. BoxCollider（Is Trigger: true）を追加し、床上面ぴったりに合わせる
//      （高さを 0.1f 程度にするとよい）
//   3. このスクリプトをアタッチ
//   4. 親の MovingFloor Inspector で _riderDetector に割り当てる
// ============================================================

using UnityEngine;

[RequireComponent(typeof(Collider))]
public class MovingFloorRiderDetector : MonoBehaviour
{
    /// <summary>現在トリガー内にいるプレイヤーの CC。いなければ null。</summary>
    public CharacterController RiderCC { get; private set; }

    public void Init(MovingFloor floor) { /* 将来の拡張用 */ }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        CharacterController cc = other.GetComponent<CharacterController>()
                              ?? other.GetComponentInParent<CharacterController>();
        if (cc == null) return;

        RiderCC = cc;
        Debug.Log("[MovingFloor] プレイヤーが乗った");
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        RiderCC = null;
        Debug.Log("[MovingFloor] プレイヤーが降りた");
    }
}
