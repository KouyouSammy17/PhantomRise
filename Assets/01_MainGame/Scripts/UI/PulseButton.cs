using UnityEngine;

public class PulseButton : MonoBehaviour
{
    Vector3 startScale;

    void Start()
    {
        startScale = transform.localScale;
    }

    void Update()
    {
        float scale = 1f + Mathf.Sin(Time.time * 3f) * 0.025f;
        transform.localScale = startScale * scale;
    }
}
