using UnityEngine;

public class CanonCollider : MonoBehaviour
{
    [SerializeField] private SphereCollider sphereCollider;

    [SerializeField] private float colliderDuration = 5f;

    [SerializeField] private bool isBulletmode = false;

    public bool isBulletMode => isBulletmode;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        isBulletmode = false;
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void OnTriggerStay(Collider other)
    {
        if (other.CompareTag("Player"))
        {
     
            isBulletmode = true;
       
        }
    }

    public void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isBulletmode = false;
        }
    }
}
