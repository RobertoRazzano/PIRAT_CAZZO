using UnityEngine;
using System.Collections;

[RequireComponent(typeof(SkinnedMeshRenderer))]
public class OutlineVisibilityController : MonoBehaviour
{
    public GameObject outlineObject;
    public Camera cam;
    public LayerMask occluderMask = ~0;

    SkinnedMeshRenderer _smr;
    Coroutine _visRoutine;
    const float CHECK_INTERVAL = 0.1f;

    void Start()
    {
        if (outlineObject == null || (cam == null && !(cam = Camera.main)))
        {
            enabled = false;
            return;
        }

        outlineObject.SetActive(false);
        _smr = GetComponent<SkinnedMeshRenderer>();

        var oSMR = outlineObject.GetComponent<SkinnedMeshRenderer>();
        if (oSMR != null) oSMR.updateWhenOffscreen = true;

        _visRoutine = StartCoroutine(VisibilityRoutine());
    }

    void OnDisable()
    {
        if (_visRoutine != null) StopCoroutine(_visRoutine);
    }

    IEnumerator VisibilityRoutine()
    {
        var wait = new WaitForSeconds(CHECK_INTERVAL);
        while (true)
        {
            CheckVisibility();
            yield return wait;
        }
    }

    void CheckVisibility()
    {
        Vector3 target = _smr.bounds.center;
        Vector3 dir = target - cam.transform.position;
        float dist = dir.magnitude;

        RaycastHit[] hits = Physics.RaycastAll(cam.transform.position, dir.normalized, dist, occluderMask);

        bool occluso = false;
        foreach (var hit in hits)
        {
            if (!hit.collider.transform.IsChildOf(transform))
            {
                occluso = true;
                break;
            }
        }

        outlineObject.SetActive(occluso);
    }
}
