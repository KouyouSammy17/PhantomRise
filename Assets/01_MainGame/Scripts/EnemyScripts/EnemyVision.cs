using UnityEngine;

public class EnemyVision : MonoBehaviour
{
    [SerializeField] private Transform player;

    [SerializeField] private float _chaseRange = 10f;
    public float chaseRange => _chaseRange;
    [SerializeField] private float _viewAngle = 90f;
    public float viewAngle => _viewAngle;

    [SerializeField] private LayerMask obstacleMask;
    [SerializeField] private LayerMask playerMask;

   public bool CanSeePlayer()
    {
        Vector3 eyePosition = transform.position + Vector3.up * 1.5f;

        Vector3 dirToPlayer = (player.position - eyePosition).normalized;

        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        // 距離判定
        if (distanceToPlayer > _chaseRange)
            return false;

        // 角度判定
        float angle = Vector3.Angle(transform.forward, dirToPlayer);

        if (angle > _viewAngle / 2f)
            return false;

        // プレイヤーまでRayを飛ばす
        Ray ray = new Ray(eyePosition, dirToPlayer);

        // 壁またはプレイヤーにだけ当たる
        LayerMask combinedMask = obstacleMask | playerMask;

        if (Physics.Raycast(ray, out RaycastHit hit, _chaseRange, combinedMask))
        {
            // 最初に当たったのがプレイヤーなら視認成功
            if (((1 << hit.collider.gameObject.layer) & playerMask) != 0)
            {
                return true;
            }
        }

        return false;
    }
}