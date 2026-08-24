using TMPro;
using UnityEngine;

public class EnemyRankUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI EnemyRankText;

    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
       
    }

    public void ShowRank(EnemyController enemy)
    {
        if (enemy == null)
        {
            EnemyRankText.text = "";
            return;
        }


        EnemyRankText.text = $"{enemy.Rank}";
    }

    public void HideRank()
    {
        EnemyRankText.text = "";
    }
}
