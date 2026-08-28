using TMPro;
using UnityEngine;
using static EnemyController;

public class EnemyRankUI : MonoBehaviour
{
   
    [SerializeField] private GameObject RankA;
    [SerializeField] private GameObject RankB;
    [SerializeField] private GameObject RankC;
    [SerializeField] private GameObject RankD;

    public void ShowRank(EnemyController enemy)
    {
        if (enemy == null)
        {
            HideRank();
            return;
        }

        // まず全部非表示
        RankA.SetActive(false);
        RankB.SetActive(false);
        RankC.SetActive(false);
        RankD.SetActive(false);

        // 敵のランクを表示
        //EnemyRankText.text = $"{enemy.Rank}";

        // ランクに応じて対応するアイコンだけ表示
        switch (enemy.Rank)
        {
            case EnemyRank.A:
                RankA.SetActive(true);
                break;

            case EnemyRank.B:
                RankB.SetActive(true);
                break;

            case EnemyRank.C:
                RankC.SetActive(true);
                break;

            case EnemyRank.D:
                RankD.SetActive(true);
                break;
        }
    }

    public void HideRank()
    {
       // EnemyRankText.text = "";

        RankA.SetActive(false);
        RankB.SetActive(false);
        RankC.SetActive(false);
        RankD.SetActive(false);
    }
}