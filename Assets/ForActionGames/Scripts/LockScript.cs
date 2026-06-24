using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Sample {
public class LockScript : MonoBehaviour
{
    private GameObject LockBreak;
    private Transform Chain;
    private Transform BreakChain;
    private float rot_x = 0;
    private float rot_y = 0;
    private float rot_z = 0;
    private PublicValueScript _Value;

    void Start()
    {
        LockBreak = Resources.Load<GameObject>("Prefabs/Objects/Gimmicks/LockBreak");
        LockBreak = Instantiate(LockBreak);
        BreakChain = LockBreak.transform.Find("LockBreakArmature/Root/Chain.Root");
        LockBreak.transform.position = this.transform.position;
        LockBreak.transform.rotation = this.transform.rotation;
        LockBreak.SetActive(false);
        Chain = this.transform.Find("LockArmature/Root/Chain.Root");
        rot_x = Chain.transform.localRotation.x;
        rot_y = Chain.transform.localRotation.y;
        rot_z = Chain.transform.localRotation.z;
        _Value = FindObjectOfType<PublicValueScript>();
    }
    // rotate the chain
    void Update()
    {
        rot_x += 10 * Time.deltaTime;
        rot_y += 5 * Time.deltaTime;
        rot_z += 40 * Time.deltaTime;
        Chain.localRotation = Quaternion.Euler(rot_x, rot_y, rot_z);
    }
    // unlock
    void OnTriggerEnter(Collider other)
    {
        if(other.gameObject.name == "ItemCollider")
        {
            if(_Value.HaveKeyNum_Public > 0)
            {
                _Value.HaveKeyNum_Public--;

                LockBreak.SetActive(true);
                // rotate the chain
                BreakChain.transform.localRotation = Quaternion.Euler(rot_x, rot_y, rot_z);

                Destroy(this.gameObject);
            }
        }
    }

}
}