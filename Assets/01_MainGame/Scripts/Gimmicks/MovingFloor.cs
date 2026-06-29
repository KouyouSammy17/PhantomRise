// ============================================================
// MovingFloor.cs
// 移動床ギミック：2点間をピンポン往復し、上に乗ったプレイヤーを運ぶ
//
// ▼ CharacterController への搬送について
//   CC は SetParent() による親子付けでは内部コリジョンが狂うため使えない。
//   代わりに Update() で床の移動差分 (_delta) を記録し、
//   LateUpdate() — すべての Update() が終わった後 — に cc.Move(delta) を
//   一度だけ呼ぶ。isGrounded チェックは不要（トリガー在否で管理）。
//
// 使い方:
//   1. 床 GameObject にこのスクリプトをアタッチ
//   2. 床メッシュに合わせた Collider（Is Trigger: false）を設定
//   3. 子 GameObject "RiderDetector" に薄い BoxCollider（Is Trigger: true）を
//      床の上面ぴったりに配置し、MovingFloorRiderDetector をアタッチ
//   4. Inspector で _riderDetector に "RiderDetector" を割り当て
//   5. _offsetB で折り返し地点のオフセット（開始位置からの距離）を指定
// ============================================================

using UnityEngine;

[RequireComponent(typeof(Collider))]
public class MovingFloor : MonoBehaviour
{
    // ─────────────────────────────────────────
    // Inspector
    // ─────────────────────────────────────────

    [Header("=== 移動設定 ===")]
    [Tooltip("開始位置 (A点) からのオフセット。この座標が折り返し地点 (B点) になる。")]
    [SerializeField] private Vector3 _offsetB = new Vector3(5f, 0f, 0f);

    [Tooltip("移動速度 (m/s)")]
    [SerializeField] private float _speed = 2f;

    [Tooltip("端点に到達したときの停止時間（秒）。0 なら即折り返し。")]
    [SerializeField] private float _waitAtEnd = 0.5f;

    [Header("=== ライダー検出 ===")]
    [Tooltip("床上面の Is Trigger な BoxCollider を持つ子オブジェクト")]
    [SerializeField] private MovingFloorRiderDetector _riderDetector;

    [Header("=== 演出（任意）===")]
    [Tooltip("移動中に有効化するビジュアル")]
    [SerializeField] private GameObject _movingVisual;

    [Tooltip("停止中に有効化するビジュアル")]
    [SerializeField] private GameObject _idleVisual;

    // ─────────────────────────────────────────
    // 内部
    // ─────────────────────────────────────────

    private Vector3 _pointA;
    private Vector3 _pointB;
    private Vector3 _currentTarget;

    private bool  _waiting   = false;
    private float _waitTimer = 0f;

    /// <summary>今フレームの床の移動差分。LateUpdate でライダーに適用する。</summary>
    private Vector3 _delta;

    // ─────────────────────────────────────────
    // Unity ライフサイクル
    // ─────────────────────────────────────────

    private void Awake()
    {
        _pointA        = transform.position;
        _pointB        = _pointA + _offsetB;
        _currentTarget = _pointB;

        if (_riderDetector != null)
            _riderDetector.Init(this);
    }

    private void Start()
    {
        SetVisual(moving: true);
    }

    private void Update()
    {
        _delta = Vector3.zero;

        if (_waiting)
        {
            _waitTimer -= Time.deltaTime;
            if (_waitTimer <= 0f)
            {
                _waiting = false;
                SetVisual(moving: true);
            }
            return;
        }

        // ── 床を移動し、差分を記録 ──
        Vector3 prev = transform.position;
        transform.position = Vector3.MoveTowards(prev, _currentTarget, _speed * Time.deltaTime);
        _delta = transform.position - prev;

        // ── 端点到達チェック ──
        if (Vector3.Distance(transform.position, _currentTarget) < 0.001f)
        {
            transform.position = _currentTarget;
            _currentTarget = (_currentTarget == _pointB) ? _pointA : _pointB;

            if (_waitAtEnd > 0f)
            {
                _waiting   = true;
                _waitTimer = _waitAtEnd;
                SetVisual(moving: false);
            }
        }
    }

    /// <summary>
    /// すべての Update() が終わってから差分をライダーに適用する。
    /// PlayerStateMachine.Update() の後に実行されるため、
    /// 自分の移動 + 床の移動が 1 フレームで正しく合算される。
    /// </summary>
    private void LateUpdate()
    {
        if (_delta == Vector3.zero) return;
        if (_riderDetector == null) return;

        CharacterController cc = _riderDetector.RiderCC;
        if (cc == null) return;

        cc.Move(_delta);
    }

    // ─────────────────────────────────────────
    // ビジュアル切り替え
    // ─────────────────────────────────────────

    private void SetVisual(bool moving)
    {
        if (_movingVisual != null) _movingVisual.SetActive(moving);
        if (_idleVisual   != null) _idleVisual.SetActive(!moving);
    }

    // ─────────────────────────────────────────
    // エディタ Gizmo
    // ─────────────────────────────────────────

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        Vector3 a = Application.isPlaying ? _pointA : transform.position;
        Vector3 b = Application.isPlaying ? _pointB : transform.position + _offsetB;

        Gizmos.color = new Color(0f, 1f, 0.3f, 0.9f);
        Gizmos.DrawSphere(a, 0.15f);

        Gizmos.color = new Color(0.3f, 0.6f, 1f, 0.9f);
        Gizmos.DrawSphere(b, 0.15f);

        Gizmos.color = new Color(1f, 1f, 1f, 0.5f);
        Gizmos.DrawLine(a, b);
    }
#endif
}
