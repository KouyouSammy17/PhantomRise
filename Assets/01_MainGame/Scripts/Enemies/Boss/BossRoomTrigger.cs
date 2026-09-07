using UnityEngine;
using Unity.Cinemachine;
using UnityEngine.AI;

public class BossRoomTrigger : MonoBehaviour
{
    [SerializeField] private GameObject BossUI;

    [SerializeField] private GameObject UIManager;

    [SerializeField] private GameObject PlayerCanvas;

    [SerializeField] private PlayerStateMachine playerStateMachine;

    [Header("ボス")]
    [SerializeField] private BossController bossController;

    [Header("Cinemachine")]
    [SerializeField] private CinemachineCamera playerCamera;
    [SerializeField] private CinemachineCamera bossCamera;

    private bool hasTriggered = false;

    //ボス戦のBGM
    //[SerializeField] private AudioClip bossBGM;
    [SerializeField] private AudioSource bossBGMSource;

    [SerializeField] private StageBGM stageBGM;

    [SerializeField] private CountUp countUp;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private void Start()
    {
        BossUI.SetActive(false);

        // 通常時
        playerCamera.Priority = 10;
        bossCamera.Priority = 0;

    }


    // Update is called once per frame
    void Update()
    {
        
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            BossUI.SetActive(true);

            if (hasTriggered)
                return;

            hasTriggered = true;

            StartCoroutine(BossEntranceSequence());

            //ステージのBGMを止めてボス戦のBGMを再生する
            stageBGM.StopStageBGM();
            bossBGMSource.Play();
        }
    }

    private System.Collections.IEnumerator BossEntranceSequence()
    {
        // =========================
        // ① ボスHP表示
        // =========================

        BossUI.SetActive(true);

        // =========================
        // ② ボスカメラへ切り替え
        // =========================

        //プレイヤーを止める
        playerStateMachine.StopMode();

        // カウントアップを停止
        countUp.PauseCounting();

        //画面に表示してあるUIを非表示にする
        UIManager.SetActive(false);
        PlayerCanvas.SetActive(false);

        bossCamera.Priority = 20;
        playerCamera.Priority = 10;

        // カメラがボスへ移動する時間
        yield return new WaitForSeconds(1.5f);

        // =========================
        // ③ ボスTaunting
        // =========================

        bossController.StartBossIntro();

        // Taunting時間
        yield return new WaitForSeconds(2f);

        // =========================
        // ④ プレイヤーカメラへ戻す
        // =========================

        playerCamera.Priority = 20;
        bossCamera.Priority = 10;

        // カメラが戻る時間
        yield return new WaitForSeconds(1.5f);

        //プレイヤーを動かす
        playerStateMachine.ResumeMode();

        //カウントを再開する
        countUp.StartCounting();

        //画面に表示してあるUIを表示する
        UIManager.SetActive(true);
        PlayerCanvas.SetActive(true);

        // =========================
        // ⑤ ボス戦開始
        // =========================

        bossController.StartBossBattle();
    }

    public void StopBossBGM()
    {
        if (bossBGMSource != null)
        {
            bossBGMSource.Stop();
        }
    }

}
