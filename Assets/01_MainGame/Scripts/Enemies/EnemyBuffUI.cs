using UnityEngine;
using System.Collections;

public class EnemyBuffUI : MonoBehaviour
{
    //アイコンの表示・非表示を制御するための参照
    [SerializeField] private GameObject SpeedDebufficon;
    [SerializeField] private GameObject poisonicon;
    [SerializeField] private GameObject bloodicon;

    //スピードダウンアイコン表示
    public void ShowSpeedDebuff(float duration)
    {
        StartCoroutine(SpeedDebuffRoutine(duration));
    }

    private IEnumerator SpeedDebuffRoutine(float duration)
    {
        SpeedDebufficon.SetActive(true);

        yield return new WaitForSeconds(duration);

        SpeedDebufficon.SetActive(false);
    }

    //ポイズンアイコン表示
    public void ShowPoisonDebuff(float duration)
    {
        StartCoroutine(PoisonDebuffRoutine(duration));
    }

    private IEnumerator PoisonDebuffRoutine(float duration)
    {
        poisonicon.SetActive(true);
        yield return new WaitForSeconds(duration);
        poisonicon.SetActive(false);
    }

    //出血アイコン表示
    public void ShowBloodDebuff(float duration)
    {
        StartCoroutine(BloodDebuffRoutine(duration));
    }

    private IEnumerator BloodDebuffRoutine(float duration)
    {
        bloodicon.SetActive(true);
        yield return new WaitForSeconds(duration);
        bloodicon.SetActive(false);
    }

    //アイコン非表示
    public void HideAll()
    {
        SpeedDebufficon.SetActive(false);
        poisonicon.SetActive(false);
        bloodicon.SetActive(false);
    }

}
