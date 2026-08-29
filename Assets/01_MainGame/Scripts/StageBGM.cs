using UnityEngine;

public class StageBGM : MonoBehaviour
{
    [SerializeField] private AudioSource stageBGM;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        stageBGM.Play();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void StopStageBGM()
    {
        if (stageBGM.isPlaying)
        {
            stageBGM.Stop();
        }
    }

    public void PauseStageBGM()
    {
        if (stageBGM.isPlaying)
        {
            stageBGM.Pause();
        }
    }

    public void PlayStageBGM()
    {
        if (!stageBGM.isPlaying)
        {
            stageBGM.Play();
        }
    }

    public void SetStageBGMVolume(float volume)
    {
        stageBGM.volume = Mathf.Clamp01(volume);
    }
}
