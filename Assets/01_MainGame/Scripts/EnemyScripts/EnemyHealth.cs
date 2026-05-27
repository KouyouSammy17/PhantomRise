using System.Collections;
using UnityEngine;

public class EnemyHealth : MonoBehaviour
{
    [SerializeField] private int _maxHP = 100;
    public int maxHP => _maxHP;
    private int currentHP;

    //無敵状態のフラグ
    private bool invincible = false;

    private EnemyController enemyController;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        currentHP = maxHP;

        enemyController = GetComponent<EnemyController>();
    }

    // 外から読み取りだけ可能
    //現在のHPを取得するプロパティ
    public int CurrentHP
    {
        get { return currentHP; }
    }

    //無敵状態のフラグを取得するプロパティ
    public bool Invincible
    {
        get { return invincible; }
    }   


    //HPバーに表示する敵のHP
    public float HPRatio
    {
        get { return (float)currentHP / maxHP; }
    }

    // Update is called once per frame
    void Update()
    {
      
    }

    public void TakeDamage(int damage)
    {
        if (invincible == false)
        {
            currentHP = Mathf.Max(0, currentHP - damage);
        }
        Debug.Log("敵がダメージを受けました！現在のHP: " + currentHP);

        //敵がプレイヤーからダメージを受けたらチェイスモードに移行する
         enemyController?.AlertDamage();
    }

    //無敵状態
    public IEnumerator InvincibleTime(float time)
    {
        invincible = true;

        //hpが0にならないようにmaxhpの10分の一のhpを回復させる
        currentHP = Mathf.Max(currentHP, _maxHP / 10);

        Debug.Log("無敵開始");

        yield return new WaitForSeconds(time);

        invincible = false;

        Debug.Log("無敵終了");
    }
}
