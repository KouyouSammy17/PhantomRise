using TMPro;
using UnityEngine;

public class CountUp : MonoBehaviour
{
    //テキストで表示するテキストメッシュプロ
    [SerializeField] TextMeshProUGUI CountTime;
    private float countUpSpeed = 1f; // カウントアップの速度 

    private bool isCounting = false; // カウントアップ中かどうかのフラグ

    // カウント開始した時のTime.time
    private float startTime;



    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {

        if (isCounting==true)
        {
            // カウント開始時からの経過時間
            float currentTime = (Time.time - startTime) * countUpSpeed;
            // 分と秒に変換
            int minutes = Mathf.FloorToInt(currentTime / 60f);
            int seconds = Mathf.FloorToInt(currentTime % 60f);

            //表示
            CountTime.text = string.Format("{0}:{1:00}", minutes, seconds);
        }



    }

    public void StopCounting()
    {
        isCounting = false; // カウントアップを停止
    }

    public void StartCounting()
    {
        startTime = Time.time;

        isCounting = true;

        CountTime.text = "0.00";
    }
}
