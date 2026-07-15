using UnityEngine;
using UnityEngine.UI;

public class EnemyHPbar : MonoBehaviour
{

    public Slider HP;
    public EnemyHealth EnemyHealth;

    [SerializeField] private GameObject Boss;
    [SerializeField] private GameObject BossUI;

    [SerializeField] private GameObject EnemyUI;

    // Update is called once per frame
    void Update()
    {
        HP.value = EnemyHealth.HPRatio;

        if (BossUI!=null&&Boss == null)
        {

            BossUI.SetActive(false);
        }

        //敵のhpが0になったら敵キャンバスを非表示にする
        if(EnemyHealth.CurrentHP <= 0)
        {
          EnemyUI.SetActive(false);
        }
    }

    void LateUpdate()
    {
        //　カメラと同じ向きに設定
        if (BossUI == null)
        {
            transform.rotation = Camera.main.transform.rotation;
        }
        }
    }
