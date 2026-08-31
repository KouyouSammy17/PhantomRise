using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;
using TMPro;


public class SkillCooldownUI : MonoBehaviour
{
    [Header("=== 参照 ===")]
    [SerializeField] private PlayerStateMachine playerMachine;

    [Header("=== UI ===")]
    [SerializeField] private Image cooldownMask;
    [SerializeField] private GameObject skillUIPanel;
    [SerializeField] private Image skillIconImage;
    [SerializeField] private TextMeshProUGUI skillNameText;
    [SerializeField] private Sprite defaultIcon;

    // 現在追跡中の敵スキル
    private EnemySkillBase _trackedSkill;

    [Header("=== サウンド ===")]
    [SerializeField] private AudioSource skillAudio;

    [Header("=== アイコン演出 ===")]
    [Tooltip("スキルが使えるようになった瞬間に弾む量")]
    [SerializeField] private float readyPunch = 0.3f;
    [SerializeField] private float readyPunchDuration = 0.45f;

    [Tooltip("使用可能なあいだ、ゆっくり脈打たせる")]
    [SerializeField] private bool idlePulseWhenReady = true;
    [SerializeField] private float idleAmp = 0.06f;
    [SerializeField] private float idleDuration = 1.1f;

    [Tooltip("クールダウン中のアイコン色（暗くして使えないと分かるようにする）")]
    [SerializeField] private Color cooldownTint = new Color(0.45f, 0.45f, 0.5f, 1f);

    [Tooltip("敵を乗り換えてアイコンが差し替わるときの出現時間")]
    [SerializeField] private float swapDuration = 0.28f;

    // 前フレームでクールダウン中だったか
    private bool _wasOnCooldown = false;

    // ─── アイコン演出用 ───────────────────────────
    // プレハブで設定されたスケールを基準にする
    // （決め打ちで 1 に戻すと拡大配置したアイコンが縮む）
    private Vector3 _iconBaseScale = Vector3.one;
    private Tween _idleTween;
    private Tween _punchTween;
    private Tween _swapTween;


    private void Awake()
    {
        if (playerMachine == null)
            playerMachine = FindAnyObjectByType<PlayerStateMachine>();

        if (skillIconImage != null)
            _iconBaseScale = skillIconImage.rectTransform.localScale;
    }

    private void OnDestroy() => KillIconTweens();


    private void Update()
    {
        bool isHijacked = playerMachine != null
            && playerMachine.IsEffectivelyHijacked;


        // パネルの表示切り替え
        if (skillUIPanel != null)
            skillUIPanel.SetActive(isHijacked);


        // ==========================================
        // 乗っ取り中ではない
        // ==========================================

        if (!isHijacked)
        {
            _trackedSkill = null;

            if (cooldownMask != null)
                cooldownMask.fillAmount = 0f;

            _wasOnCooldown = false;

            // 非表示のあいだは演出も止めて元の大きさに戻す
            ResetIcon();

            return;
        }


        // ==========================================
        // 現在乗っ取っている敵を取得
        // ==========================================

        EnemyController enemy =
            playerMachine.Hijacked.CurrentEnemy;


        if (enemy != null)
        {
            EnemySkillBase skill =
                enemy.GetComponent<EnemySkillBase>();


            // 敵が変わったとき
            if (skill != _trackedSkill)
            {
                _trackedSkill = skill;

                RefreshSkillVisual();

                // 新しい敵を乗っ取った瞬間は
                // 現在のクールダウン状態を記録するだけ
                if (_trackedSkill != null)
                {
                    _wasOnCooldown =
                        _trackedSkill.CooldownFillAmount > 0f;
                }
                else
                {
                    _wasOnCooldown = false;
                }
            }
        }
        else
        {
            if (_trackedSkill != null)
            {
                _trackedSkill = null;
                RefreshSkillVisual();
            }

            _wasOnCooldown = false;
        }


        // ==========================================
        // クールダウン表示
        // ==========================================

        if (cooldownMask != null)
        {
            float currentFill =
                _trackedSkill != null
                    ? _trackedSkill.CooldownFillAmount
                    : 0f;


            cooldownMask.fillAmount = currentFill;


            // ======================================
            // クールダウン完了を検出
            // ======================================

            if (_wasOnCooldown && currentFill <= 0f)
            {
                Debug.Log("[SkillCooldownUI] スキルチャージ完了！");

                if (skillAudio != null)
                {
                    skillAudio.PlayOneShot(
                        skillAudio.clip);
                }

                PlayReadyAnimation();
            }

            bool nowOnCooldown = currentFill > 0f;

            // クールダウンに入った瞬間：脈動を止めて暗くする
            if (!_wasOnCooldown && nowOnCooldown)
                EnterCooldownLook();


            // 現在の状態を保存
            _wasOnCooldown = nowOnCooldown;
        }
    }


