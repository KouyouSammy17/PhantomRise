using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Sample {
public class RespawnScript : MonoBehaviour
{
    private PublicValueScript _Value;
    private CharacterController Ctrl;
    // respawn position
    [SerializeField] private Vector3 RespawnPos;
    // respawn facing
    [SerializeField] private Vector3 RespawnRot;
    
    void Start()
    {
        _Value = FindObjectOfType<PublicValueScript>();
        Ctrl = _Value.Player_Public.GetComponent<CharacterController>();
    }
    void Update()
    {
        // Respawn
        if(Input.GetKeyDown(KeyCode.Space) && _Value.HP_Public <= 0 && _Value.RespawnFlg_Public)
        {
            // player HP
            _Value.HP_Public = _Value.MaxHP_Public;
            // display the player
            _Value.Player_Public.gameObject.SetActive(true);
            // player position
            _Value.Player_Public.transform.position = RespawnPos;
            // player facing
            _Value.Player_Public.transform.rotation = Quaternion.Euler(RespawnRot);
            // CharacterController
            Ctrl.enabled = true;
            // flag
            _Value.RespawnFlg_Public = false;
        }
    }
}
}