using UnityEngine;
using System.Collections;

public class TitleCameraCut : MonoBehaviour
{
    [SerializeField] private Transform[] cameraPoints;
    [SerializeField] private float stayTime = 3f;
    [SerializeField] private float moveAmount = 0.5f;
    [SerializeField] private float moveSpeed = 0.5f;

    private int currentIndex = 0;

    private void Start()
    {
        StartCoroutine(CameraCut());
    }

    private IEnumerator CameraCut()
    {
        while (true)
        {
            Transform point = cameraPoints[currentIndex];

            // カメラの初期位置
            Vector3 startPosition = point.position;

            // 少し右方向へ移動する
            Vector3 endPosition = startPosition + point.right * moveAmount;

            // カメラの位置を初期化
            transform.position = startPosition;
            transform.rotation = point.rotation;

            float elapsed = 0f;

            // スーッと移動
            while (elapsed < stayTime)
            {
                elapsed += Time.deltaTime;

                float t = elapsed / stayTime;

                // なめらかに移動
                t = Mathf.SmoothStep(0f, 1f, t);

                transform.position = Vector3.Lerp(
                    startPosition,
                    endPosition,
                    t
                );

                yield return null;
            }

            // 次のカット
            currentIndex++;

            if (currentIndex >= cameraPoints.Length)
            {
                currentIndex = 0;
            }
        }
    }
}
