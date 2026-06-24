using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Sample {
public class PortalScript : MonoBehaviour
{
    [SerializeField] private Vector3 Destination = Vector3.zero;
    // movement by this portal
    void OnTriggerEnter(Collider other)
    {
        if(other.gameObject.tag == "Player")
        {
            GameObject player = other.gameObject;
            CharacterController ctrl = player.GetComponent<CharacterController>();
            ctrl.enabled = false;
            player.transform.position = Destination;
            ctrl.enabled = true;
        }
    }
}
}