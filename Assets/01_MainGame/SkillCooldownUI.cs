using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class SkillCooldownUI : MonoBehaviour
{
    [SerializeField] private Image cooldownMask;
    [SerializeField] private float cooldownTime = 5f;

    private float timer;
    private bool isCooldown;

    void Update()
    {
        if (Keyboard.current != null &&
            Keyboard.current.eKey.wasPressedThisFrame &&
            !isCooldown)
        {
            isCooldown = true;
            timer = cooldownTime;

            cooldownMask.fillAmount = 1f;
        }

        if (isCooldown)
        {
            timer -= Time.deltaTime;

            cooldownMask.fillAmount = timer / cooldownTime;

            if (timer <= 0)
            {
                isCooldown = false;
                cooldownMask.fillAmount = 0f;
            }
        }
    }

}
