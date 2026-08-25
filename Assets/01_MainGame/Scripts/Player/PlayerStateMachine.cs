// ============================================================
// PlayerStateMachine.cs
// 状態の登録・切り替えを管理する
// PlayerController にアタッチして使う
// ============================================================

using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using System.Collections;

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
    private float _speedMultiplier = 1f;
    private CancellationTokenSource _slowCts;

    // ─── スタン ───
    private bool _isStunned = false;
    private CancellationTokenSource _stunCts;
    /// <summary>スタン中は true。移動・アクション入力をすべてブロックする。</summary>
    public bool IsStunned => _isStunned;


    [Header("=== 回避 ===")]
    [SerializeField] private float _dodgeSpeed = 12f;
    [SerializeField] private float _dodgeDuration = 0.25f;
    [SerializeField] private float _dodgeCooldown = 1.0f;

    [Header("=== 幽霊タイマー ===")]
    [SerializeField] private float _ghostTimeLimit = 60f;

    [Header("=== 落下死 ===")]
    [Tooltip("この Y 座標を下回ったらゲームオーバー（ステージの床より十分低い値に設定）")]
    [SerializeField] private float _fallDeathY = -10f;

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

    /// <summary>
    /// HijackedState 中、または HijackedState から一時離脱した Dodge 中のとき true。
    /// 乗っ取り UI（HP バー・スキルアイコン）の表示判定に使う。
    /// </summary>
    public bool IsEffectivelyHijacked =>
        _currentState == Hijacked ||
        (_currentState == Dodge && Dodge.IsReturningToHijacked);

    // ─────────────────────────────────────────
    // シャドウゾーン
    // ─────────────────────────────────────────

    /// <summary>
    /// プレイヤーがシャドウゾーン内にいるか。
    /// ShadowZone コンポーネントが OnTriggerEnter/Exit で設定する。
    /// </summary>
    public bool IsInShadowZone { get; set; }

    // ─────────────────────────────────────────
    // 状態インスタンス
    // ─────────────────────────────────────────

    public GhostState Ghost { get; private set; }
    public HijackedState Hijacked { get; private set; }
    public HijackState Hijack { get; private set; }
    public DodgeState Dodge { get; private set; }
    public DeadState Dead { get; private set; }

    public PlayerHP PlayerHP { get; private set; }

    /// <summary>幽霊モデルのアニメーション制御（CrossFade 方式）</summary>
    public GhostAnimation GhostAnim { get; private set; }

    /// <summary>幽霊モデルのディゾルブ演出（シェーダーの _Dissolve を駆動）</summary>
    public GhostDissolveEffect DissolveFx { get; private set; }

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
    private InputAction _disposeAction;

    // ─────────────────────────────────────────
    // プレイヤーのバフUI表示
    // ─────────────────────────────────────────

    private Coroutine demonBuffCoroutine;
    private Coroutine specterBuffCoroutine;



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

        // GhostAnimation も同様に自動追加
        GhostAnim = GetComponent<GhostAnimation>() ?? gameObject.AddComponent<GhostAnimation>();

        // ディゾルブ演出も自動追加
        DissolveFx = GetComponent<GhostDissolveEffect>() ?? gameObject.AddComponent<GhostDissolveEffect>();

        hijackQTEUI?.Initialize(Hijack);

        // InputAction を asset から直接取得（Inspector バインド不要）
        _pi           = GetComponent<PlayerInput>();
        _moveAction   = _pi.actions.FindAction("Move",   true);
        _hijackAction = _pi.actions.FindAction("Hijack", true);
        _attackAction = _pi.actions.FindAction("Attack", true);
        _dodgeAction  = _pi.actions.FindAction("Dodge",  true);
        _skillAction   = _pi.actions.FindAction("Skill");        // 存在しない場合は null
        _disposeAction = _pi.actions.FindAction("Dispose");   // 存在しない場合は null
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
        if (_skillAction   != null) _skillAction.started   += OnSkillStarted;
        if (_disposeAction != null) _disposeAction.started += OnDisposeStarted;
    }

    private void OnDisable()
    {
        _moveAction.performed -= OnMovePerformed;
        _moveAction.canceled  -= OnMoveCanceled;

        _hijackAction.started -= OnHijackStarted;
        _attackAction.started -= OnAttackStarted;
        _dodgeAction.started  -= OnDodgeStarted;
        if (_skillAction   != null) _skillAction.started   -= OnSkillStarted;
        if (_disposeAction != null) _disposeAction.started -= OnDisposeStarted;
    }

    private void Start()
    {
        CacheCameraAxes();
        TransitionTo(Ghost);
    }

    private void Update()
    {
        _currentState?.Update(Time.deltaTime);

        // 落下死：Dead 以外の状態でステージ外まで落ちたらゲームオーバー
        if (CurrentStateName != nameof(DeadState)
            && transform.position.y < _fallDeathY)
        {
            Debug.Log("[Player] ステージ外に落下 → ゲームオーバー");
            TransitionTo(Dead);
        }
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
        if (_isStunned) return;
        // QTE 中は同じボタンをリング判定に転送
        if (_currentState == Hijack)
        {
            hijackQTEUI?.OnQTEPress();
            return;
        }

        // 乗っ取り中に再度押したら背後の敵に即転送を試みる（身体を捨てるのは Q ボタン）
        if (_currentState == Hijacked)
        {
            Hijack.TryTransfer(_hijackRange, _behindAngle);
            return;
        }

        if (_currentState != Ghost) return;

        bool found = Hijack.TryStart(_hijackRange, _behindAngle);
        if (!found)
            Debug.Log("[Player] 背後に乗っ取れる敵がいません");
    }

    private void OnAttackStarted(InputAction.CallbackContext ctx)
    {
        if (_isStunned) return;
        if (_currentState != Hijacked) return;
        Hijacked.TryAttack();
    }

    private void OnDodgeStarted(InputAction.CallbackContext ctx)
    {
        if (_isStunned) return;
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
        if (_isStunned) return;
        if (_currentState != Hijacked) return;
        Hijacked.TrySkill();
    }

    private void OnDisposeStarted(InputAction.CallbackContext ctx)
    {
        if (_isStunned) return;
        // 乗っ取り中のみ有効 — Q ボタンで身体を捨てて Ghost に戻る
        if (_currentState != Hijacked) return;
        Hijacked.DisposeBody();
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
            PlayerHP.TakeDamage(attacker.AttackPower);
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
        // スタン中は移動しない（重力だけ適用）
        if (_isStunned)
        {
            if (CC.isGrounded) VelocityY = -2f;
            else VelocityY += _gravity * deltaTime;
            CC.Move(Vector3.up * VelocityY * deltaTime);
            return;
        }

        Vector3 moveDir = GetMoveDirection();

        if (CC.isGrounded) VelocityY = -2f;
        else VelocityY += _gravity * deltaTime;

        CC.Move((moveDir * (_moveSpeed * _speedMultiplier) + Vector3.up * VelocityY)* deltaTime);

        if (moveDir.sqrMagnitude > 0.01f)
        {
            Quaternion target = Quaternion.LookRotation(moveDir);
            transform.rotation = Quaternion.RotateTowards(
                transform.rotation, target, _rotationSpeed * deltaTime);
        }

        // アニメーション用
        if (CurrentStateName == nameof(GhostState))
        {
            GhostAnim?.SetMove(MoveInput.sqrMagnitude > 0.01f);
            //幽霊の時は敵のランクを消す
            FindAnyObjectByType<EnemyRankUI>()?.HideRank();
        }
        else if (CurrentStateName == nameof(HijackedState))
        {
            EnemyController enemy = Hijacked.CurrentEnemy;

            if (enemy != null)
            {
                EnemyAnimation anim =
                    enemy.GetComponent<EnemyAnimation>();

                if (anim != null)
                {
                    bool moving = MoveInput.sqrMagnitude > 0.01f;
                    anim.SetMove(moving);
                }
            }
        }
    }

    // ─────────────────────────────────────────
    // スタン（UniTask）
    // ─────────────────────────────────────────

    /// <summary>
    /// プレイヤーを一時的にスタンさせる。移動・全アクション入力をブロックする。
    /// すでにスタン中なら残り時間を上書きする。
    /// </summary>
    public void ApplyStun(float duration)
    {
        _stunCts?.Cancel();
        _stunCts?.Dispose();
        _stunCts = CancellationTokenSource.CreateLinkedTokenSource(
            this.GetCancellationTokenOnDestroy());

        StunAsync(duration, _stunCts.Token).Forget();
    }

    private async UniTaskVoid StunAsync(float duration, CancellationToken ct)
    {
        try
        {
            _isStunned = true;
            Debug.Log($"[Player] スタン {duration}秒");

            await UniTask.Delay(
                TimeSpan.FromSeconds(duration),
                cancellationToken: ct);

            _isStunned = false;
            Debug.Log("[Player] スタン解除");
        }
        catch (OperationCanceledException)
        {
            // 上書きキャンセル時 — 次の ApplyStun が _isStunned を再設定する
        }
        finally
        {
            _stunCts?.Dispose();
            _stunCts = null;
        }
    }

    // ─────────────────────────────────────────
    // スロウ（UniTask）
    // ─────────────────────────────────────────

    /// <summary>
    /// 移動速度を一時的に低下させる。
    /// すでにスロウ中なら古い効果をキャンセルして新しい値で上書きする。
    /// </summary>
    /// <param name="slowPercent"> 低下割合（例: 0.5 = 50% ダウン）</param>
    /// <param name="duration">    効果時間（秒）</param>
    public void ApplySlow(float slowPercent, float duration)
    {
        BuffUIController.Instance.ShowBuff(BuffType.SpeedDeBuff);

        // 既存のスロウをキャンセルして上書き（コルーチン版の StopCoroutine 相当）
        _slowCts?.Cancel();
        _slowCts?.Dispose();
        _slowCts = CancellationTokenSource.CreateLinkedTokenSource(
            this.GetCancellationTokenOnDestroy());

        SlowAsync(slowPercent, duration, _slowCts.Token).Forget();
    }

    private async UniTaskVoid SlowAsync(
        float slowPercent,
        float duration,
        CancellationToken ct)
    {
        try
        {
            _speedMultiplier = 1f - slowPercent;
            Debug.Log($"[Player] 移動速度 {slowPercent * 100f}% ダウン");

            await UniTask.Delay(
                TimeSpan.FromSeconds(duration),
                cancellationToken: ct);

            _speedMultiplier = 1f;
            Debug.Log("[Player] 移動速度回復");
        }
        catch (OperationCanceledException)
        {
            // 上書きキャンセル時はここに来る。
            // _speedMultiplier は次の ApplySlow が即座に上書きするので触らない。
        }
        finally
        {
            BuffUIController.Instance.HideBuff(BuffType.SpeedDeBuff);
            _slowCts?.Dispose();
            _slowCts = null;
        }
    }


    //スピード倍率の設定（自分が移動するときの速さ）
    public void SetPMoveSpeedMultiplier(float multiplier)
    {
        _speedMultiplier= multiplier;
    }

    // ─────────────────────────────────────────
    // デーモンのバフ（コルーチン）
    //  ─────────────────────────────────────────


    public void StartDemonBuff(float duration)
    {
        if (demonBuffCoroutine != null)
            StopCoroutine(demonBuffCoroutine);

        demonBuffCoroutine = StartCoroutine(DemonBuffRoutine(duration));
    }


    private IEnumerator DemonBuffRoutine(float duration)
    {
        BuffUIController.Instance.ShowBuff(BuffType.DemonBuff);

        yield return new WaitForSeconds(duration);

        BuffUIController.Instance.HideBuff(BuffType.DemonBuff);

        demonBuffCoroutine = null;
    }

    public void StopDemonBuff()
    {
        if (demonBuffCoroutine != null)
        {
            StopCoroutine(demonBuffCoroutine);
            demonBuffCoroutine = null;
        }

        // UI解除
        BuffUIController.Instance.HideBuff(BuffType.DemonBuff);

        // デーモンバフ効果解除
        SetPMoveSpeedMultiplier(1f);

        Debug.Log("デーモンバフ解除");
    }

    // ─────────────────────────────────────────
    // スペクターのバフ（コルーチン）
    //  ─────────────────────────────────────────

    public void StartSpecterBuff(float duration)
    {
        if (specterBuffCoroutine != null)
            StopCoroutine(specterBuffCoroutine);

        specterBuffCoroutine = StartCoroutine(SpecterBuffRoutine(duration));
    }


    private IEnumerator SpecterBuffRoutine(float duration)
    {
        BuffUIController.Instance.ShowBuff(BuffType.SpecterBuff);

        yield return new WaitForSeconds(duration);

        BuffUIController.Instance.HideBuff(BuffType.SpecterBuff);

        specterBuffCoroutine = null;
    }

    public void StopSpecterBuff()
    {
        if (specterBuffCoroutine != null)
        {
            StopCoroutine(specterBuffCoroutine);
            specterBuffCoroutine = null;
        }

        // UI解除
        BuffUIController.Instance.HideBuff(BuffType.SpecterBuff);

        // スペクターバフ効果解除
        SetPMoveSpeedMultiplier(1f);

        Debug.Log("スペクターバフ解除");
    }

    //ボス演出の時にプレイヤーの操作を止める
    public void StopMode()
    {
        _isStunned = true;
    }

    public void ResumeMode()
    {
        _isStunned = false;
    }

}