    private void RefreshSkillVisual()
    {
        // アイコン
        if (skillIconImage != null)
        {
            Sprite icon =
                _trackedSkill != null &&
                _trackedSkill.SkillIcon != null
                    ? _trackedSkill.SkillIcon
                    : defaultIcon;

            if (icon != null)
                skillIconImage.sprite = icon;
        }


        // スキル名
        if (skillNameText != null)
        {
            skillNameText.text =
                _trackedSkill != null
                    ? _trackedSkill.SkillName
                    : "";
        }

        // 差し替わったアイコンをポップさせる
        PlaySwapAnimation();
    }

    // ─────────────────────────────────────────
    // アイコン演出
    //
    // Tween はすべて SetUpdate(true)（未スケール時間）。
    // ポーズ中は Time.timeScale = 0 なので、
    // これが無いと演出の途中で固まったまま残る。
    // ─────────────────────────────────────────

    /// <summary>敵を乗り換えてアイコンが変わったとき。</summary>
    private void PlaySwapAnimation()
    {
        if (skillIconImage == null) return;

        RectTransform rt = skillIconImage.rectTransform;

        _swapTween?.Kill();
        _idleTween?.Kill();

        rt.localScale = _iconBaseScale * 0.6f;

        _swapTween = rt.DOScale(_iconBaseScale, swapDuration)
                       .SetEase(Ease.OutBack)
                       .SetUpdate(true)
                       .OnComplete(StartIdleIfReady);
    }

    /// <summary>クールダウンが明けた瞬間。</summary>
    private void PlayReadyAnimation()
    {
        if (skillIconImage == null) return;

        skillIconImage.color = Color.white;

        RectTransform rt = skillIconImage.rectTransform;

        _idleTween?.Kill();
        _punchTween?.Kill();

        // パンチは開始時のスケールに戻って終わるので、先に基準へそろえる
        rt.localScale = _iconBaseScale;

        if (readyPunch <= 0f)
        {
            StartIdleIfReady();
            return;
        }

        _punchTween = rt.DOPunchScale(_iconBaseScale * readyPunch, readyPunchDuration, 6, 0.7f)
                        .SetUpdate(true)
                        .OnComplete(StartIdleIfReady);
    }

    /// <summary>クールダウンに入った瞬間。</summary>
    private void EnterCooldownLook()
    {
        _idleTween?.Kill();
        _idleTween = null;

        if (skillIconImage == null) return;

        skillIconImage.color = cooldownTint;
        skillIconImage.rectTransform.localScale = _iconBaseScale;
    }

    private void StartIdleIfReady()
    {
        if (!idlePulseWhenReady || skillIconImage == null) return;
        if (_wasOnCooldown) return;   // まだクールダウン中なら脈動させない

        _idleTween?.Kill();
        _idleTween = skillIconImage.rectTransform
            .DOScale(_iconBaseScale * (1f + idleAmp), idleDuration)
            .SetEase(Ease.InOutSine)
            .SetLoops(-1, LoopType.Yoyo)
            .SetUpdate(true);
    }

    private void ResetIcon()
    {
        KillIconTweens();

        if (skillIconImage == null) return;

        skillIconImage.rectTransform.localScale = _iconBaseScale;
        skillIconImage.color = Color.white;
    }

    private void KillIconTweens()
    {
        _idleTween?.Kill();   _idleTween  = null;
        _punchTween?.Kill();  _punchTween = null;
        _swapTween?.Kill();   _swapTween  = null;

        if (skillIconImage != null)
            DOTween.Kill(skillIconImage.rectTransform);
    }
}