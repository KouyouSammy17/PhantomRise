using UnityEngine;

public class FloatLogo : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    Vector3 startPos;

    void Start()
    {
        startPos = transform.position;
    }

    void Update()
    {
        transform.position = startPos +
            new Vector3(0, Mathf.Sin(Time.time) * 10f, 0);
    }
}
