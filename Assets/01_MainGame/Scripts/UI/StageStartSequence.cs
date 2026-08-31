using System.Collections;
using Unity.Cinemachine;
using UnityEngine;
using static UnityEditor.Experimental.AssetDatabaseExperimental.AssetDatabaseCounters;

public class StageStartSequence : MonoBehaviour
{
    [Header("=== プレイヤー ===")]
    [SerializeField] private PlayerStateMachine player;

    [Header("=== カメラ ===")]
    [SerializeField] private CinemachineCamera playerCamera;
    [SerializeField] private CinemachineCamera startCamera;

    [Header("=== 設定 ===")]
    [SerializeField] private float waitTime = 1.5f;
    [SerializeField] private float cameraMoveTime = 2.0f;

    [Header("=== 開幕フェード ===")]
    [Tooltip("真っ黒から始めて、カメラが構えてから明ける")]
    [SerializeField] private bool fadeInOnStart = true;

    [SerializeField] private float fadeInDuration = 0.8f;

    [Header("=== GAME START UI ===")]
    [SerializeField] private GameObject gameStartUI;

    [SerializeField] private GameObject CountUI;

    [SerializeField] private GameStartUIAnimation gameStartAnimation;

    [SerializeField] private StageBGM stageBGM;

    private EnemyController[] enemies;

    private Camera mainCamera;

    // 通常時のカメラ位置
    private Vector3 normalPosition;
    private Quaternion normalRotation;

    [SerializeField] private GameObject[] UIs;
    [SerializeField] private CountUp countUp;





    // ============================================================
    // Awake
    // ============================================================

    private void Awake()
    {
        // シーン読み込み直後の自動フェードインを止めて、
        // 下の StartSequence が明けるまで真っ黒を保持する。
        if (fadeInOnStart)
        {
            ScreenFader.Instance.HoldBlack();
        }

        mainCamera = Camera.main;

        if (mainCamera == null)
        {
            Debug.LogError("Main Cameraが見つかりません！");
            return;
        }

        // 通常時のカメラ位置を保存
        normalPosition = mainCamera.transform.position;
        normalRotation = mainCamera.transform.rotation;

        // 最初はStartCameraを最優先
        startCamera.Priority = 100;
        playerCamera.Priority = 0;
    }


    // ============================================================
    // Start
    // ============================================================

    private void Start()
    {
         foreach (var ui in UIs)
        {
            ui.SetActive(false);
        }

        if (gameStartUI != null)
        {
            gameStartUI.SetActive(false);
        }

        if (CountUI != null)
        {
            CountUI.SetActive(false);
        }

        // プレイヤー停止
        if (player != null)
        {
            player.SetStageStarted(false);
            player.StopMode();
        }

        //BGMを停止
        if (stageBGM != null)
        {
            stageBGM.SetStageBGMVolume(0.3f);
        }

        // 敵取得
        enemies = FindObjectsByType<EnemyController>(
            FindObjectsSortMode.None
        );

        // 敵停止
        foreach (EnemyController enemy in enemies)
        {
            enemy.SetStageStarting(true);
        }

        countUp.StopCounting();
        StartCoroutine(StartSequence());
    }


    // ============================================================
    // 開始演出
    // ============================================================

