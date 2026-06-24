using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Sample {
public class PositionResetScript : MonoBehaviour
{
    // The switch be able to move this object. 
    [SerializeField] private GameObject SwitchObj;
    [SerializeField] private Vector3 ResetPos;
    private Quaternion ResetRot;
    private Animator Anim;
    private static readonly int TurnOnState = Animator.StringToHash("Base Layer.turnon");
    private static readonly int TurnOffState = Animator.StringToHash("Base Layer.turnoff");
    private bool flg = false;
    private Vector3 pos_start;
    private Vector3 pos_end;
    private float t = 0;


    void Start()
    {
        Anim = SwitchObj.GetComponent<Animator>();
        ResetRot = this.transform.rotation;
        pos_start = ResetPos + new Vector3(0,1,0);
        pos_end = ResetPos;
    }

    void Update()
    {
        // be turned on the switch
        if(!flg && Anim.GetCurrentAnimatorStateInfo(0).fullPathHash == TurnOnState)
        {
            flg = true;
        }
        if(flg)
        {
            t += 3 * Time.deltaTime;
            // movement this object
            if(t <= 1)
            {
                this.transform.rotation = ResetRot;
                this.transform.position = Vector3.Lerp(pos_start, pos_end, t);
            }
            // be turned off the switch
            if(t > 1 && Anim.GetCurrentAnimatorStateInfo(0).fullPathHash != TurnOnState)
            {
                flg = false;
                t = 0;
            }
        }
    }
}
}