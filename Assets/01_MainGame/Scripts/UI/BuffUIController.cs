using UnityEngine;

public class BuffUIController : MonoBehaviour
{
 
    public static BuffUIController Instance;

    [SerializeField] private GameObject demonIcon;
    [SerializeField] private GameObject specterIcon;

    private void Awake()
    {
        Instance = this;
        HideAll();
    }

    public void ShowBuff(BuffType type)
    {
        switch (type)
        {
            case BuffType.DemonBuff:
                demonIcon.SetActive(true);
                break;

            case BuffType.SpecterBuff:
                specterIcon.SetActive(true);
                break;
        }
    }

    public void HideBuff(BuffType type)
    {
        switch (type)
        {
            case BuffType.DemonBuff:
                demonIcon.SetActive(false);
                break;

            case BuffType.SpecterBuff:
                specterIcon.SetActive(false);
                break;
        }
    }

    public void HideAll()
    {
        demonIcon.SetActive(false);
        specterIcon.SetActive(false);
    }
}
