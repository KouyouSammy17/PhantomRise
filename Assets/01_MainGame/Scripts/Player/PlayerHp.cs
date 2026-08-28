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
using System.Collections;

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
    
    // 出血 DoT タスクのキャンセルソース（null = 出血なし）
    private CancellationTokenSource _bleedCts;



    //回復エフェクト
    [SerializeField] private ParticleSystem healEffect;

    //ダメージ音
    [SerializeField] private AudioClip damageSound;
    [SerializeField] private AudioSource damageAudioSource;


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
        StartCoroutine(HealEfects());
        Debug.Log($"[PlayerHP] +{amount} 回復 → {CurrentHP}/{MaxHP}");
    }

    //回復エフェクトを再生するためのメソッド
    public virtual IEnumerator HealEfects()
    {
        healEffect.Play();
        yield return new WaitForSeconds(2f);
        healEffect.Stop();
    }

    /// <summary>乗っ取り状態で敵の攻撃を受けたとき呼ぶ</summary>
    public void TakeDamage(int damage)
    {
        if (_machine.CurrentStateName != nameof(HijackedState)) return;

        CurrentHP = Mathf.Max(0, CurrentHP - damage);
        _onHPChanged?.Invoke(CurrentHP, MaxHP);
        Debug.Log($"[PlayerHP] -{damage} → {CurrentHP}/{MaxHP}");
        // ダメージ音を再生
        if (damageSound != null)
        {
            damageAudioSource.PlayOneShot(damageSound);
        }

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
        BuffUIController.Instance.ShowBuff(BuffType.PoisonDeBuff);
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
        BuffUIController.Instance.HideBuff(BuffType.PoisonDeBuff);
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
            BuffUIController.Instance.HideBuff(BuffType.PoisonDeBuff);
            _poisonCts?.Dispose();
            _poisonCts = null;
        }
    }

    // ─────────────────────────────────────────
    // 出血 DoT（UniTask）
    // ─────────────────────────────────────────

    /// <summary>
    /// 出血ダメージを開始する（重複しない）
    /// </summary>
    /// <param name="duration">継続時間</param>
    /// <param name="interval">ダメージ間隔</param>
    /// <param name="damagePerTick">1回あたりの固定ダメージ</param>
    public void ApplyBleed(
        float duration,
        float interval = 1f,
        int damagePerTick = 7)
    {

        BuffUIController.Instance.ShowBuff(BuffType.BleedingDeBuff);

        // すでに出血中なら無視
        if (_bleedCts != null)
            return;

        _bleedCts =
            CancellationTokenSource.CreateLinkedTokenSource(
                this.GetCancellationTokenOnDestroy());

        BleedAsync(
            duration,
            interval,
            damagePerTick,
            _bleedCts.Token).Forget();
    }


    /// <summary>
    /// 敵が攻撃してきて出血状態になったときに、
    /// 別の敵に乗り移ったときそのまま出血ダメージを食らったままなのを後で直す。
    public void CancelBleed()
    {
        BuffUIController.Instance.HideBuff(BuffType.BleedingDeBuff);
        _bleedCts?.Cancel();
    }

    private async UniTaskVoid BleedAsync(
    float duration,
    float interval,
    int damagePerTick,
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

                TakeDamage(damagePerTick);

                Debug.Log(
                    $"[PlayerHP] 出血ダメージ {damagePerTick}");

                timer += interval;
            }
        }
        catch (OperationCanceledException)
        {
            Debug.Log("[PlayerHP] 出血キャンセル");
        }
        finally
        {

            BuffUIController.Instance.HideBuff(BuffType.BleedingDeBuff);
            _bleedCts?.Dispose();
            _bleedCts = null;
        }
    }

}