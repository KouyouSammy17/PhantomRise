using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Sample {
public class WallScript : MonoBehaviour
{
    [SerializeField] private Color WallColor = new Color(0.7f,0,0,1);
    [SerializeField] private GameObject Button;
    [SerializeField] private float OpenDelayTime = 0;
    private Animator ButtonAnim;
    private Animator Anim;
    // Cache hash values
    private static readonly int TurnOnState = Animator.StringToHash("Base Layer.turnon");
    private static readonly int TurnOffState = Animator.StringToHash("Base Layer.turnoff");
    private static readonly int DefaultState = Animator.StringToHash("Base Layer.default");
    private static readonly int OpenState = Animator.StringToHash("Base Layer.open");
    private bool WallOpen = false;
    private BoxCollider Col;

    void Start()
    {
        SkinnedMeshRenderer mesh = this.transform.Find("Wall").gameObject.GetComponent<SkinnedMeshRenderer>();
        mesh.material.SetColor("_VariationColor", WallColor);
        ButtonAnim = Button.GetComponent<Animator>();
        Anim = this.GetComponent<Animator>();
        Col = this.GetComponent<BoxCollider>();
    }

    void Update()
    {
        if(ButtonAnim.GetCurrentAnimatorStateInfo(0).fullPathHash == TurnOnState && !WallOpen)
        {
            Invoke("Open",OpenDelayTime);
            WallOpen = true;
            Col.enabled = false;
        }
        else if(ButtonAnim.GetCurrentAnimatorStateInfo(0).fullPathHash == TurnOffState && WallOpen)
        {
            Anim.CrossFade(DefaultState,0.1f,0,0);
            WallOpen = false;
            Col.enabled = true;
        }
    }
    private void Open ()
    {
        Anim.CrossFade(OpenState,0.1f,0,0);
    }
}
}