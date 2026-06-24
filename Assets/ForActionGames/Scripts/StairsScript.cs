using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Sample {
public class StairsScript : MonoBehaviour
{
    [SerializeField] private GameObject Effect;
    [SerializeField] private Animator Anim;
    private static readonly int OpenState = Animator.StringToHash("Base Layer.open");
    private static readonly int CloseState = Animator.StringToHash("Base Layer.close");

    private PublicValueScript _Value;

    void Start ()
    {
        Effect.gameObject.SetActive(false);

        _Value = FindObjectOfType<PublicValueScript>();
    }
    // opening the door
    void OnTriggerEnter(Collider other)
    {
        if(other.gameObject.tag == "Player")
        {
            _Value.Clear_Public = true;
            
            if(Anim.GetCurrentAnimatorStateInfo(0).fullPathHash != OpenState)
            {
                Anim.CrossFade(OpenState, 0.1f, 0, 0);
                Invoke("EffectActive",0.2f);
            }
        }
    }
    // spawn a effect
    private void EffectActive ()
    {
        Effect.gameObject.SetActive(true);
    }
}
}