using UnityEngine;

/// <summary>
/// Distrugge il barile quando il ratto sta effettivamente mordendo
/// ed è dentro il trigger.
/// </summary>
[RequireComponent(typeof(SphereCollider))]
public class BarrelInteractable : MonoBehaviour
{
    private void Awake()
    {
        // Garantisce che il collider resti trigger
        GetComponent<Collider>().isTrigger = true;
    }

    private void OnTriggerStay(Collider other)
    {
        // deve essere il ratto
        if (!other.CompareTag("Player")) return;

        // serve il RatInteractionManager sul ratto
        var rat = other.GetComponent<RatInteractionManager>();
        if (rat == null) return;

        // "biting" è true per ~1 s ogni volta che parte un morso
        // (sia colpirà che no) → RatInteractionManager, righe con ‘biting = true’:contentReference[oaicite:0]{index=0}
        if (rat.biting)
        {
            // distrugge l’intero barile (l’oggetto padre)
            Destroy(transform.parent.gameObject);
        }
    }
}
