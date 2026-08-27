using System;
using UnityEngine;

public class EnemyAudio : MonoBehaviour
{
    [SerializeField] private AudioSource e_AudioSource;

    [SerializeField] private AudioClip eAttackSE;
    [SerializeField] private AudioClip eSkillSE;
    [SerializeField] private AudioClip eDeathSE;
    //[SerializeField] private AudioClip emovesSE;
    [SerializeField] private AudioClip eHitSE;
    [SerializeField] private AudioClip eQTEFailureSE;
    [SerializeField] private AudioClip eQTESuccessSE;
    //[SerializeField] private AudioClip eTakeOverSE;



    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void PlayAttackSE()
    {
        e_AudioSource.PlayOneShot(eAttackSE);
    }
    public void PlaySkillSE()
    {
        e_AudioSource.PlayOneShot(eSkillSE);
    }

    public void PlayDeathSE()
    {
        e_AudioSource.PlayOneShot(eDeathSE);
    }

    //public void PlayMovesSE()
    //{
    //    e_AudioSource.PlayOneShot(emovesSE);
    //}

    public void PlayHitSE()
    {
        e_AudioSource.PlayOneShot(eHitSE);
    }


    public void PlayQTEFSE()
    {
        e_AudioSource.PlayOneShot(eQTEFailureSE);
    }

    public void PlayQTESSE()
    {
        e_AudioSource.PlayOneShot(eQTESuccessSE);
    }
    
    //public void PlayTakeOverSE()
    //{
    //    e_AudioSource.PlayOneShot(eTakeOverSE);
    //}



}
