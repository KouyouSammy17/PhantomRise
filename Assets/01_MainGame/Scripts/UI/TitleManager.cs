using UnityEngine;

public class TitleManager : MonoBehaviour
{
    [SerializeField] private GameObject titleUI; // タイトル画面のルートUI（Canvas or Panel）

    public void OnStartButton()
    {
        titleUI?.SetActive(false);
        GameManager.Instance.LoadGameScene();
    }
}
