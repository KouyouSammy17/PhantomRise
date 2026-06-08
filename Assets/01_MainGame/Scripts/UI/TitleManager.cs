using UnityEngine;

public class TitleManager : MonoBehaviour
{
    public void OnStartButton()
    {
        GameManager.Instance.LoadGameScene();
    }
}
