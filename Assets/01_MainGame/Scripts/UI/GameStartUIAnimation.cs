using System.Collections;
using Unity.VisualScripting;
using UnityEngine;

public class GameStartUIAnimation : MonoBehaviour
{
    [Header("=== 表示アニメーション ===")]

    [SerializeField] private float startScale = 0.5f;
    [SerializeField] private float maxScale = 1.15f;
    [SerializeField] private float normalScale = 1.0f;

    [SerializeField] private float showTime = 0.25f;
    [SerializeField] private float stayTime = 0.6f;
    [SerializeField] private float hideTime = 0.25f;


    private RectTransform rectTransform;

    [SerializeField] private AudioSource gameStartAudioSource;


    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();

        // 最初は小さくする
        rectTransform.localScale =
            Vector3.one * startScale;

        gameObject.SetActive(false);
    }


    /// <summary>
    /// GAME START! の表示アニメーション
    /// </summary>
    public IEnumerator PlayAnimation()
    {
        gameObject.SetActive(true);

        // GAME START! が表示される瞬間に1回だけ再生
        if (gameStartAudioSource != null)
        {
            gameStartAudioSource.Play();
        }

        // 最初の状態
        rectTransform.localScale =
            Vector3.one * startScale;


        // ==================================================
        // ① 小さい状態 → 大きく表示
        // ==================================================

        float timer = 0f;

        while (timer < showTime)
        {
            timer += Time.deltaTime;

            float t =
                Mathf.Clamp01(timer / showTime);

            // なめらかにする
            t = Mathf.SmoothStep(0f, 1f, t);

            float scale =
                Mathf.Lerp(
                    startScale,
                    maxScale,
                    t
                );


            rectTransform.localScale =
                Vector3.one * scale;

            yield return null;
        }


        // ==================================================
        // ② 少しだけ縮んで通常サイズへ
        // ==================================================

        timer = 0f;

        while (timer < 0.1f)
        {
            timer += Time.deltaTime;

            float t =
                Mathf.Clamp01(timer / 0.1f);

            t = Mathf.SmoothStep(0f, 1f, t);

            float scale =
                Mathf.Lerp(
                    maxScale,
                    normalScale,
                    t
                );

            rectTransform.localScale =
                Vector3.one * scale;

            yield return null;
        }


        // 通常サイズ
        rectTransform.localScale =
            Vector3.one * normalScale;


        // ==================================================
        // ③ 少し表示
        // ==================================================

        yield return new WaitForSeconds(stayTime);


        // ==================================================
        // ④ 縮小して消える
        // ==================================================

        timer = 0f;

        while (timer < hideTime)
        {
            timer += Time.deltaTime;

            float t =
                Mathf.Clamp01(timer / hideTime);

            t = Mathf.SmoothStep(0f, 1f, t);

            float scale =
                Mathf.Lerp(
                    normalScale,
                    0f,
                    t
                );

            rectTransform.localScale =
                Vector3.one * scale;

            yield return null;
        }


        // ==================================================
        // ⑤ 完全に消す
        // ==================================================

        rectTransform.localScale =
            Vector3.one * startScale;

        gameObject.SetActive(false);
    }
}