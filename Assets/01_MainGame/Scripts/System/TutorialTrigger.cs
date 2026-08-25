using UnityEngine;
using UnityEngine.InputSystem;

public class TutorialTrigger : MonoBehaviour
{
    [SerializeField] private GameObject[] panels;
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip enterSound;

    private bool isShown = false;
    private int currentIndex = 0;
    private bool isTutorialActive = false;

    private void Start()
    {
        foreach (GameObject panel in panels)
        {
            panel.SetActive(false);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (isShown) return;

        if (other.CompareTag("Player"))
        {
            isShown = true;
            isTutorialActive = true;
            currentIndex = 0;

            panels[currentIndex].SetActive(true);
            Time.timeScale = 0f;
        }
    }

    private void Update()
    {
        if (!isTutorialActive) return;

        if (Keyboard.current.enterKey.wasPressedThisFrame)
        {
            // 効果音を鳴らす
            if (audioSource != null && enterSound != null)
            {
                audioSource.PlayOneShot(enterSound);
            }

            panels[currentIndex].SetActive(false);

            currentIndex++;

            if (currentIndex < panels.Length)
            {
                panels[currentIndex].SetActive(true);
            }
            else
            {
                isTutorialActive = false;
                Time.timeScale = 1f;
            }
        }
    }
}
