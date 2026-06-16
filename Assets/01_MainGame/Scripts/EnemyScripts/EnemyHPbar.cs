using UnityEngine;
using UnityEngine.UI;

public class EnemyHPbar : MonoBehaviour
{

    public Slider HP;
    public EnemyHealth EnemyHealth;

    // Update is called once per frame
     void Update()
    {
        HP.value = EnemyHealth.HPRatio;
    }

    void LateUpdate()
    {
        //　カメラと同じ向きに設定
        transform.rotation = Camera.main.transform.rotation;
    }
}
