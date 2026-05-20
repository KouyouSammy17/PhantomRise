using UnityEngine;

public class SpiderThreadMove : MonoBehaviour
{
    private float speed = 10f;
    private Rigidbody rb;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

        rb = GetComponent<Rigidbody>();
        //移動
        rb.linearVelocity = transform.forward * speed;
    }

    // Update is called once per frame
    void Update()
    {



    }

    //プレイヤーに当たったら消える
    //private void OnTriggerEnter(Collider other)
    //{
    //    // ぶつかったオブジェクトのタグが "Player" かどうかを判定
    //    if (other.CompareTag("Player"))
    //    {
    //        // ここにプレイヤーへの拘束効果


    //        // 自分自身（糸のオブジェクト）を消去する
    //        Destroy(gameObject);
    //    }
    //}

    private void OnCollisionEnter(Collision collision)
    {
        // ぶつかったオブジェクトのタグが "Player" かどうかを判定
        if (collision.gameObject.CompareTag("Player"))
        {
            // ここにプレイヤーへの拘束効果

            // 自分自身（糸のオブジェクト）を消去する
            Destroy(gameObject);
        }
    }
}