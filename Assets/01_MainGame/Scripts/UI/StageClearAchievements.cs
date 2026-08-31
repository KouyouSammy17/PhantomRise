// ============================================================
// StageClearAchievements.cs
// ゲームクリア画面のミッション達成表示と星の演出。
//
// ミッションは 3 つ（Play_Result の Toggle_Mission 3 つに対応）:
//   ① クリアした            … クリア画面が出た時点で必ず達成
//   ② 制限時間内にクリア    … StageStats.ElapsedTime <= timeLimit
//   ③ 一度も死なずにクリア  … StageStats.Deaths == 0
//
// 星は「達成したミッションの数」だけ順番に光る。
//
// Play_Result の Star まわりの構成:
//   Star（入れ物）
//     ├ Fx_Shines_Glow03      … 全体の光。1 つでも取れていたら出す
//     ├ Fx_Spread_Star03 × 3  … 星ごとの発光。取れた星だけ出す
//     └ Star / Star shadow × 3 … 表と影。どちらか一方だけを出す
//
// 取れていない星は Star を消して Star shadow を出す（＝抜け殻表示）。
//
// UIManager.ShowGameClear() から Show() を呼ぶ。
//
// 実装メモ:
//   ・クリア画面は Time.timeScale = 0 で出るので Tween は SetUpdate(true)
//   ・トグルと星はプレハブが差し替わっても壊れないよう名前で探す
//   ・トグルは表示専用。interactable を切らないと
//     パッドのカーソルがトグルに吸われてボタンを押せなくなる
// ============================================================

