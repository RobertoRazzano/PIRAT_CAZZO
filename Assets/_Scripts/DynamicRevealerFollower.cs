using UnityEngine;

public class DynamicRevealerFollower : MonoBehaviour
{
    [Header("Riferimenti")]
    public Transform ratTransform;
    public Camera mainCamera; // <-- usa la vera camera
    [Range(0f, 1f)]
    public float distanceFactor = 0.9f;

    void Start()
{
    if (mainCamera == null)
        mainCamera = Camera.main;
    
    // Forza la posizione corretta al primo frame
    if (ratTransform != null && mainCamera != null)
    {
        Vector3 direction = ratTransform.position - mainCamera.transform.position;
        transform.position = mainCamera.transform.position + direction * distanceFactor;
        transform.position = new Vector3(transform.position.x, ratTransform.position.y, transform.position.z);
    }
}

    void LateUpdate()
    {
        if (ratTransform == null || mainCamera == null) return;

        Vector3 direction = ratTransform.position - mainCamera.transform.position;
        transform.position = mainCamera.transform.position + direction * distanceFactor;

        // Mantieni altezza costante sulla fog plane
        transform.position = new Vector3(transform.position.x, ratTransform.position.y, transform.position.z);
    }
}
