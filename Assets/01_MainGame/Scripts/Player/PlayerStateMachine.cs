// ============================================================
// PlayerStateMachine.cs
// 状態の登録・切り替えを管理する
// PlayerController にアタッチして使う
// ============================================================

using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(PlayerInput))]
public class PlayerStateMachine : MonoBehaviour
{
    // ─────────────────────────────────────────
    // Inspector パラメーター
    // ─────────────────────────────────────────

    [Header("=== 移動 ===")]
    public float MoveSpeed = 5f;
    public float Gravity = -20f;
    public float RotationSpeed = 720f;

    [Header("=== 回避 ===")]
    public float DodgeSpeed = 12f;
    public float DodgeDuration = 0.25f;
    public float DodgeCooldown = 1.0f;

    [Header("=== 幽霊タイマー ===")]
    public float GhostTimeLimit = 60f;

    [Header("=== カメラ ===")]
    public Transform CameraTransform;

    // ─────────────────────────────────────────
    // UnityEvents（UI・GameManager への通知）
    // ─────────────────────────────────────────

    [Header("=== イベント ===")]
    public UnityEvent<float> OnGhostTimerUpdate;  // 残り秒数（毎フレーム）
    public UnityEvent OnPlayerDead;

    // ─────────────────────────────────────────
    // 共有データ（各状態クラスから参照）
    // ─────────────────────────────────────────

    public CharacterController CC { get; private set; }
    public Vector2 MoveInput { get; private set; }
    public float VelocityY { get; set; }

    // カメラ固定軸キャッシュ
    public Vector3 CamForward { get; private set; }
    public Vector3 CamRight { get; private set; }

    // 現在の状態名（デバッグ・UI 用）
    public string CurrentStateName => _currentState?.GetType().Name ?? "None";

    // ─────────────────────────────────────────
    // 状態インスタンス
    // ─────────────────────────────────────────

    public GhostState Ghost { get; private set; }
    public HijackedState Hijacked { get; private set; }
    public DodgeState Dodge { get; private set; }
    public DeadState Dead { get; private set; }

    private PlayerBaseState _currentState;

    // ─────────────────────────────────────────
    // Unity ライフサイクル
    // ─────────────────────────────────────────

    private void Awake()
    {
        CC = GetComponent<CharacterController>();

        if (CameraTransform == null && Camera.main != null)
            CameraTransform = Camera.main.transform;

        // 状態インスタンスを生成（new のみ、Enter はまだ呼ばない）
        Ghost = new GhostState(this);
        Hijacked = new HijackedState(this);
        Dodge = new DodgeState(this);
        Dead = new DeadState(this);
    }

    private void Start()
    {
        CacheCameraAxes();
        TransitionTo(Ghost);   // ゲーム開始 → 幽霊状態
    }

    private void Update()
    {
        _currentState?.Update(Time.deltaTime);
    }

    // ─────────────────────────────────────────
    // 状態遷移
    // ─────────────────────────────────────────

    public void TransitionTo(PlayerBaseState nextState)
    {
        _currentState?.Exit();
        _currentState = nextState;
        _currentState.Enter();
        Debug.Log($"[FSM] → {CurrentStateName}");
    }

    // ─────────────────────────────────────────
    // Input コールバック（Invoke Unity Events）
    // Move performed / canceled 両方にバインドすること
    // ─────────────────────────────────────────

    public void OnMoveInput(InputAction.CallbackContext ctx)
    {
        MoveInput = ctx.ReadValue<Vector2>();
    }

    public void OnDodgeInput(InputAction.CallbackContext ctx)
    {
        if (!ctx.performed) return;
        // Ghost / Hijacked どちらからでも回避可
        if (_currentState == Ghost || _currentState == Hijacked)
            Dodge.SetCaller(_currentState);  // 回避後の戻り先を記憶
        TransitionTo(Dodge);
    }

    // ─────────────────────────────────────────
    // 当たり判定（幽霊状態のみ有効）
    // 敵の攻撃判定コライダーに "EnemyAttack" タグを設定すること
    // ─────────────────────────────────────────

    private void OnTriggerEnter(Collider other)
    {
        if (_currentState != Ghost) return;
        if (!other.CompareTag("EnemyAttack")) return;

        Ghost.OnHit();
    }

    // ─────────────────────────────────────────
    // ユーティリティ
    // ─────────────────────────────────────────

    /// <summary>カメラ水平軸をキャッシュ（固定カメラなので Start 時のみ呼ぶ）</summary>
    public void CacheCameraAxes()
    {
        if (CameraTransform == null) return;
        CamForward = Vector3.ProjectOnPlane(CameraTransform.forward, Vector3.up).normalized;
        CamRight = Vector3.ProjectOnPlane(CameraTransform.right, Vector3.up).normalized;
    }

    /// <summary>入力をカメラ基準のワールド方向に変換</summary>
    public Vector3 GetMoveDirection()
    {
        if (MoveInput.sqrMagnitude < 0.01f) return Vector3.zero;
        return (CamForward * MoveInput.y + CamRight * MoveInput.x).normalized;
    }

    /// <summary>移動＋重力を適用（各状態から呼ぶ共通処理）</summary>
    public void ApplyMovement(float deltaTime)
    {
        Vector3 moveDir = GetMoveDirection();

        if (CC.isGrounded) VelocityY = -2f;
        else VelocityY += Gravity * deltaTime;

        CC.Move((moveDir * MoveSpeed + Vector3.up * VelocityY) * deltaTime);

        if (moveDir.sqrMagnitude > 0.01f)
        {
            Quaternion target = Quaternion.LookRotation(moveDir);
            transform.rotation = Quaternion.RotateTowards(
                transform.rotation, target, RotationSpeed * deltaTime);
        }
    }
}