// ============================================================
// HijackPromptIcon.cs  (DOTween 版)
// 敵の頭上に出す「乗っ取り可能」アイコンの見た目制御。
//
// HijackableIndicator が SetActive で出し入れするだけなので、
// このスクリプトを IndicatorRoot 側に付けて
//   ・常にカメラを向く（ビルボード）
//   ・出るときにポンっと拡大（OutBack）
//   ・ふわふわ上下 + 脈打つ（Yoyo ループ）
// を担当させる。
//
// 【Setup】
//   Enemy (EnemyController + HijackableIndicator)
//    └ HijackPrompt            ← このスクリプト、LocalPos (0, 2.2, 0)
//        └ Canvas (World Space, Scale 0.01 前後)
//            └ Image           ← Hijack_Prompt.png
//
//   ・HijackableIndicator の IndicatorRoot に「HijackPrompt」を指定する
//
// 実装メモ:
//   ・SetActive で何度も出し入れされるので OnEnable で作り直し、
//     OnDisable で必ず Kill する（Tween が残ると次回の演出が壊れる）
//   ・ビルボードは Tween ではなく LateUpdate で毎フレーム
// ============================================================

using DG.Tweening;
using UnityEngine;

public class HijackPromptIcon : MonoBehaviour
{
    [Header("=== ビルボード ===")]
    [Tooltip("常にカメラを向く。ワールド空間アイコンなら ON")]
    [SerializeField] private bool faceCamera = true;

    /// <summary>未指定なら Camera.main を使う</summary>
    [SerializeField] private Camera targetCamera;

    [Header("=== 出現 ===")]
    [SerializeField] private float popInDuration = 0.22f;

    [Header("=== ふわふわ ===")]
    [SerializeField] private float bobAmplitude = 0.12f;
    [Tooltip("片道にかかる秒数")]
    [SerializeField] private float bobDuration = 0.7f;

    [Header("=== 脈動 ===")]
    [SerializeField] private float pulseAmount = 0.08f;
    [SerializeField] private float pulseDuration = 0.5f;

    private Vector3 _baseLocalPos;
    private Vector3 _baseScale;

    private Tween _pop;
    private Tween _bob;
    private Tween _pulse;

    // ─────────────────────────────────────────
    private void Awake()
    {
        _baseLocalPos = transform.localPosition;
        _baseScale = transform.localScale;

        if (targetCamera == null) targetCamera = Camera.main;
    }

    private void OnEnable()
    {
        KillAll();

        transform.localPosition = _baseLocalPos;
        transform.localScale = Vector3.zero;

        // ポンっと出る → 出終わってからループ演出を始める
        _pop = transform
            .DOScale(_baseScale, popInDuration)
            .SetEase(Ease.OutBack)
            .SetUpdate(true)
            .OnComplete(StartLoops);
    }

    private void OnDisable() => KillAll();

    private void StartLoops()
    {
        // 脈動（スケール）
        _pulse = transform
            .DOScale(_baseScale * (1f + pulseAmount), pulseDuration)
            .SetEase(Ease.InOutSine)
            .SetLoops(-1, LoopType.Yoyo)
            .SetUpdate(true);

        // ふわふわ（位置）— スケールとは別プロパティなので競合しない
        _bob = transform
            .DOLocalMoveY(_baseLocalPos.y + bobAmplitude, bobDuration)
            .SetEase(Ease.InOutSine)
            .SetLoops(-1, LoopType.Yoyo)
            .SetUpdate(true);
    }

    private void LateUpdate()
    {
        if (!faceCamera) return;

        if (targetCamera == null) targetCamera = Camera.main;
        if (targetCamera == null) return;

        // カメラに正対させる
        transform.rotation = Quaternion.LookRotation(
            transform.position - targetCamera.transform.position,
            Vector3.up);
    }

    private void KillAll()
    {
        _pop?.Kill();   _pop = null;
        _bob?.Kill();   _bob = null;
        _pulse?.Kill(); _pulse = null;

        DOTween.Kill(transform);
    }
}
