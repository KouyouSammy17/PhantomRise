using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Sample {
public class TileMoveScript : MonoBehaviour
{
    private bool Move_flg = false;
    private GameObject Player;
    private CharacterController Ctrl;
    private Animator Anim;
    // this speed to move a character
    [SerializeField] private float Moving_Speed = 1;
    // time to move a character
    [SerializeField] private float Moving_Time = 1;
    // Cache hash values
    private static readonly int SurprisedState = Animator.StringToHash("Base Layer.surprised");
    
    // moving the player
    void Update()
    {
        if(Move_flg)
        {
            Vector3 velocity = this.transform.rotation * new Vector3(0,0,Moving_Speed);
            Vector3 MoveDirection = new Vector3 (velocity.x, 0, velocity.z);
            if(Ctrl.enabled)
            {
                Ctrl.Move(MoveDirection * Time.deltaTime);
            }
            MoveDirection.x = 0;
            MoveDirection.z = 0;
            Player.transform.rotation = this.transform.rotation;
        }
    }
    // touch this object
    void OnTriggerEnter(Collider other)
    {
        if(other.gameObject.tag == "Player")
        {
            Player = other.gameObject;
            Ctrl = Player.GetComponent<CharacterController>();
            Anim = Player.GetComponent<Animator>();
            Anim.CrossFade(SurprisedState, 0.1f, 0, 0);
            Move_flg = true;
            Invoke("StopMoving", Moving_Time);
        }
    }
    private void StopMoving ()
    {
        Move_flg = false;
    }
}
}