    private IEnumerator StartSequence()
    {
        // --------------------------------------------------------
        // PlayerStateMachine.Start() が終わるまで1フレーム待つ
        // --------------------------------------------------------

        yield return null;


        // --------------------------------------------------------
        // アニメーション再生
        // --------------------------------------------------------

        if (player != null)
        {
            player.GhostAnim?.StartPlayAttackAnimation();
        }


        // --------------------------------------------------------
        // StartCameraを確実に表示
        // --------------------------------------------------------

        startCamera.Priority = 100;
        playerCamera.Priority = 0;

        yield return null;


        // --------------------------------------------------------
        // カメラが構えたので暗転から明ける
        // （Tween は実時間で動くので Realtime で待つ）
        // --------------------------------------------------------

        if (fadeInOnStart)
        {
            ScreenFader.Instance.FadeIn(fadeInDuration);

            yield return new WaitForSecondsRealtime(fadeInDuration);
        }


        // --------------------------------------------------------
        // プレイヤー正面を1.5秒見る
        // --------------------------------------------------------

        yield return new WaitForSeconds(waitTime);


        // --------------------------------------------------------
        // Main CameraをStartCameraの位置へ移動
        // --------------------------------------------------------

        mainCamera.transform.position =
            startCamera.transform.position;

        mainCamera.transform.rotation =
            startCamera.transform.rotation;


        // --------------------------------------------------------
        // Cinemachineを解除
        // --------------------------------------------------------

        startCamera.Priority = 0;
        playerCamera.Priority = 0;

        yield return null;


        // --------------------------------------------------------
        // StartCamera → 通常カメラへ移動
        // --------------------------------------------------------

        yield return StartCoroutine(
            MoveCameraToNormal()
        );


        // --------------------------------------------------------
        // 通常カメラの位置へ完全移行
        // --------------------------------------------------------

        mainCamera.transform.position = normalPosition;
        mainCamera.transform.rotation = normalRotation;


        // --------------------------------------------------------
        // ★重要
        // 通常カメラの向きを移動基準として再登録
        // --------------------------------------------------------

        if (player != null)
        {
            player.CacheCameraAxes();
        }


        // --------------------------------------------------------
        // 通常カメラをCinemachineへ戻す
        // --------------------------------------------------------

        playerCamera.Priority = 100;

        yield return null;

        yield return new WaitForSeconds(1.5f);


        // --------------------------------------------------------
        // GAME START!
        // --------------------------------------------------------

        if (gameStartUI != null)
        {
            gameStartUI.SetActive(true);
        }

        //yield return new WaitForSeconds(3.0f);

        //if (gameStartUI != null)
        //{
        //    gameStartUI.SetActive(false);
        //}

        if (gameStartAnimation != null)
        {
            yield return StartCoroutine(
                gameStartAnimation.PlayAnimation()
            );
        }

       // yield return new WaitForSeconds(1.0f);


        // --------------------------------------------------------
        // ゲーム開始
        // --------------------------------------------------------

        if (CountUI != null)
        {
            CountUI.SetActive(true);
        }
        countUp.StartCounting();

        foreach (var ui in UIs)
        {
            ui.SetActive(true);
        }
        
        foreach (EnemyController enemy in enemies)
        {
            enemy.SetStageStarting(false);
        }

        if (player != null)
        {
            player.SetStageStarted(true);
            player.ResumeMode();
        }

        //BGMを再生
        if (stageBGM != null)
        {
            stageBGM.SetStageBGMVolume(1f);
        }
    }


    // ============================================================
    // StartCamera → 通常カメラ
    // ============================================================

    private IEnumerator MoveCameraToNormal()
    {
        Vector3 startPosition =
            mainCamera.transform.position;

        Quaternion startRotation =
            mainCamera.transform.rotation;

        Vector3 targetPosition =
            normalPosition;

        Quaternion targetRotation =
            normalRotation;

        float timer = 0f;

        while (timer < cameraMoveTime)
        {
            timer += Time.deltaTime;

            float t =
                Mathf.Clamp01(
                    timer / cameraMoveTime
                );

            t = Mathf.SmoothStep(
                0f,
                1f,
                t
            );

            mainCamera.transform.position =
                Vector3.Lerp(
                    startPosition,
                    targetPosition,
                    t
                );

            mainCamera.transform.rotation =
                Quaternion.Slerp(
                    startRotation,
                    targetRotation,
                    t
                );

            yield return null;
        }

        mainCamera.transform.position =
            targetPosition;

        mainCamera.transform.rotation =
            targetRotation;
    }
}