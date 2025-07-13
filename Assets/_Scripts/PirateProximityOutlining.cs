using System.Collections.Generic;
using UnityEngine;

public class PirateProximityOutlining : MonoBehaviour
{
    [SerializeField] private float biteRaycastDistance = 1.7f;
    [SerializeField] private float nearSphereRadius = 0.8f;

    void Update()
    {
        // 1) Trova i pirati che sono davvero "in vista" e vicini
        List<Transform> nearPirates = FindPirates();

        // 2) Disattiva l’outline su tutti...
        PirateController[] allPirates = Object.FindObjectsByType<PirateController>(
            FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        foreach (var pirate in allPirates)
        {
            PirateOutline outl = pirate.GetComponent<PirateOutline>();
            if (outl != null)
                outl.SetOutline(false);
        }

        // 3) …e riattiva solo su quelli restituiti da FindPirates()
        foreach (var t in nearPirates)
        {
            PirateOutline outl = t.GetComponent<PirateOutline>();
            if (outl != null)
                outl.SetOutline(true);
        }
    }

    private List<Transform> FindPirates()
    {
        var result = new List<Transform>();
        Vector3 origin = transform.position + Vector3.up * 0.5f;
        int mask = LayerMask.GetMask("PirateHittable", "Default");

        // 1) prendi tutti i collider vicini
        Collider[] hits = Physics.OverlapSphere(transform.position, nearSphereRadius, mask);
        foreach (var c in hits)
        {
            if (!c.CompareTag("Pirate"))
                continue;

            // 2) calcola direzione e distanza
            Vector3 toPirate = c.transform.position - origin;
            float dist = toPirate.magnitude;
            Vector3 dir = toPirate / dist;

            // 3) (opzionale) filtro angolare ±30°
            if (Vector3.Dot(transform.forward, dir) < Mathf.Cos(30f * Mathf.Deg2Rad))
                continue;

            // 4) verifica che non ci sia un muro fra te e il pirata
            if (Physics.Raycast(origin, dir, out RaycastHit hit, dist, mask)
                && hit.collider.transform == c.transform)
            {
                result.Add(c.transform);
            }
        }

        return result;
    }
}
