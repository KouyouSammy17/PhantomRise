using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Sample {
public class BoxBreakScript : MonoBehaviour
{
    private GameObject EffectSmoke;

    void Start()
    {
        EffectSmoke = Resources.Load<GameObject>("Prefabs/Effects/EffectSmoke");
    }
    // broken this box
    void OnTriggerEnter(Collider other)
    {
        if(other.gameObject.name == "AttackEffect")
        {
            EffectSmoke = Instantiate(EffectSmoke);
            EffectSmoke.transform.position = this.transform.position;
            Destroy(this.gameObject);
        }
    }
}
}