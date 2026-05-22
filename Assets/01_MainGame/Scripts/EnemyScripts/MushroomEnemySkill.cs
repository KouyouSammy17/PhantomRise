using UnityEngine;

public class MushroomEnemySkill : MonoBehaviour
{

    [Header("Mushroom")]
    public GameObject poisonPrefab;


    //敵のスキルクールダウン
    private float skillCooldown = 5f;
    private float skillTimer = 0f;

    //スキルを使用したかどうか
    //攻撃とスキルが重ならないようにするためのフラグ
    public bool usedskill = false; 

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        skillCooldown = 10f;
        skillTimer = 10f; // 最初からスキルが溜まっている状態にする
    }

    // Update is called once per frame
    void Update()
    {
        if (skillTimer <= skillCooldown)
        {
            skillTimer += Time.deltaTime;
        }
    }

    public void TryUseSkill()
    {
        // クールダウン中
        if (skillTimer < skillCooldown)
            return;

        UseSkill();

        skillTimer = 0f;
    }


    public void UseSkill()
    {
        //攻撃内容
        Instantiate(poisonPrefab, transform.position, transform.rotation);
        Debug.Log("キノコのスキル！毒を与える！");
        skillTimer = 0f;
        usedskill = true;
    }


}
