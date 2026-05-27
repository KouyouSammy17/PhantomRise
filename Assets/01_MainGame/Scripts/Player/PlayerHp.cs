// ============================================================
// PlayerHP.cs
// 乗っ取り中のプレイヤー HP を管理する
// PlayerStateMachine と同じ GameObject にアタッチする
// ============================================================

using UnityEngine;
using UnityEngine.Events;

public class PlayerHP : MonoBehaviour
{
    [Header("=== イベント ===")]
    [SerializeField] private UnityEvent<int, int> _onHPChanged;  // (currentHP, maxHP)
    [SerializeField] private UnityEvent _onDead;

    // Public accessors for external event subscriptions
    public UnityEvent<int, int> OnHPChanged => _onHPChanged;
    public UnityEvent OnDead => _onDead;

    public int CurrentHP { get; private set; }
    public int MaxHP { get; private set; }

    private PlayerStateMachine _machine;

    private void Awake()
    {
        _machine = GetComponent<PlayerStateMachine>();
    }

    /// <summary>乗っ取り成功時に HijackState から呼ぶ</summary>
    public void Initialize(int maxHP, int currentHP)
    {
        MaxHP = maxHP;
        CurrentHP = currentHP;
        _onHPChanged?.Invoke(CurrentHP, MaxHP);
        Debug.Log($"[PlayerHP] 初期化 {CurrentHP}/{MaxHP}");
    }

    /// <summary>
    /// HP を回復する（アイテム取得時などに呼ぶ）。
    /// 乗っ取り中のみ有効。MaxHP を超えない。
    /// </summary>
    public void Heal(int amount)
    {
        if (_machine.CurrentStateName != nameof(HijackedState)) return;

        CurrentHP = Mathf.Min(MaxHP, CurrentHP + amount);
        _onHPChanged?.Invoke(CurrentHP, MaxHP);
        Debug.Log($"[PlayerHP] +{amount} 回復 → {CurrentHP}/{MaxHP}");
    }

    /// <summary>乗っ取り状態で敵の攻撃を受けたとき呼ぶ</summary>
    public void TakeDamage(int damage)
    {
        if (_machine.CurrentStateName != nameof(HijackedState)) return;

        CurrentHP = Mathf.Max(0, CurrentHP - damage);
        _onHPChanged?.Invoke(CurrentHP, MaxHP);
        Debug.Log($"[PlayerHP] -{damage} → {CurrentHP}/{MaxHP}");

        if (CurrentHP <= 0)
        {
            _onDead?.Invoke();
            _machine.Hijacked.OnHPZero();
        }
    }
}