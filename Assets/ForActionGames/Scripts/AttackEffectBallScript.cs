using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Sample {
public class AttackEffectBallScript : MonoBehaviour
{
    [SerializeField] private GameObject Effect_Hit;

    void Update()
    {
        float speed = 5 * Time.deltaTime;
        this.transform.position = this.transform.position + (this.transform.rotation * new Vector3(0,0,speed));
    }
    
    void OnTriggerEnter(Collider other)
    {
        if(other.gameObject.tag != "Player" && other.gameObject.name != "Lock" && other.gameObject.name != "AttackEffect")
        {
            GameObject obj = Instantiate(Effect_Hit);
            obj.transform.position = this.transform.position;
            Destroy(this.gameObject);
        }
    }
}
}