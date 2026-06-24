using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Sample {
public class SwitchLeverScript : MonoBehaviour
{
    Animator anim;
    private static readonly int TurnOnState = Animator.StringToHash("Base Layer.turnon");
    private static readonly int TurnOffState = Animator.StringToHash("Base Layer.turnoff");
    private static readonly int LRParameter = Animator.StringToHash("LR");
    private bool LeverOn = false;
    // Color 
    [SerializeField] private Color LeverColor = new Color(0.7f,0,0,1);
    [SerializeField] private SkinnedMeshRenderer Mesh;
    // delay
    [SerializeField] private float OffDelayTime = 3;
    [SerializeField] private bool OffDelayTimeFlg = false;

    void Start()
    {
        anim = this.GetComponent<Animator>();
        // change color
        Mesh.material.SetColor("_VariationColor", LeverColor);
    }

    // turn off lever
    private void TurnOff ()
    {
        anim.CrossFade(TurnOffState,0.1f,0,0);
        LeverOn = false;
    }
    
    void OnTriggerEnter(Collider other)
    {
        // turn on lever
        if(other.gameObject.name == "AttackEffect" && !LeverOn)
        {
            anim.CrossFade(TurnOnState,0.1f,0,0);
            if(!OffDelayTimeFlg)
            {
                if(anim.GetFloat(LRParameter) == 0)
                {
                    anim.SetFloat(LRParameter, 1);
                }
                else if(anim.GetFloat(LRParameter) == 1)
                {
                    anim.SetFloat(LRParameter, 0);
                }
            }
            LeverOn = true;
            Invoke("TurnOff", OffDelayTime);
        }
    }
}
}