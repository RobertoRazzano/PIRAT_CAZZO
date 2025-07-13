
using UnityEngine;

public class CircularMotion : MonoBehaviour
{
    public float radius = 5f;
    public float speed = 1f;
    public Vector3 center = Vector3.zero;
    public bool clockwise = true;

    private float angle = 0f;

    void Update()
    {
        angle += (clockwise ? -1 : 1) * speed * 2 * Mathf.PI * Time.deltaTime;

        float x = Mathf.Cos(angle) * radius + center.x;
        float z = Mathf.Sin(angle) * radius + center.z;
        float y = center.y;

        transform.position = new Vector3(x, y, z);
    }
}
