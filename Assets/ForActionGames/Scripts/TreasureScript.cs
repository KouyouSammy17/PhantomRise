using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Sample {
public class TreasureScript : MonoBehaviour
{
    Animator anim;
    private static readonly int OpenState = Animator.StringToHash("Base Layer.open");
    private bool OpenFlg = false;
    // The item available from treasure chests.
    [SerializeField] private GameObject Item;
    

    void Start()
    {
        anim = this.GetComponent<Animator>();
    }
    // opening this treasure
    void OnTriggerEnter(Collider other)
    {
        if(other.gameObject.name == "AttackEffect" && !OpenFlg)
        {
            anim.CrossFade(OpenState,0.1f,0,0);
            OpenFlg = true;
            Invoke("GetItem", 0.8f);
        }
    }
    // spawn the item
    private void GetItem ()
    {
        GameObject obj = Instantiate(Item);
        obj.transform.position = this.transform.position + new Vector3(0,0.5f,0);
    }
}
}
