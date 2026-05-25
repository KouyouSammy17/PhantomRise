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
    public UnityEvent<int, int> OnHPChanged;  // (currentHP, maxHP)
    public UnityEvent OnDead;

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
        OnHPChanged?.Invoke(CurrentHP, MaxHP);
        Debug.Log($"[PlayerHP] 初期化 {CurrentHP}/{MaxHP}");
    }

    /// <summary>乗っ取り状態で敵の攻撃を受けたとき呼ぶ</summary>
    public void TakeDamage(int damage)
    {
        if (_machine.CurrentStateName != nameof(HijackedState)) return;

        CurrentHP = Mathf.Max(0, CurrentHP - damage);
        OnHPChanged?.Invoke(CurrentHP, MaxHP);
        Debug.Log($"[PlayerHP] -{damage} → {CurrentHP}/{MaxHP}");

        if (CurrentHP <= 0)
        {
            OnDead?.Invoke();
            _machine.Hijacked.OnHPZero();
        }
    }
}