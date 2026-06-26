using UnityEngine;
using UnityEngine.UI;

public class EnemyHPbar : MonoBehaviour
{

    public Slider HP;
    public EnemyHealth EnemyHealth;

    [SerializeField] private GameObject Boss;
    [SerializeField] private GameObject BossUI;

    // Update is called once per frame
     void Update()
    {
        HP.value = EnemyHealth.HPRatio;

        if (BossUI!=null&&Boss == null)
        {

            BossUI.SetActive(false);
        }
    }

    void LateUpdate()
    {
        //　カメラと同じ向きに設定
        transform.rotation = Camera.main.transform.rotation;
    }
}
