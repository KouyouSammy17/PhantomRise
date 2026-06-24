using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Sample {
public class PlayerScript : MonoBehaviour
{
    private Animator Anim;
    [SerializeField] private GameObject Exclamation;
    private Animator Exclamation_Anim;
    [SerializeField] private GameObject Question;
    private Animator Question_Anim;
    private CharacterController Ctrl;
    private Vector3 MoveDirection = Vector3.zero;
    private GameObject AttackCollider;
    // Cache hash values
    private static readonly int IdleState = Animator.StringToHash("Base Layer.idle");
    private static readonly int MoveState = Animator.StringToHash("Base Layer.move");
    private static readonly int SurprisedState = Animator.StringToHash("Base Layer.surprised");
    private static readonly int Attack1State = Animator.StringToHash("Base Layer.attack1_shift");
    private static readonly int Attack2State = Animator.StringToHash("Base Layer.attack2_shift");
    private static readonly int Attack3State = Animator.StringToHash("Base Layer.attack3_shift");
    private static readonly int DissolveState = Animator.StringToHash("Base Layer.dissolve");
    private static readonly int OpenState = Animator.StringToHash("Base Layer.open");
    private static readonly int AttackTag = Animator.StringToHash("Attack");
    
    // dissolve
    [SerializeField] private SkinnedMeshRenderer[] MeshR;
    private float Dissolve_value = 1;
    private bool DissolveFlg = false;
    private GameObject DissolveEffect;
    // public value
    private PublicValueScript _Value;
    // interval for using item
    private bool ItemUsingFlg = false;
    private GameObject ItemCollider;
    // moving speed
    [SerializeField] private float Speed = 4;


    void Start()
    {
        this.gameObject.tag = "Player";
        Anim = this.GetComponent<Animator>();
        Ctrl = this.GetComponent<CharacterController>();
        Exclamation_Anim = Exclamation.GetComponent<Animator>();
        Exclamation.gameObject.SetActive(false);
        Question_Anim = Exclamation.GetComponent<Animator>();
        Question.gameObject.SetActive(false);

        AttackCollider = Resources.Load<GameObject>("Prefabs/Collider/SphereCollider");
        ItemCollider = Resources.Load<GameObject>("Prefabs/Collider/SphereCollider2");

        _Value = FindObjectOfType<PublicValueScript>();
        DissolveEffect = Resources.Load<GameObject>("Prefabs/Effects/DissolveEffect");

        _Value.Player_Public = this.gameObject;
    }

    void Update()
    {
        STATUS();
        GRAVITY();
        // this character status
        if(!PlayerStatus.ContainsValue( true ))
        {
            MOVE();
            PlayerAttack();
        }
        else if(PlayerStatus.ContainsValue( true ))
        {
            int status_name = 0;
            foreach(var i in PlayerStatus)
            {
                if(i.Value == true)
                {
                    status_name = i.Key;
                    break;
                }
            }
            if(status_name == Dissolve)
            {
                PlayerDissolve();
            }
            else if(status_name == Attack)
            {
                PlayerAttack();
            }
            else if(status_name == Surprised)
            {
                // nothing method
            }
        }
        // using a item
        if(Input.GetKeyDown(KeyCode.S) && !ItemUsingFlg && _Value.HaveKeyNum_Public > 0)
        {
            Invoke("UsingItem", 1);
            ItemColliderInstantiate();
            ItemUsingFlg = true;
        }
        // Dissolve
        if(_Value.HP_Public <= 0 && !DissolveFlg)
        {
            Anim.CrossFade(DissolveState, 0.1f, 0, 0);
            GameObject obj = Instantiate(DissolveEffect);
            obj.transform.position = this.transform.position;
            DissolveFlg = true;
        }
        // processing at respawn
        else if(_Value.HP_Public == _Value.MaxHP_Public && DissolveFlg)
        {
            DissolveFlg = false;
            QuestionSpawn();
        }
    }
    //---------------------------------------------------------------------
    // character status
    //---------------------------------------------------------------------
    private const int Dissolve = 1;
    private const int Attack = 2;
    private const int Surprised = 3;
    private Dictionary<int, bool> PlayerStatus = new Dictionary<int, bool>
    {
        {Dissolve, false },
        {Attack, false },
        {Surprised, false },
    };
    //------------------------------
    private void STATUS ()
    {
        // during dissolve
        if(DissolveFlg &&_Value.HP_Public <= 0)
        {
            PlayerStatus[Dissolve] = true;
        }
        else if(!DissolveFlg)
        {
            PlayerStatus[Dissolve] = false;
        }
        // during attacking
        if(Anim.GetCurrentAnimatorStateInfo(0).tagHash == AttackTag)
        {
            PlayerStatus[Attack] = true;
        }
        else if(Anim.GetCurrentAnimatorStateInfo(0).tagHash != AttackTag)
        {
            PlayerStatus[Attack] = false;
        }
        // during damaging
        if(Anim.GetCurrentAnimatorStateInfo(0).fullPathHash == SurprisedState)
        {
            PlayerStatus[Surprised] = true;
        }
        else if(Anim.GetCurrentAnimatorStateInfo(0).fullPathHash != SurprisedState)
        {
            PlayerStatus[Surprised] = false;
        }
    }
    //--------------------------------------------------
    // character's animation & effects
    //--------------------------------------------------
    // spawn emotion effects
    private void QuestionSpawn ()
    {
        Question.gameObject.SetActive(true);
        Question.transform.localPosition = new Vector3(0, 1.2f, 0); //reset position
        Invoke("Hide", 1.1f);
    }
    private void ExclamationSpawn ()
    {
        Exclamation.gameObject.SetActive(true);
        Exclamation.transform.localPosition = new Vector3(0, 1.2f, 0); //reset position
        Invoke("Hide", 1.1f);
    }
    // hide emotion effect
    private void Hide ()
    {
        Exclamation.gameObject.SetActive(false);
        Question.gameObject.SetActive(false);
    }
    // dissolve shading
    private void PlayerDissolve ()
    {
        Dissolve_value -= Time.deltaTime;
        for(int i = 0; i < MeshR.Length; i++)
        {
            MeshR[i].material.SetFloat("_Dissolve", Dissolve_value);
        }
        if(Dissolve_value <= 0)
        {
            Dissolve_value = 1;
            for(int i = 0; i < MeshR.Length; i++)
            {
                MeshR[i].material.SetFloat("_Dissolve", Dissolve_value);
            }
            this.gameObject.SetActive(false);
            Ctrl.enabled = false;
            _Value.RespawnFlg_Public = true;
        }
    }
    // play a animation of Attack
    private void PlayerAttack ()
    {
        if(Input.GetKeyDown(KeyCode.A))
        {
            if(Input.GetKey(KeyCode.LeftShift))
            {
                Anim.CrossFade(Attack2State,0.1f,0,0);
            }
            else if(Input.GetKey(KeyCode.LeftAlt))
            {
                Anim.CrossFade(Attack3State,0.1f,0,0);
            }
            else
            {
                Anim.CrossFade(Attack1State,0.1f,0,0);
            }
            //Invoke("AttackColliderInstantiate", 0.2f);
        }
    }
    // collider of attack
    /*
    private void AttackColliderInstantiate ()
    {
        GameObject obj = Instantiate(AttackCollider);
        obj.transform.position = this.transform.position + this.transform.rotation * new Vector3(0, 0.5f, 1);
        obj.name = "AttackCollider";
    }//*/

    // interval for using item
    private void UsingItem ()
    {
        ItemUsingFlg = false;
    }
    // collider of item
    private void ItemColliderInstantiate ()
    {
        GameObject obj = Instantiate(ItemCollider);
        obj.transform.position = this.transform.position + this.transform.rotation * new Vector3(0, 0.5f, 0);
        obj.name = "ItemCollider";
    }
    //---------------------------------------------------------------------
    // gravity for fall of this character
    //---------------------------------------------------------------------
    private void GRAVITY ()
    {
        if(Ctrl.enabled)
        {
            if(CheckGrounded())
            {
                if(MoveDirection.y < -0.1f)
                {
                    MoveDirection.y = -0.1f;
                }
            }
            MoveDirection.y -= 0.1f;
            Ctrl.Move(MoveDirection * Time.deltaTime);
        }
    }
    //---------------------------------------------------------------------
    // whether it is grounded
    //---------------------------------------------------------------------
    private bool CheckGrounded()
    {
        if (Ctrl.isGrounded && Ctrl.enabled)
        {
            return true;
        }
        Ray ray = new Ray(this.transform.position + Vector3.up * 0.1f, Vector3.down);
        float range = 0.2f;
        return Physics.Raycast(ray, range);
    }
    //---------------------------------------------------------------------
    // for slime moving
    //---------------------------------------------------------------------
    private void MOVE ()
    {
        // velocity
        if(Anim.GetCurrentAnimatorStateInfo(0).fullPathHash == MoveState)
        {
            if (Input.GetKey(KeyCode.UpArrow) && !Input.GetKey(KeyCode.DownArrow) && !Input.GetKey(KeyCode.LeftArrow) && !Input.GetKey(KeyCode.RightArrow))
            {
                MOVE_Velocity(new Vector3(0, 0, -Speed), new Vector3(0, 180, 0));
            }
            else if (Input.GetKey(KeyCode.DownArrow) && !Input.GetKey(KeyCode.UpArrow) && !Input.GetKey(KeyCode.LeftArrow) && !Input.GetKey(KeyCode.RightArrow))
            {
                MOVE_Velocity(new Vector3(0, 0, Speed), new Vector3(0, 0, 0));
            }
            else if (Input.GetKey(KeyCode.LeftArrow) && !Input.GetKey(KeyCode.UpArrow) && !Input.GetKey(KeyCode.DownArrow) && !Input.GetKey(KeyCode.RightArrow))
            {
                MOVE_Velocity(new Vector3(Speed, 0, 0), new Vector3(0, 90, 0));
            }
            else if (Input.GetKey(KeyCode.RightArrow) && !Input.GetKey(KeyCode.UpArrow) && !Input.GetKey(KeyCode.DownArrow) && !Input.GetKey(KeyCode.LeftArrow))
            {
                MOVE_Velocity(new Vector3(-Speed, 0, 0), new Vector3(0, 270, 0));
            }
        }
        KEY_DOWN();
        KEY_UP();
    }
    //---------------------------------------------------------------------
    // value for moving
    //---------------------------------------------------------------------
    private void MOVE_Velocity (Vector3 velocity, Vector3 rot)
    {
        MoveDirection = new Vector3 (velocity.x, MoveDirection.y, velocity.z);
        if(Ctrl.enabled)
        {
            Ctrl.Move(MoveDirection * Time.deltaTime);
        }
        MoveDirection.x = 0;
        MoveDirection.z = 0;
        this.transform.rotation = Quaternion.Euler(rot);
    }
    //---------------------------------------------------------------------
    // whether arrow key is key down
    //---------------------------------------------------------------------
    private void KEY_DOWN ()
    {
        if (Input.GetKeyDown(KeyCode.UpArrow))
        {
            Anim.CrossFade(MoveState, 0.1f, 0, 0);
        }
        else if (Input.GetKeyDown(KeyCode.DownArrow))
        {
            Anim.CrossFade(MoveState, 0.1f, 0, 0);
        }
        else if (Input.GetKeyDown(KeyCode.LeftArrow))
        {
            Anim.CrossFade(MoveState, 0.1f, 0, 0);
        }
        else if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            Anim.CrossFade(MoveState, 0.1f, 0, 0);
        }
    }
    //---------------------------------------------------------------------
    // whether arrow key is key up
    //---------------------------------------------------------------------
    private void KEY_UP ()
    {
        if (Input.GetKeyUp(KeyCode.UpArrow))
        {
            if(!Input.GetKey(KeyCode.DownArrow) && !Input.GetKey(KeyCode.LeftArrow) && !Input.GetKey(KeyCode.RightArrow))
            {
                Anim.CrossFade(IdleState, 0.1f, 0, 0);
            }
        }
        else if (Input.GetKeyUp(KeyCode.DownArrow))
        {
            if(!Input.GetKey(KeyCode.UpArrow) && !Input.GetKey(KeyCode.LeftArrow) && !Input.GetKey(KeyCode.RightArrow))
            {
                Anim.CrossFade(IdleState, 0.1f, 0, 0);
            }
        }
        else if (Input.GetKeyUp(KeyCode.LeftArrow))
        {
            if(!Input.GetKey(KeyCode.UpArrow) && !Input.GetKey(KeyCode.DownArrow) && !Input.GetKey(KeyCode.RightArrow))
            {
                Anim.CrossFade(IdleState, 0.1f, 0, 0);
            }
        }
        else if (Input.GetKeyUp(KeyCode.RightArrow))
        {
            if(!Input.GetKey(KeyCode.UpArrow) && !Input.GetKey(KeyCode.DownArrow) && !Input.GetKey(KeyCode.LeftArrow))
            {
                Anim.CrossFade(IdleState, 0.1f, 0, 0);
            }
        }
    }
    //---------------------------------------------------------------------
    // Trigger
    //---------------------------------------------------------------------
    void OnTriggerEnter(Collider other)
    {
        // Damaged by traps.
        if(other.gameObject.name.Length >= 10)
        {
            if(other.gameObject.name.Substring(0, 10) == "TileNeedle")
            {
                Anim.CrossFade(SurprisedState, 0.1f, 0, 0);
                ExclamationSpawn();
                Damage(1);
            }
        }
        // Damaged by outside field.
        else if(other.gameObject.name.Substring(0, 2) == "BG")
        {
            Anim.CrossFade(SurprisedState, 0.1f, 0, 0);
            ExclamationSpawn();
            Damage(3);
            Ctrl.enabled = false;
        }
    }
    //---------------------------------------------------------------------
    // Damage
    //---------------------------------------------------------------------
    private void Damage (int damage)
    {
        if(_Value.HP_Public > 0)
        {
            _Value.HP_Public -= damage;
        }
    }
}
}