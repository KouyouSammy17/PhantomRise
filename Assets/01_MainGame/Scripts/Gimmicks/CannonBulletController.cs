using UnityEngine;

public class CannonBulletController : MonoBehaviour
{
    [SerializeField] private float speed = 10f;

    [SerializeField] private int damage = 10;

    //爆発エフェクト
    [SerializeField] private GameObject explosionEffect;

    [SerializeField] private AudioSource bulletAudioSource;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        Destroy(this.gameObject, 3f);

        transform.Translate(Vector3.forward * speed * Time.deltaTime);
    }


    public void OnTriggerEnter(Collider other)
    {
        GameObject.Instantiate(explosionEffect, transform.position, Quaternion.identity);

        bulletAudioSource.Play();

        if (other.CompareTag("Player"))
        {
            Debug.Log("Bullet hit the player!");
            other.GetComponent<PlayerHP>()?.TakeDamage(damage);
            // Ghost状態のプレイヤーに当たった場合の処理
            var machine =
                   other.GetComponent<PlayerStateMachine>();

            if (machine != null &&
                machine.CurrentStateName == nameof(GhostState))
            {
                // Ghost状態で針に触れたら即ゲームオーバー
                machine.Ghost.OnHit();
            }
            Destroy(this.gameObject);
        }
        else if (other.CompareTag("Enemy"))
        {
            Debug.Log("Bullet hit an enemy!");
            other.GetComponent<EnemyHealth>()?.TakeDamage(damage);
            Destroy(this.gameObject);
        }
        else
        {
            Debug.Log("Bullet hit something else!");
            Destroy(this.gameObject);
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        Destroy(this.gameObject);
    }
}
