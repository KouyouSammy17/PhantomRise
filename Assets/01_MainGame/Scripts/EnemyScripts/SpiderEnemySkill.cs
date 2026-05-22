using UnityEngine;
using static EnemyController;

public class SpiderEnemySkill : MonoBehaviour
{

    [Header("Spider")]
    public GameObject spiderWebPrefab;
    public Transform webSpawnPoint;

    //敵のスキルクールダウン
    private float skillCooldown = 5f;
    private float skillTimer = 0f;

    //スキルを使用したかどうか
    //攻撃とスキルが重ならないようにするためのフラグ
    public bool usedskill = false;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        skillCooldown = 8f;
        skillTimer = 8f; // 最初からスキルが溜まっている状態にする
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
            Instantiate(spiderWebPrefab, webSpawnPoint.position, webSpawnPoint.transform.rotation);
            Debug.Log("スパイダーのスキル！糸を飛ばす！");
            skillTimer = 0f;
            usedskill = true;
    }




}
