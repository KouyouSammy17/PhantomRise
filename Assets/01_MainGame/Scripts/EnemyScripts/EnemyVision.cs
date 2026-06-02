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

    [Header("=== シャドウゾーン ===")]
    [Tooltip("シャドウゾーン内の幽霊に対して有効な視野距離の割合（0〜1）")]
    [SerializeField, Range(0f, 1f)] private float shadowRangeMultiplier = 0.2f;

    // プレイヤーの状態機械（シャドウゾーン判定に使用）
    private PlayerStateMachine _playerMachine;

    private void Start()
    {
        if (player != null)
            _playerMachine = player.GetComponent<PlayerStateMachine>();
    }

    public bool CanSeePlayer()
    {
        Vector3 eyePosition = transform.position + Vector3.up * 1.5f;

        Vector3 dirToPlayer = (player.position - eyePosition).normalized;

        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        // ── シャドウゾーン補正 ──────────────────────────────
        // 幽霊状態 かつ シャドウゾーン内なら視野距離を大幅に縮小する
        float effectiveRange = _chaseRange;
        if (_playerMachine != null
            && _playerMachine.IsInShadowZone
            && _playerMachine.CurrentStateName == nameof(GhostState))
        {
            effectiveRange *= shadowRangeMultiplier;
        }

        // 距離判定
        if (distanceToPlayer > effectiveRange)
            return false;

        // 角度判定
        float angle = Vector3.Angle(transform.forward, dirToPlayer);

        if (angle > _viewAngle / 2f)
            return false;

        // プレイヤーまでRayを飛ばす
        Ray ray = new Ray(eyePosition, dirToPlayer);

        // 壁またはプレイヤーにだけ当たる
        LayerMask combinedMask = obstacleMask | playerMask;

        if (Physics.Raycast(ray, out RaycastHit hit, effectiveRange, combinedMask))
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