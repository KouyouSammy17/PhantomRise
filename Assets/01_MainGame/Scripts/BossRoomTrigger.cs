using Unity.VisualScripting;
using UnityEngine;

public class BossRoomTrigger : MonoBehaviour
{
    [SerializeField] private GameObject BossUI;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        BossUI.SetActive(false);
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            BossUI.SetActive(true);
        }
    }
}
