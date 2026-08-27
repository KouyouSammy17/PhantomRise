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
}
