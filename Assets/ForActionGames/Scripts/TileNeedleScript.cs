using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Sample {
public class TileNeedleScript : MonoBehaviour
{
    Animator anim;
    private static readonly int DefaultState = Animator.StringToHash("Base Layer.default");
    private static readonly int StabState = Animator.StringToHash("Base Layer.stab");
    [SerializeField] private float OffDelayTime = 3;
    private bool DelayFlg = true;

    void Start()
    {
        anim = this.GetComponent<Animator>();
    }
    // touch this object
    void OnTriggerStay(Collider other)
    {
        if(other.gameObject.tag == "Player")
        {
            if(anim.GetCurrentAnimatorStateInfo(0).fullPathHash != StabState && DelayFlg)
            {
                anim.CrossFade(StabState,0.1f,0,0);
                DelayFlg = false;
                Invoke("Interval", OffDelayTime);
            }
        }
    }
    // return to previous state
    private void Interval ()
    {
        anim.CrossFade(DefaultState,0.3f,0,0);
        DelayFlg = true;
    }
}
}