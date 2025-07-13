using UnityEngine;
using System.Collections;

public class PoisonPuddle : MonoBehaviour
{
    private bool used = false;
    Vector3 originalScale;

    void Awake()
    {
        originalScale = transform.localScale;

        // 👇 DEBUG: Verifica setup collider
        Collider col = GetComponent<Collider>();
        if (col == null)
        {
            Debug.LogError("PoisonPuddle: NESSUN COLLIDER trovato! Aggiungine uno e impostalo come Trigger");
        }
        else
        {
            if (!col.isTrigger)
            {
                Debug.LogWarning("PoisonPuddle: Collider non è un Trigger! Impostalo come Trigger");
                col.isTrigger = true; // Fix automatico
            }
            Debug.Log($"PoisonPuddle: Collider OK - Tipo: {col.GetType().Name}, IsTrigger: {col.isTrigger}");
        }
    }

    void Start()
    {
        transform.localScale = Vector3.zero;
        StartCoroutine(ScaleUp());
    }

    IEnumerator ScaleUp()
    {
        float t = 0f;
        float duration = 0.5f;
        while (t < duration)
        {
            transform.localScale = Vector3.Lerp(Vector3.zero, originalScale, t / duration);
            t += Time.deltaTime;
            yield return null;
        }
        transform.localScale = originalScale;
        Debug.Log("PoisonPuddle: Scale completato, pronto per trigger");
    }

    private void OnTriggerEnter(Collider other)
    {
        Debug.Log($"🔥 TRIGGER ENTER con: {other.name} (Tag: {other.tag})");

        if (used)
        {
            Debug.Log("❌ Pozza già usata, ignoro");
            return;
        }

        if (other.CompareTag("Pirate"))
        {
            PirateController pirate = other.GetComponentInParent<PirateController>();
            if (pirate == null)
            {
                Debug.LogError($"❌ PirateController NON trovato su {other.name}!");
                return;
            }

            Debug.Log("✅ PIRATA RILEVATO! Inizio infezione...");
            used = true;

            // Registra il pirata come infetto
            RatInteractionManager ratManager = FindFirstObjectByType<RatInteractionManager>();
            if (ratManager != null)
            {
                ratManager.RegisterInfectedPirate(pirate);
                Debug.Log("✅ Pirata registrato come infetto");
            }
            else
            {
                Debug.LogError("❌ RatInteractionManager non trovato!");
            }

            // 🚨 IMMEDIATA GESTIONE TRAPPOLA E SCOMPARSA
            PeeAttractor attractor = GetComponentInChildren<PeeAttractor>();
            if (attractor != null && attractor.WasLegitForTrap(pirate))
            {
                // Istanzia la trappola PRIMA che la puddle scompaia
                attractor.OnPuddleConsumed(pirate);
                Debug.Log("🪤 Trappola istanziata immediatamente!");
            }

            // Nascondi immediatamente la puddle visivamente
            HidePuddleVisually();

            // Avvia il danno over time in background
            StartCoroutine(ApplyPoisonInBackground(pirate));
        }
        else
        {
            Debug.Log($"❌ Non è un pirata: {other.name} (Tag: {other.tag})");
        }
    }

    private void HidePuddleVisually()
    {
        // Nascondi tutti i renderer
        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        foreach (Renderer renderer in renderers)
        {
            renderer.enabled = false;
        }

        // Disabilita il collider per evitare ulteriori trigger
        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            col.enabled = false;
        }

        Debug.Log("💥 Puddle nascosta visivamente");
    }

    private IEnumerator ApplyPoisonInBackground(PirateController pirate)
    {
        // Applica il danno over time
        int totalDamage = 30;
        int ticks = 30;
        for (int i = 0; i < ticks; i++)
        {
            if (pirate == null) break;
            pirate.TakeDamage(1);
            yield return new WaitForSeconds(1f);
        }

        // Aspetta un po' prima di distruggere completamente
        yield return new WaitForSeconds(0.5f);
        PeeAttractor attractor = GetComponentInChildren<PeeAttractor>();
        if (attractor != null)
        {
            attractor.OnPuddleConsumed(pirate);

        }

        // Distruggi completamente l'oggetto
        Debug.Log("💥 Distruggo PoisonPuddle completamente");
        Destroy(transform.root.gameObject);
    }

    private void OnTriggerStay(Collider other)
    {
        // Debug per vedere se il pirata rimane nel trigger
        if (other.CompareTag("Pirate") && !used)
        {
            Debug.Log($"🔄 Pirata {other.name} ancora nel trigger");
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Pirate"))
        {
            Debug.Log($"🚪 Pirata {other.name} uscito dal trigger");
        }
    }
}