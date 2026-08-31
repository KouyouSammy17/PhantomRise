// ============================================================
// EnemyBuffUI.cs
// 敵の頭上に出るデバフアイコン（スピードダウン / 毒 / 出血）。
//
// アイコンは敵ごとに用意されているとは限らない
// （例: BossEnemy にはアイコンのオブジェクト自体が無い）。
// 未設定でも例外を出さず、アイコンが出ないだけにする。
//
// 同じデバフが重ねてかかった場合は、
// 前のコルーチンを止めてから測り直す。
// そうしないと先にかかった分の終了でアイコンが消えてしまう。
// ============================================================

using UnityEngine;
using System.Collections;

public class EnemyBuffUI : MonoBehaviour
{
    //アイコンの表示・非表示を制御するための参照
    [SerializeField] private GameObject SpeedDebufficon;
    [SerializeField] private GameObject poisonicon;
    [SerializeField] private GameObject bloodicon;

    // 実行中の表示コルーチン（重ねがけ時に止めるため）
    private Coroutine _speedRoutine;
    private Coroutine _poisonRoutine;
    private Coroutine _bloodRoutine;

    //スピードダウンアイコン表示
    public void ShowSpeedDebuff(float duration)
    {
        _speedRoutine = Restart(_speedRoutine, SpeedDebufficon, duration);
    }

    //ポイズンアイコン表示
    public void ShowPoisonDebuff(float duration)
    {
        _poisonRoutine = Restart(_poisonRoutine, poisonicon, duration);
    }

    //出血アイコン表示
    public void ShowBloodDebuff(float duration)
    {
        _bloodRoutine = Restart(_bloodRoutine, bloodicon, duration);
    }

    //アイコン非表示
    public void HideAll()
    {
        StopAndHide(ref _speedRoutine,  SpeedDebufficon);
        StopAndHide(ref _poisonRoutine, poisonicon);
        StopAndHide(ref _bloodRoutine,  bloodicon);
    }

    // ─────────────────────────────────────────
    // 内部
    // ─────────────────────────────────────────

    /// <summary>
    /// アイコンを duration 秒だけ出す。
    /// すでに出ていれば時間を測り直す。
    /// アイコン未設定なら何もしない（例外を出さない）。
    /// </summary>
    private Coroutine Restart(Coroutine running, GameObject icon, float duration)
    {
        if (icon == null) return null;

        if (running != null) StopCoroutine(running);

        return StartCoroutine(ShowRoutine(icon, duration));
    }

    private IEnumerator ShowRoutine(GameObject icon, float duration)
    {
        icon.SetActive(true);

        yield return new WaitForSeconds(duration);

        icon.SetActive(false);
    }

    private void StopAndHide(ref Coroutine running, GameObject icon)
    {
        if (running != null)
        {
            StopCoroutine(running);
            running = null;
        }

        if (icon != null) icon.SetActive(false);
    }
}
