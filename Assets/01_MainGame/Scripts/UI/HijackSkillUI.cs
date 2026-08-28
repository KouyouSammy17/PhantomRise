using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class HijackSkillUI : MonoBehaviour
{
    [SerializeField]
    private GameObject panel;

    [SerializeField]
    private TMP_Text skillNameText;

    [SerializeField]
    private TMP_Text descriptionText;


    private HashSet<string> learnedSkills = new HashSet<string>();

    private Coroutine hideCoroutine;

    public void ShowSkill(EnemySkillBase skill)
    {
        // すでに取得済みなら表示しない
        if (learnedSkills.Contains(skill.SkillID))
            return;


        learnedSkills.Add(skill.SkillID);


        panel.SetActive(true);

        //skillNameText.text = skill.SkillName;
        descriptionText.text = skill.SkillDescription;

        if (hideCoroutine != null)
            StopCoroutine(hideCoroutine);

       hideCoroutine=  StartCoroutine(HideRoutine());
    }


    IEnumerator HideRoutine()
    {
        yield return new WaitForSecondsRealtime(3f);

        skillNameText.text = "";
        descriptionText.text = "";

        panel.SetActive(false);
    }
}
