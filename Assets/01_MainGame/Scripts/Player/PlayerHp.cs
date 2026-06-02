// ============================================================
// PlayerHP.cs
// 乗っ取り中のプレイヤー HP を管理する
// PlayerStateMachine と同じ GameObject にアタッチする
// ============================================================

using System;
using System.Threading;
using Cysharp.Threading.Tasks;
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

    // 毒 DoT タスクのキャンセルソース（null = 毒なし）
    private CancellationTokenSource _poisonCts;

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

    // ─────────────────────────────────────────
    // 毒 DoT（UniTask）
    // ─────────────────────────────────────────

    /// <summary>
    /// 毒ダメージを開始する。すでに毒状態なら重複しない。
    /// </summary>
    /// <param name="duration"> 毒の持続時間（秒）</param>
    /// <param name="interval"> ダメージを与える間隔（秒）</param>
    /// <param name="percent">  1 ティックあたり現在 HP に対する割合（例: 0.1 = 10%）</param>
    public void ApplyPoison(float duration, float interval, float percent)
    {
        if (_poisonCts != null) return;  // 毒は重複しない

        // GameObject が Destroy されたときも自動キャンセルするようリンクする
        _poisonCts = CancellationTokenSource.CreateLinkedTokenSource(
            this.GetCancellationTokenOnDestroy());

        PoisonAsync(duration, interval, percent, _poisonCts.Token).Forget();
    }

    /// <summary>
    /// 毒を強制解除する（解毒アイテムなどに使用）。
    /// </summary>
    public void CancelPoison()
    {
        _poisonCts?.Cancel();
    }

    private async UniTaskVoid PoisonAsync(
        float duration,
        float interval,
        float percent,
        CancellationToken ct)
    {
        try
        {
            float timer = 0f;
            while (timer < duration)
            {
                await UniTask.Delay(
                    TimeSpan.FromSeconds(interval),
                    cancellationToken: ct);

                int poisonDamage = Mathf.Max(1, Mathf.CeilToInt(CurrentHP * percent));
                TakeDamage(poisonDamage);
                Debug.Log($"[PlayerHP] 毒ダメージ {poisonDamage}");
                timer += interval;
            }
        }
        catch (OperationCanceledException)
        {
            Debug.Log("[PlayerHP] 毒キャンセル");
        }
        finally
        {
            // 正常終了・キャンセルどちらでも必ずクリーンアップ
            _poisonCts?.Dispose();
            _poisonCts = null;
        }
    }
}