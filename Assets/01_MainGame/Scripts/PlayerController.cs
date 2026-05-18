using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(PlayerInput))]
public class PlayerController : MonoBehaviour
{
    // ─────────────────────────────────────────
    // Inspector パラメーター — 移動
    // ─────────────────────────────────────────
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float gravity = -20f;
    [SerializeField] private float rotationSpeed = 720f;

    // Cinemachine Virtual Camera の Transform をアサイン
    // (Main Camera でも可。固定なので毎フレーム向きが変わらない)
    [SerializeField] private Transform cameraTransform;

    private CharacterController _cc;
    private Vector2 _moveInput;
    private float _velocityY;

    // カメラの水平向きをゲーム開始時に一度だけキャッシュする。
    // 固定カメラなので毎フレーム再計算する必要がない。
    private Vector3 _camForward;
    private Vector3 _camRight;

    private void Awake()
    {
        _cc = GetComponent<CharacterController>();

        if (cameraTransform == null && Camera.main != null)
            cameraTransform = Camera.main.transform;
    }

    private void Start()
    {
        CacheCameraAxes();
    }

    /// <summary>
    /// カメラの水平軸をキャッシュする。
    /// ステージ切り替えなどでカメラ向きが変わったときは
    /// 外部からこのメソッドを呼び直す。
    /// </summary>
    public void CacheCameraAxes()
    {
        if (cameraTransform == null) return;

        // Y 成分を除いて水平面に投影する
        _camForward = Vector3.ProjectOnPlane(cameraTransform.forward, Vector3.up).normalized;
        _camRight = Vector3.ProjectOnPlane(cameraTransform.right, Vector3.up).normalized;
    }

    private void Update()
    {
        Vector3 moveDir = GetMoveDirection();

        // ── 重力 ──────────────────────────────
        if (_cc.isGrounded) _velocityY = -2f;
        else _velocityY += gravity * Time.deltaTime;

        // ── 移動適用 ──────────────────────────
        _cc.Move((moveDir * moveSpeed + Vector3.up * _velocityY) * Time.deltaTime);

        // ── 進行方向へ旋回（カメラは動かない、キャラだけ向く）──
        if (moveDir.sqrMagnitude > 0.01f)
        {
            Quaternion target = Quaternion.LookRotation(moveDir);
            transform.rotation = Quaternion.RotateTowards(
                transform.rotation, target, rotationSpeed * Time.deltaTime);
        }
    }

    // ── Input コールバック ─────────────────────
    // PlayerInput → Events → Player → Move の
    // performed と canceled 両方にバインドすること

    public void OnMoveInput(InputAction.CallbackContext ctx)
    {
        _moveInput = ctx.ReadValue<Vector2>();
    }

    // ── ユーティリティ ────────────────────────

    private Vector3 GetMoveDirection()
    {
        if (_moveInput.sqrMagnitude < 0.01f) return Vector3.zero;

        // キャッシュ済みの固定軸を使う（毎フレーム ProjectOnPlane しない）
        return (_camForward * _moveInput.y + _camRight * _moveInput.x).normalized;
    }
}
