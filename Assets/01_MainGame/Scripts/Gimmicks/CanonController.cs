using UnityEngine;

public class CanonController : MonoBehaviour
{
    [SerializeField] private float fireRate = 3f;
    private float nextFireTime;
    [SerializeField] private GameObject bulletPrefab;

    [SerializeField] private CanonCollider cannonCollider;

    [SerializeField] private Transform firePoint;

    //大砲が出す音
    [SerializeField] private AudioSource cannonAudioSource;

    [SerializeField] private Transform playerTransform;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        nextFireTime = 0f;
    }

    // Update is called once per frame
    void Update()
    {
        
    
        if (cannonCollider.isBulletMode)
        {
            Vector3 directionToPlayer = playerTransform.position;
            directionToPlayer.y=this.gameObject.transform.position.y;
            //プレイヤーの向きに大砲を向ける
            this.gameObject.transform.LookAt(directionToPlayer);



            if (fireRate<=nextFireTime)
            {
                cannonAudioSource.Play();
                GameObject.Instantiate(bulletPrefab, firePoint.transform.position, firePoint.transform.rotation);
                nextFireTime = 0f;
            }

            if(nextFireTime < fireRate) {
                nextFireTime += Time.deltaTime;
            }


        }
    }
}
