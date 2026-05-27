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
    [SerializeField] private float _moveSpeed = 5f;
    [SerializeField] private float _gravity = -20f;
    [SerializeField] private float _rotationSpeed = 720f;

    [Header("=== 回避 ===")]
    [SerializeField] private float _dodgeSpeed = 12f;
    [SerializeField] private float _dodgeDuration = 0.25f;
    [SerializeField] private float _dodgeCooldown = 1.0f;

    [Header("=== 幽霊タイマー ===")]
    [SerializeField] private float _ghostTimeLimit = 60f;

    [Header("=== カメラ ===")]
    [SerializeField] private Transform _cameraTransform;

    // ─────────────────────────────────────────
    // UnityEvents（UI・GameManager への通知）
    // ─────────────────────────────────────────

    [Header("=== 乗っ取り ===")]
    [SerializeField] private float _hijackRange = 2.5f;   // 乗っ取り可能距離
    [SerializeField] private float _behindAngle = 120f;   // 背後アークの幅（度）

    [Header("=== イベント ===")]
    public UnityEvent<float> OnGhostTimerUpdate;
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
    public HijackState Hijack { get; private set; }
    public DodgeState Dodge { get; private set; }
    public DeadState Dead { get; private set; }

    public PlayerHP PlayerHP { get; private set; }

    [Header("=== QTE UI ===")]
    [SerializeField] private HijackQTEUI hijackQTEUI;

    [Header("=== UI Manager ===")]
    [SerializeField] private UIManager uiManager;
    public UIManager UIManager => uiManager;

    [Header("=== ビジュアル ===")]
    /// <summary>幽霊キャラモデルのルート GameObject。乗っ取り中に非表示にする。</summary>
    [SerializeField] private GameObject _playerVisual;
    public GameObject PlayerVisual => _playerVisual;

    // ─────────────────────────────────────────
    // パラメーター用パブリックゲッター（backward compatibility）
    // ─────────────────────────────────────────

    public float MoveSpeed => _moveSpeed;
    public float Gravity => _gravity;
    public float RotationSpeed => _rotationSpeed;
    public float DodgeSpeed => _dodgeSpeed;
    public float DodgeDuration => _dodgeDuration;
    public float DodgeCooldown => _dodgeCooldown;
    public float GhostTimeLimit => _ghostTimeLimit;
    public float HijackRange => _hijackRange;
    public float BehindAngle => _behindAngle;

    private PlayerBaseState _currentState;

    // ─────────────────────────────────────────
    // 入力（PlayerInput に依存しないコードバインド）
    // ─────────────────────────────────────────

    private PlayerInput _pi;
    private InputAction _moveAction;
    private InputAction _hijackAction;
    private InputAction _attackAction;
    private InputAction _dodgeAction;
    private InputAction _skillAction;

    // ─────────────────────────────────────────
    // Unity ライフサイクル
    // ─────────────────────────────────────────

    private void Awake()
    {
        CC = GetComponent<CharacterController>();

        if (_cameraTransform == null && Camera.main != null)
            _cameraTransform = Camera.main.transform;

        // 状態インスタンスを生成
        Ghost   = new GhostState(this);
        Hijacked = new HijackedState(this);
        Hijack  = new HijackState(this);
        Dodge   = new DodgeState(this);
        Dead    = new DeadState(this);

        // PlayerHP コンポーネントがなければ自動追加（プレハブに未アタッチでも動く）
        PlayerHP = GetComponent<PlayerHP>() ?? gameObject.AddComponent<PlayerHP>();

        hijackQTEUI?.Initialize(Hijack);

        // InputAction を asset から直接取得（Inspector バインド不要）
        _pi           = GetComponent<PlayerInput>();
        _moveAction   = _pi.actions.FindAction("Move",   true);
        _hijackAction = _pi.actions.FindAction("Hijack", true);
        _attackAction = _pi.actions.FindAction("Attack", true);
        _dodgeAction  = _pi.actions.FindAction("Dodge",  true);
        _skillAction  = _pi.actions.FindAction("Skill");   // 存在しない場合は null
    }

    private void OnEnable()
    {
        // Move は Value 型なので performed/canceled 両方購読
        _moveAction.performed += OnMovePerformed;
        _moveAction.canceled  += OnMoveCanceled;

        // ボタン系はすべて started で即反応
        _hijackAction.started += OnHijackStarted;
        _attackAction.started += OnAttackStarted;
        _dodgeAction.started  += OnDodgeStarted;
        if (_skillAction != null) _skillAction.started += OnSkillStarted;
    }

    private void OnDisable()
    {
        _moveAction.performed -= OnMovePerformed;
        _moveAction.canceled  -= OnMoveCanceled;

        _hijackAction.started -= OnHijackStarted;
        _attackAction.started -= OnAttackStarted;
        _dodgeAction.started  -= OnDodgeStarted;
        if (_skillAction != null) _skillAction.started -= OnSkillStarted;
    }

    private void Start()
    {
        CacheCameraAxes();
        TransitionTo(Ghost);
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
    // 入力ハンドラ（コードバインド用プライベート）
    // ─────────────────────────────────────────

    private void OnMovePerformed(InputAction.CallbackContext ctx)
        => MoveInput = ctx.ReadValue<Vector2>();

    private void OnMoveCanceled(InputAction.CallbackContext ctx)
        => MoveInput = Vector2.zero;

    private void OnHijackStarted(InputAction.CallbackContext ctx)
    {
        // QTE 中は同じボタンをリング判定に転送
        if (_currentState == Hijack)
        {
            hijackQTEUI?.OnQTEPress();
            return;
        }

        // 乗っ取り中に再度押したら身体を捨てて Ghost に戻る
        if (_currentState == Hijacked)
        {
            Hijacked.DisposeBody();
            return;
        }

        if (_currentState != Ghost) return;

        bool found = Hijack.TryStart(_hijackRange, _behindAngle);
        if (!found)
            Debug.Log("[Player] 背後に乗っ取れる敵がいません");
    }

    private void OnAttackStarted(InputAction.CallbackContext ctx)
    {
        if (_currentState != Hijacked) return;
        Hijacked.TryAttack();
    }

    private void OnDodgeStarted(InputAction.CallbackContext ctx)
    {
        if (_currentState == Ghost || _currentState == Hijacked)
        {
            // HijackedState からの離脱は一時的 — モデルスワップを保持するよう通知
            if (_currentState == Hijacked)
                Hijacked.PrepareDodge();

            Dodge.SetCaller(_currentState);
            TransitionTo(Dodge);
        }
    }

    private void OnSkillStarted(InputAction.CallbackContext ctx)
    {
        if (_currentState != Hijacked) return;
        Hijacked.TrySkill();
    }

    // ─────────────────────────────────────────
    // レガシーコールバック（Inspector UnityEvent 互換用 — 中身は空）
    // PlayerInput の InvokeUnityEvents 設定が残っていても二重発火しない
    // ─────────────────────────────────────────

    public void OnMoveInput(InputAction.CallbackContext ctx) { }
    public void OnDodgeInput(InputAction.CallbackContext ctx) { }
    public void OnHijackInput(InputAction.CallbackContext ctx) { }
    public void OnAttackInput(InputAction.CallbackContext ctx) { }
    public void OnSkillInput(InputAction.CallbackContext ctx) { }

    // ─────────────────────────────────────────
    // 当たり判定
    // ─────────────────────────────────────────

    private void OnTriggerEnter(Collider other)
    {
        if (_currentState == Ghost)
        {
            if (!other.CompareTag("Enemy")) return;
            Ghost.OnHit();
        }
        else if (_currentState == Hijacked)
        {
            if (!other.CompareTag("Enemy")) return;
            EnemyController attacker = other.GetComponentInParent<EnemyController>();
            if (attacker == null || attacker == Hijacked.CurrentEnemy) return;
            PlayerHP.TakeDamage(attacker.attackPower);
        }
    }

    // ─────────────────────────────────────────
    // ユーティリティ
    // ─────────────────────────────────────────

    /// <summary>カメラ水平軸をキャッシュ（固定カメラなので Start 時のみ）</summary>
    public void CacheCameraAxes()
    {
        if (_cameraTransform == null) return;
        CamForward = Vector3.ProjectOnPlane(_cameraTransform.forward, Vector3.up).normalized;
        CamRight   = Vector3.ProjectOnPlane(_cameraTransform.right,   Vector3.up).normalized;
    }

    /// <summary>
    /// ゴーストタイマーを延長する（アイテム取得時などに呼ぶ）。
    /// GhostState 以外の状態でも安全に呼べる（Ghost に戻ったとき延長分が残る）。
    /// </summary>
    public void AddGhostTime(float seconds) => Ghost.AddTime(seconds);

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
        else VelocityY += _gravity * deltaTime;

        CC.Move((moveDir * _moveSpeed + Vector3.up * VelocityY) * deltaTime);

        if (moveDir.sqrMagnitude > 0.01f)
        {
            Quaternion target = Quaternion.LookRotation(moveDir);
            transform.rotation = Quaternion.RotateTowards(
                transform.rotation, target, _rotationSpeed * deltaTime);
        }
    }
}
