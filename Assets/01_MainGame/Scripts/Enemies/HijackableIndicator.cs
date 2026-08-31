// ============================================================
// HijackableIndicator.cs
// 乗っ取り可能インジケーター
//
// 【表示条件】
//   D ランク  : プレイヤーが Ghost / HijackedState かつ
//               背後 (behindAngle) かつ射程内 (hijackRange)
//   C/B/A ランク: スタン中 (IsStunned) かつ未乗っ取り
//
// 【Setup】
//   - EnemyController と同じ GameObject にアタッチ
//   - Inspector で IndicatorRoot に浮かび上がる GameObject を指定
//     （World Space Canvas の Icon や Sprite など）
// ============================================================

using UnityEngine;

[RequireComponent(typeof(EnemyController))]
public class HijackableIndicator : MonoBehaviour
{
    [Header("=== 参照 ===")]
    [Tooltip("表示・非表示を切り替える GameObject (敵頭上に置いた Icon など)")]
    [SerializeField] private GameObject _indicatorRoot;

    [Header("=== オフセット (IndicatorRoot が null のとき自動生成) ===")]
    [SerializeField] private Vector3 _worldOffset = new Vector3(0f, 2.2f, 0f);

    // ─────────────────────────────────────────
    // 内部
    // ─────────────────────────────────────────

    private EnemyController _enemy;
    private PlayerStateMachine _player;

    /// <summary>前フレームの乗っ取り可否。SE を「なった瞬間」だけ鳴らすのに使う</summary>
    private bool _wasHijackable;

    // PlayerStateMachine が持つ設定値をキャッシュ
    private float _hijackRange;
    private float _behindAngle;

    // ─────────────────────────────────────────
    // 初期化
    // ─────────────────────────────────────────

    private void Awake()
    {
        _enemy = GetComponent<EnemyController>();
    }

    private void Start()
    {
        // シーン内の PlayerStateMachine を検索
        _player = FindFirstObjectByType<PlayerStateMachine>();

        if (_player != null)
        {
            _hijackRange = _player.HijackRange;
            _behindAngle = _player.BehindAngle;
        }
        else
        {
            Debug.LogWarning($"[HijackableIndicator] {name}: PlayerStateMachine が見つかりません。");
        }

        // インジケーター GameObject が未設定なら非表示のまま
        if (_indicatorRoot != null)
            _indicatorRoot.SetActive(false);
    }

    // ─────────────────────────────────────────
    // 毎フレーム判定
    // ─────────────────────────────────────────

    private void Update()
    {
        if (_indicatorRoot == null || _player == null) return;

        bool show = EvaluateHijackable();

        // 乗っ取り可能になった「瞬間」だけ鳴らす。
        // Update は毎フレーム走るので、状態が変わったときだけにしないと鳴り続ける。
        if (show && !_wasHijackable)
            _enemy.GetComponent<EnemyAudio>()?.PlayHijackableSE();

        _wasHijackable = show;

        _indicatorRoot.SetActive(show);
    }

    // ─────────────────────────────────────────
    // 乗っ取り可否チェック
    // ─────────────────────────────────────────

    private bool EvaluateHijackable()
    {
        // 既に乗っ取り済み or 死亡 → 非表示
        if (_enemy.IsHijacked) return false;

        // プレイヤーが Ghost か HijackedState のときだけ判定
        // （それ以外の状態では乗っ取りアクション自体が無効）
        string stateName = _player.CurrentStateName;
        bool playerCanHijack = stateName == nameof(GhostState)
                            || stateName == nameof(HijackedState);
        if (!playerCanHijack) return false;

        if (_enemy.Rank == EnemyController.EnemyRank.D)
        {
            return IsPlayerBehindAndInRange();
        }
        else
        {
            // C / B / A ランク: スタン中のみ
            return _enemy.IsStunned;
        }
    }

    // ─────────────────────────────────────────
    // D ランク: 背後 + 射程チェック
    // HijackState.FindTargetBehind と同じロジック
    // ─────────────────────────────────────────

    private bool IsPlayerBehindAndInRange()
    {
        float dist = Vector3.Distance(_player.transform.position, transform.position);
        if (dist > _hijackRange) return false;

        // 敵→プレイヤーのベクトルと敵の前方とのなす角
        Vector3 enemyToPlayer = (_player.transform.position - transform.position).normalized;
        float angle = Vector3.Angle(transform.forward, enemyToPlayer);

        // angle >= (180 - behindAngle/2) なら背後ゾーン
        float threshold = 180f - _behindAngle * 0.5f;
        return angle >= threshold;
    }

    // ─────────────────────────────────────────
    // Gizmo（Editor での確認用）
    // ─────────────────────────────────────────

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (_enemy == null) _enemy = GetComponent<EnemyController>();

        // 射程円
        Gizmos.color = new Color(0f, 1f, 0.5f, 0.25f);
        if (_player != null)
            Gizmos.DrawWireSphere(transform.position, _player.HijackRange);

        // インジケーター位置
        Gizmos.color = Color.yellow;
        Gizmos.DrawSphere(transform.position + _worldOffset, 0.08f);
    }
#endif
}
