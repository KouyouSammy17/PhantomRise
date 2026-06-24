using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Sample {
public class MoveBlockScript : MonoBehaviour
{
    private PublicValueScript _Value;
    private bool Move = false;
    private float t = 0;
    private Vector3 pos_start;
    private Vector3 pos_end;

    void Start ()
    {
        _Value = FindObjectOfType<PublicValueScript>();
    }
    // moving this block
    void Update ()
    {
        if(Move)
        {
            t += 3 * Time.deltaTime;
            this.transform.position = Vector3.Lerp(pos_start, pos_end, t);
            if(t > 1)
            {
                Move = false;
                t = 0;
            }
        }
    }
    // pushed this block
    void OnTriggerEnter(Collider other)
    {
        if(other.gameObject.name == "AttackEffect" && !Move)
        {
            // get position
            pos_start = this.transform.position;
            float offsetz = pos_start.z - _Value.Player_Public.transform.position.z;
            float offsetx = pos_start.x - _Value.Player_Public.transform.position.x;
            // distance
            float disz = offsetz * offsetz;
            float disx = offsetx * offsetx;
            // Direction of movement
            if(disz > disx)
            {
                if(offsetz > 0)
                {
                    pos_end = this.transform.position + new Vector3(0,0,1);
                }
                else if(offsetz < 0)
                {
                    pos_end = this.transform.position + new Vector3(0,0,-1);
                }
            }
            if(disz < disx)
            {
                if(offsetx > 0)
                {
                    pos_end = this.transform.position + new Vector3(1,0,0);
                }
                else if(offsetx < 0)
                {
                    pos_end = this.transform.position + new Vector3(-1,0,0);
                }
            }
            // starting movement
            Move = true;
        }
    }
}
}