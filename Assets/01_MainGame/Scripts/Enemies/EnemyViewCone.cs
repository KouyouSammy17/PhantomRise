using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
public class EnemyViewCone : MonoBehaviour
{
    //[SerializeField] private EnemyController enemyController;

    [SerializeField] private EnemyVision enemyVision;

    [SerializeField] private int meshResolution = 40;

    private Mesh mesh;

    void Start()
    {
        mesh = new Mesh();
        mesh.name = "View Cone Mesh";

        GetComponent<MeshFilter>().mesh = mesh;
    }

    void LateUpdate()
    {
        DrawViewCone();
    }

    void DrawViewCone()
    {
        float viewAngle = enemyVision.viewAngle;
        float viewDistance = enemyVision.chaseRange;

        int stepCount = meshResolution;

        float stepAngleSize = viewAngle / stepCount;

        Vector3[] vertices = new Vector3[stepCount + 2];

        int[] triangles = new int[stepCount * 3];

        vertices[0] = Vector3.zero;

        for (int i = 0; i <= stepCount; i++)
        {
            float angle = -viewAngle / 2 + stepAngleSize * i;

            Vector3 direction =
                Quaternion.Euler(0, angle, 0) * transform.forward;

            Vector3 vertex =
                transform.InverseTransformPoint(
                    transform.position + direction * viewDistance
                );

            vertices[i + 1] = vertex;

            if (i < stepCount)
            {
                triangles[i * 3] = 0;
                triangles[i * 3 + 1] = i + 1;
                triangles[i * 3 + 2] = i + 2;
            }
        }

        mesh.Clear();

        mesh.vertices = vertices;
        mesh.triangles = triangles;

        mesh.RecalculateNormals();
    }
}