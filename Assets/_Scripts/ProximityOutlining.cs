using System.Collections.Generic;
using UnityEngine;

public class ProximityOutlining : MonoBehaviour
{
    [Header("Configurazione")]
    [SerializeField] private float biteRaycastDistance = 2f;
    [SerializeField] private float nearSphereRadius = 0.8f;
    [SerializeField] private Transform ratTransform;

    void Update()
    {
        if (ratTransform == null) return;

        List<CheesePowerUp> found = FindMordibili();

        foreach (var cheese in FindObjectsOfType<CheesePowerUp>())
        {
            if (found.Contains(cheese))
            {
                if (!cheese.wasNear)
                {
                    cheese.EnableOutline(true);
                    cheese.wasNear = true;
                }
            }
            else
            {
                // Se prima era vicino ma ora non lo è più, ci pensa OnTriggerExit a disattivare
                cheese.wasNear = false;
            }
        }

    }

    private List<CheesePowerUp> FindMordibili()
    {
        List<CheesePowerUp> result = new List<CheesePowerUp>();

        Vector3 origin = ratTransform.position + Vector3.up * 0.5f;
        Vector3 direction = ratTransform.forward;

        // Raggio frontale
        if (Physics.Raycast(origin, direction, out RaycastHit hit, biteRaycastDistance, LayerMask.GetMask("PirateHittable")))
        {
            if (hit.collider.CompareTag("Cheese"))
            {
                var cheese = hit.collider.GetComponent<CheesePowerUp>();
                if (cheese != null)
                    result.Add(cheese);
            }
        }

        // Sfera ravvicinata
        Collider[] cheeseHits = Physics.OverlapSphere(ratTransform.position, nearSphereRadius);
        foreach (Collider c in cheeseHits)
        {
            if (c.CompareTag("Cheese"))
            {
                var cheese = c.GetComponent<CheesePowerUp>();
                if (cheese != null && !result.Contains(cheese))
                    result.Add(cheese);
            }
        }

        return result;
    }
}
