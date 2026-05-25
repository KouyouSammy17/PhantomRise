using UnityEngine;
using UnityEngine.UI;

public class EnemyHPbar : MonoBehaviour
{

    public Slider HP;
    public EnemyHealth EnemyHealth;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        HP.value = EnemyHealth.HPRatio;

        //お試し用
        if(Input.GetKeyDown(KeyCode.Space))
        {
            EnemyHealth.TakeDamage(10);
        }

    }

    void LateUpdate()
    {
        //　カメラと同じ向きに設定
        transform.rotation = Camera.main.transform.rotation;
    }
}
