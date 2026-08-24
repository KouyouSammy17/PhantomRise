using UnityEngine;

public class StunVisualization : MonoBehaviour
{
    [SerializeField] Transform center;
    [SerializeField] float speed = 100f;

    void Update()
    {
        transform.RotateAround(
            center.position,
            Vector3.up,
            speed * Time.deltaTime
        );
    }

}
