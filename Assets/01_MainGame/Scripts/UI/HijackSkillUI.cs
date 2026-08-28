// ============================================================
// HijackSkillUI.cs  (UniTask + DOTween 版)
// 乗っ取った瞬間に「そのモンスターのスキル」を一度だけ紹介するカード。
//
// ・同じ SkillID を 2 回目以降に乗っ取ったときは表示しない
//   （初見だけ説明 → 慣れたらテンポを邪魔しない）
// ・アイコン / スキル名 / 説明を EnemySkillBase から取得
// ・showDuration 秒で自動的に閉じる
//
// 【Setup】
//   UI Manager
//    └ HijackSkillPanel        ← panel（CanvasGroup を付けると綺麗にフェードする）
//        ├ Icon   (Image)      ← skillIconImage
//        ├ Name   (TMP_Text)   ← skillNameText
//        └ Desc   (TMP_Text)   ← descriptionText
//
//   乗っ取り完了時に ShowSkill(skill) を呼ぶ。
//
// 実装メモ:
//   ・待ち時間は UniTask.Delay（UnscaledDeltaTime）。
//     連続で乗っ取っても CancellationTokenSource で前の待ちを確実に打ち切る。
//   ・GetCancellationTokenOnDestroy と Link しているので、
//     破棄後に UI を触って例外、という事故が起きない。
//   ・出入りの動きは DOTween。
// ============================================================

using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HijackSkillUI : MonoBehaviour
{
    [Header("=== UI ===")]
    [SerializeField] private GameObject panel;

    /// <summary>パネルの CanvasGroup（無ければフェード無しで動く）</summary>
    [SerializeField] private CanvasGroup panelGroup;

    /// <summary>スキルアイコン（EnemySkillBase.SkillIcon が入る）</summary>
    [SerializeField] private Image skillIconImage;

    [SerializeField] private TMP_Text skillNameText;
    [SerializeField] private TMP_Text descriptionText;

    [Header("=== 表示設定 ===")]
    [Tooltip("カードを出しておく秒数")]
    [SerializeField] private float showDuration = 3f;

    [SerializeField] private float fadeInDuration = 0.22f;
    [SerializeField] private float fadeOutDuration = 0.25f;

    /// <summary>SkillIcon 未設定の敵に使うフォールバック（Skill_Hijack など）</summary>
    [SerializeField] private Sprite fallbackIcon;

    // 一度説明したスキルは記録しておく
    private readonly HashSet<string> _learnedSkills = new HashSet<string>();

    private CancellationTokenSource _cts;
    private Sequence _showTween;
    private RectTransform _panelRect;

    // ─────────────────────────────────────────
    private void Awake()
    {
        if (panel != null)
        {
            _panelRect = panel.GetComponent<RectTransform>();
            if (panelGroup == null) panelGroup = panel.GetComponent<CanvasGroup>();
            panel.SetActive(false);
        }
    }

    private void OnDestroy()
    {
        CancelPending();
        _showTween?.Kill();
        if (_panelRect != null) DOTween.Kill(_panelRect);
        if (panelGroup != null) DOTween.Kill(panelGroup);
    }

    // ─────────────────────────────────────────
    /// <summary>乗っ取り完了時に呼ぶ。初見のスキルだけカードを出す。</summary>
    public void ShowSkill(EnemySkillBase skill)
    {
        if (skill == null || panel == null) return;

        // ID が未設定なら型名で代用（Inspector の設定漏れ対策）
        string id = string.IsNullOrEmpty(skill.SkillID)
            ? skill.GetType().Name
            : skill.SkillID;

        // すでに取得済みなら表示しない
        if (!_learnedSkills.Add(id)) return;

        // ── 中身を流し込む
        if (skillIconImage != null)
        {
            Sprite icon = skill.SkillIcon != null ? skill.SkillIcon : fallbackIcon;
            skillIconImage.sprite = icon;
            skillIconImage.enabled = icon != null;
        }

        if (skillNameText != null)
            skillNameText.text = skill.SkillName;

        if (descriptionText != null)
            descriptionText.text = skill.SkillDescription;

        PlayShowTween();

        // ── 前回の待ちを打ち切ってから新しく待つ
        CancelPending();
        _cts = CancellationTokenSource.CreateLinkedTokenSource(
            this.GetCancellationTokenOnDestroy());

        HideAfterDelayAsync(_cts.Token).Forget();
    }

    /// <summary>リスタート時などに「初見」状態へ戻す</summary>
    public void ResetLearned()
    {
        _learnedSkills.Clear();
    }

    /// <summary>すぐ閉じたいとき（乗っ取り解除など）</summary>
    public void HideNow()
    {
        CancelPending();
        _showTween?.Kill();

        if (panel != null) panel.SetActive(false);
    }

    // ─────────────────────────────────────────
    private async UniTaskVoid HideAfterDelayAsync(CancellationToken token)
    {
        try
        {
            await UniTask.Delay(
                TimeSpan.FromSeconds(showDuration),
                DelayType.UnscaledDeltaTime,
                cancellationToken: token);

            await PlayHideTweenAsync(token);
        }
        catch (OperationCanceledException)
        {
            // 次のカードに差し替えられた / 破棄された。何もしない。
        }
    }

    private void PlayShowTween()
    {
        _showTween?.Kill();

        panel.SetActive(true);

        if (panelGroup != null) panelGroup.alpha = 0f;
        if (_panelRect != null) _panelRect.localScale = Vector3.one * 0.9f;

        _showTween = DOTween.Sequence().SetUpdate(true);

        if (panelGroup != null)
            _showTween.Join(panelGroup.DOFade(1f, fadeInDuration).SetUpdate(true));

        if (_panelRect != null)
            _showTween.Join(_panelRect.DOScale(1f, fadeInDuration)
                                      .SetEase(Ease.OutBack)
                                      .SetUpdate(true));
    }

    private async UniTask PlayHideTweenAsync(CancellationToken token)
    {
        _showTween?.Kill();

        if (panelGroup != null)
        {
            panelGroup.DOFade(0f, fadeOutDuration).SetUpdate(true);

            await UniTask.Delay(
                TimeSpan.FromSeconds(fadeOutDuration),
                DelayType.UnscaledDeltaTime,
                cancellationToken: token);
        }

        if (skillNameText != null) skillNameText.text = "";
        if (descriptionText != null) descriptionText.text = "";

        if (panel != null) panel.SetActive(false);
    }

    private void CancelPending()
    {
        if (_cts == null) return;

        _cts.Cancel();
        _cts.Dispose();
        _cts = null;
    }
}
