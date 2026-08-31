// ============================================================
// TitleManager.cs
// タイトル画面の開始ボタン。
//
// 押した瞬間にシーンを切り替えず、
//   ① 少し待つ（ボタンの演出・効果音を見せる）
//   ② 画面を暗転させる
//   ③ シーン遷移
// の順に進める。
//
// 実装メモ:
//   ・titleUI は TitleManager と同じ GameObject を指しているので、
//     先に SetActive(false) すると自分ごと止まってしまう。
//     コルーチンではなく DOTween を使い、暗転が終わってから消す。
//   ・Tween は SetUpdate(true)（timeScale に左右されない）
//   ・暗転用の CanvasGroup が未設定なら実行時に全画面の黒を作る
// ============================================================

using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class TitleManager : MonoBehaviour
{
    [SerializeField] private GameObject titleUI; // タイトル画面のルートUI（Canvas or Panel）

    [Header("=== 開始演出 ===")]
    [Tooltip("ボタンを押してから暗転が始まるまでの間")]
    [SerializeField] private float startDelay = 0.35f;

    [Tooltip("暗転にかける時間")]
    [SerializeField] private float fadeDuration = 0.6f;

    [Tooltip("暗転に使う CanvasGroup。未設定なら実行時に全画面の黒を作る")]
    [SerializeField] private CanvasGroup fadeOverlay;

    [SerializeField] private Color fadeColor = Color.black;

    /// <summary>連打で二重に走らせないためのフラグ</summary>
    private bool _starting;

    public void OnStartButton()
    {
        if (_starting) return;
        _starting = true;

        CanvasGroup fade = ResolveFadeOverlay();

        Sequence seq = DOTween.Sequence().SetUpdate(true);

        // ① 少し待つ
        seq.AppendInterval(startDelay);

        // ② 暗転
        if (fade != null)
        {
            fade.gameObject.SetActive(true);
            fade.alpha = 0f;
            fade.blocksRaycasts = true;   // 暗転中はボタンを触らせない

            seq.Append(fade.DOFade(1f, fadeDuration)
                           .SetEase(Ease.InQuad)
                           .SetUpdate(true));
        }

        // ③ シーン遷移
        seq.OnComplete(() =>
        {
            titleUI?.SetActive(false);
            GameManager.Instance?.LoadGameScene();
        });
    }

    /// <summary>
    /// 暗転に使う CanvasGroup を返す。
    /// Inspector で未設定なら、Canvas の一番手前に黒い全画面を作る。
    /// </summary>
    private CanvasGroup ResolveFadeOverlay()
    {
        if (fadeOverlay != null) return fadeOverlay;

        Canvas parentCanvas = GetComponentInParent<Canvas>();
        if (parentCanvas == null)
        {
            // Canvas の下にいないので暗転できない。遷移だけは進める
            Debug.LogWarning("[TitleManager] Canvas が見つかりません。暗転なしで遷移します。");
            return null;
        }

        var go = new GameObject("StartFade",
            typeof(RectTransform), typeof(Canvas), typeof(GraphicRaycaster),
            typeof(CanvasGroup), typeof(Image));

        go.transform.SetParent(parentCanvas.transform, false);

        var rt = (RectTransform)go.transform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        rt.localScale = Vector3.one;

        var canvas = go.GetComponent<Canvas>();
        canvas.overrideSorting = true;
        canvas.sortingOrder = 900;   // タイトルロゴより確実に手前

        go.GetComponent<Image>().color = fadeColor;

        fadeOverlay = go.GetComponent<CanvasGroup>();
        fadeOverlay.alpha = 0f;
        return fadeOverlay;
    }
}
