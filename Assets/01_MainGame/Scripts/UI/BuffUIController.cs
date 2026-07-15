using Unity.VisualScripting;
using UnityEngine;

public class BuffUIController : MonoBehaviour
{

    public static BuffUIController Instance;

    [SerializeField] private GameObject demonIcon;
    [SerializeField] private GameObject specterIcon;

    [SerializeField] private GameObject speedDownIcon;

    [SerializeField] private GameObject poisonIcon;
    [SerializeField] private GameObject bloodIcon;


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
            case BuffType.SpeedDeBuff:
                speedDownIcon.SetActive(true);
                break;
            case BuffType.PoisonDeBuff:
                poisonIcon.SetActive(true);
                break;
            case BuffType.BleedingDeBuff:
                bloodIcon.SetActive(true);
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
            case BuffType.SpeedDeBuff:
                speedDownIcon.SetActive(false);
                break;
            case BuffType.PoisonDeBuff:
                poisonIcon.SetActive(false);
                break;
            case BuffType.BleedingDeBuff:
                bloodIcon.SetActive(false);
                break;

        }
    }

    public void HideAll()
    {
        demonIcon.SetActive(false);
        specterIcon.SetActive(false);
        speedDownIcon.SetActive(false);
        poisonIcon.SetActive(false);
        bloodIcon.SetActive(false);

    }
}