using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class StageClearAchievements : MonoBehaviour
{
    // Play_Result 内のオブジェクト名
    private const string ToggleName = "Toggle_Mission";
    private const string StarName   = "Star";
    private const string ShadowName = "Star shadow";
    private const string FxStarName = "Fx_Spread_Star";     // 星ごとの発光
    private const string FxGlowName = "Fx_Shines_Glow";     // 全体の光

    /// <summary>星 1 個ぶんの表示セット</summary>
    private class StarSlot
    {
        public Transform Star;
        public GameObject Shadow;
        public GameObject Fx;
        public Vector3 BaseScale = Vector3.one;
    }

    // ─────────────────────────────────────────
    // Inspector
    // ─────────────────────────────────────────

    [Header("=== 対象パネル（未割り当てなら UIManager から渡される）===")]
    [SerializeField] private GameObject clearPanel;

    [Header("=== ミッション② 制限時間 ===")]
    [Tooltip("この秒数以内にクリアすれば達成（ステージ滞在の合計時間で判定）")]
    [SerializeField] private float timeLimit = 180f;

    [Header("=== 星の演出 ===")]
    [SerializeField] private float starDelay      = 0.18f;   // 1 個ごとの間
    [SerializeField] private float starDuration   = 0.42f;
    [SerializeField] private float starStartScale = 0.2f;

    [Tooltip("最初の星が光り始めるまでの待ち")]
    [SerializeField] private float starStartDelay = 0.35f;

    // ─────────────────────────────────────────
    // 内部
    // ─────────────────────────────────────────

    private Toggle[] _toggles;
    private StarSlot[] _slots;
    private GameObject _glowFx;
    private Sequence _starSequence;

    private void OnDestroy() => KillStarTween();

    // ─────────────────────────────────────────
    // 表示
    // ─────────────────────────────────────────

    /// <summary>クリア画面が開いたときに UIManager から呼ぶ。</summary>
    public void Show(GameObject panel = null)
    {
        if (panel != null) clearPanel = panel;
        if (clearPanel == null) return;

        Resolve();

        bool[] results = Evaluate();

        ApplyToggles(results);
        PlayStars(results.Count(r => r));
    }

    /// <summary>3 つのミッションの達成状況。</summary>
    private bool[] Evaluate()
    {
        bool cleared = true;                                  // ① ここに来た＝クリア
        bool inTime  = StageStats.ElapsedTime <= timeLimit;    // ② 制限時間内
        bool noDeath = StageStats.Deaths == 0;                 // ③ ノーデス

        Debug.Log($"[Achievements] クリア={cleared} " +
                  $"タイム={StageStats.ElapsedTime:F1}s/{timeLimit:F0}s({inTime}) " +
                  $"死亡={StageStats.Deaths}({noDeath})");

        return new[] { cleared, inTime, noDeath };
    }

    private void ApplyToggles(bool[] results)
    {
        if (_toggles == null) return;

        for (int i = 0; i < _toggles.Length; i++)
        {
            if (_toggles[i] == null) continue;

            bool done = i < results.Length && results[i];

            // onValueChanged を走らせないよう WithoutNotify を使う
            _toggles[i].SetIsOnWithoutNotify(done);

            // 表示専用にする（パッドの選択対象から外す）
            _toggles[i].interactable = false;
        }
    }

    // ─────────────────────────────────────────
    // 星の演出
    // ─────────────────────────────────────────

    private void PlayStars(int earned)
    {
        if (_slots == null || _slots.Length == 0) return;

        KillStarTween();

        // まず全部「取れていない」見た目にする
        for (int i = 0; i < _slots.Length; i++)
            SetSlot(_slots[i], false);

        // 全体の光は 1 個でも取れていたら出す
        if (_glowFx != null) _glowFx.SetActive(earned > 0);

        _starSequence = DOTween.Sequence().SetUpdate(true);
        _starSequence.AppendInterval(starStartDelay);

        for (int i = 0; i < _slots.Length && i < earned; i++)
        {
            StarSlot slot = _slots[i];
            if (slot?.Star == null) continue;

            // 出現前は縮めておく
            slot.Star.localScale = slot.BaseScale * starStartScale;

            _starSequence.AppendCallback(() => SetSlot(slot, true));
            _starSequence.Append(slot.Star.DOScale(slot.BaseScale, starDuration)
                                          .SetEase(Ease.OutBack)
                                          .SetUpdate(true));
            _starSequence.AppendInterval(starDelay);
        }
    }

    /// <summary>
    /// 星 1 個の表示を切り替える。
    /// 取れていれば 星＋発光、取れていなければ 影だけ。
    /// </summary>
    private static void SetSlot(StarSlot slot, bool earned)
    {
        if (slot == null) return;

        if (slot.Star != null)   slot.Star.gameObject.SetActive(earned);
        if (slot.Fx != null)     slot.Fx.SetActive(earned);
        if (slot.Shadow != null) slot.Shadow.SetActive(!earned);
    }

    private void KillStarTween()
    {
        _starSequence?.Kill();
        _starSequence = null;

        if (_slots == null) return;

        foreach (StarSlot slot in _slots)
            if (slot?.Star != null) DOTween.Kill(slot.Star);
    }

    // ─────────────────────────────────────────
    // 名前で解決
    // ─────────────────────────────────────────

    private void Resolve()
    {
        if (_toggles != null && _slots != null) return;

        Transform[] all = clearPanel.GetComponentsInChildren<Transform>(true);

        // Toggle_Mission / Toggle_Mission (1) / Toggle_Mission (2)
        // 名前順だと (1)(2) が先に来るので、並び順は画面の上から下にそろえる
        _toggles = all
            .Where(t => t.name.StartsWith(ToggleName))
            .Select(t => t.GetComponent<Toggle>())
            .Where(t => t != null)
            .OrderByDescending(t => t.transform.position.y)
            .ToArray();

        ResolveStars(all);

        if (_toggles.Length == 0) Debug.LogWarning($"[Achievements] '{ToggleName}' が見つかりません。");
        if (_slots.Length   == 0) Debug.LogWarning($"[Achievements] '{StarName}' が見つかりません。");
    }

    private void ResolveStars(Transform[] all)
    {
        // 入れ物の "Star"（"Star" という子を持つ方）を探す
        Transform container = all.FirstOrDefault(
            t => t.name == StarName && HasChildNamed(t, StarName));

        if (container == null)
        {
            _slots = new StarSlot[0];
            return;
        }

        // 子を順番に見て「Star の直後に来る Star shadow」を組にする。
        // 星と影は同じ位置に重なっているので、座標では対にできない。
        List<StarSlot> slots = new List<StarSlot>();
        List<Transform> starFx = new List<Transform>();
        StarSlot current = null;

        foreach (Transform child in container)
        {
            if (child.name == StarName)
            {
                current = new StarSlot { Star = child, BaseScale = child.localScale };
                slots.Add(current);
            }
            else if (child.name == ShadowName)
            {
                if (current != null) current.Shadow = child.gameObject;
            }
            else if (child.name.StartsWith(FxStarName))
            {
                starFx.Add(child);
            }
            else if (child.name.StartsWith(FxGlowName))
            {
                _glowFx = child.gameObject;
            }
        }

        // 左から右にそろえる
        slots = slots.OrderBy(s => s.Star.position.x).ToList();

        // 発光は星と別オブジェクトなので、いちばん近いものを 1 個ずつ割り当てる
        foreach (StarSlot slot in slots)
        {
            Transform nearest = starFx
                .OrderBy(f => Mathf.Abs(f.position.x - slot.Star.position.x))
                .FirstOrDefault();

            if (nearest == null) continue;

            slot.Fx = nearest.gameObject;
            starFx.Remove(nearest);          // 同じ発光を 2 つの星で使わない
        }

        _slots = slots.ToArray();
    }

    private static bool HasChildNamed(Transform parent, string name)
    {
        foreach (Transform child in parent)
            if (child.name == name) return true;

        return false;
    }
}
