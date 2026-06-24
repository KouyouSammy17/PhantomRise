using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Sample {
public class SwitchButtonScript : MonoBehaviour
{
    Animator anim;
    private static readonly int TurnOnState = Animator.StringToHash("Base Layer.turnon");
    private static readonly int TurnOffState = Animator.StringToHash("Base Layer.turnoff");
    private bool ButtonOn = false;
    // Color
    [SerializeField] private Color ButtonColor = new Color(0.7f,0,0,1);
    [SerializeField] private SkinnedMeshRenderer Mesh;
    // delay
    [SerializeField] private float OffDelayTime = 0;
    // Objects that be able to contact with the button.
    [SerializeField] private GameObject[] ContactableObjects;

    void Start()
    {
        anim = this.GetComponent<Animator>();
        // change color
        Mesh.material.SetColor("_VariationColor", ButtonColor);
    }

    // turn on
    void OnTriggerEnter(Collider other)
    {
        foreach(var i in ContactableObjects)
        {
            if(other.gameObject == i)
            {
                if(!ButtonOn && anim.GetCurrentAnimatorStateInfo(0).fullPathHash != TurnOnState)
                {
                    ButtonOn = true;
                    anim.CrossFade(TurnOnState,0.1f,0,0);
                }
                break;
            }
        }
    }

    // turn off
    void OnTriggerExit(Collider other)
    {
        foreach(var i in ContactableObjects)
        {
            if(other.gameObject == i)
            {
                if(ButtonOn && anim.GetCurrentAnimatorStateInfo(0).fullPathHash != TurnOffState)
                {
                    ButtonOn = false;
                    Invoke("TurnOff", OffDelayTime);
                }
                break;
            }
        }
    }
    // play a animation of to turn off
    private void TurnOff ()
    {
        anim.CrossFade(TurnOffState,0.1f,0,0);
    }
}
}