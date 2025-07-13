using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

public class BonusMalus : MonoBehaviour
{
    [Header("Parametri Vita")]
    public int maxHealth = 100; //max vita topo 
    public int currentHealth; // vita corrente top

    private CameraControlManager cameraControlManager;

    [Header("Eventi")]
    public UnityEvent<int, int> onHealthChanged; // (current, max)
    public UnityEvent onDeath;

    //[SerializeField] private Animator animator; // Animatore per le animazioni del topo
    [SerializeField] private GameObject VFXPrefab;
    public RatInteractionManager rat;
    [SerializeField]
    private PossessionManager pm;
    public Animator animator;
    [SerializeField] private RatInputHandler inputHandler;
    [SerializeField] private RatInteractionManager interactionManager;
    [SerializeField] private Rigidbody ratRigidbody;
    
    void Awake()
    {
        currentHealth = maxHealth;
        cameraControlManager = FindFirstObjectByType<CameraControlManager>();
        animator = GetComponent<Animator>();
        NotifyHealthChange();
    }

    public void TakeDamage(int amount)
    {

        if (pm != null)
            pm.OnAttacked();
        currentHealth -= amount;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);
        StartCoroutine(EnableVFXAfterDestroy(rat.transform, 1.7f));
        NotifyHealthChange();

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    private IEnumerator EnableVFXAfterDestroy(Transform ratTransform, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (VFXPrefab != null)
        {
            var vfx = Instantiate(VFXPrefab, ratTransform.position, Quaternion.identity, ratTransform);
            vfx.transform.localPosition = Vector3.zero;
            Destroy(vfx, 2f);
        }
    }

    public void Heal(int amount)
    {
        currentHealth += amount;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);
        NotifyHealthChange();
    }


    // In BonusMalus.cs

    private IEnumerator ReloadSceneAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);

        // NON salvare i dati qui, altrimenti salvi la vita a 0.
        // GameStateManager.Instance.SaveRatData(); // RIMUOVI O COMMENTA QUESTA RIGA

        // Ricarica la scena corrente
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    private void Die()
    {

        // Aggiungi gli altri controlli di sicurezza
        if (ratRigidbody != null)
        {
            ratRigidbody.isKinematic = true;
        }
        else
        {
            Debug.LogWarning("Rigidbody non trovato");
        }

        // Gestisci altre possibili verifiche di null
        if (cameraControlManager != null)
        {
            cameraControlManager.deathZoomCoroutine = cameraControlManager.StartCoroutine(
                cameraControlManager.ZoomInOnRat(zoomMultiplier: 1.5f, zoomSpeed: 1f, duration: 3f)
            );
        }
        else {
            Debug.LogWarning("cameraControlManager non è stato trovato o inizializzato.");
        }
    

            Debug.Log("Il topo è morto!");
        onDeath?.Invoke();
        animator.SetTrigger("Die");

        // Blocca i movimenti
        inputHandler.movementLocked = true;

        // Impedisci morso e altre interazioni
        interactionManager.allowBite = false;

        // Blocca fisicamente il topo
        ratRigidbody.constraints = RigidbodyConstraints.FreezeAll;

        // Avvia zoom della camera
        // Ensure that the coroutine is stored in the deathZoomCoroutine field
        if (cameraControlManager != null)
        {
            cameraControlManager.deathZoomCoroutine = cameraControlManager.StartCoroutine(
                cameraControlManager.ZoomInOnRat(zoomMultiplier: 1.5f, zoomSpeed: 1f, duration: 3f)
            );
        }

        if (animator == null) Debug.LogError("Animator non assegnato!");
        // Salva il nome della scena PRIMA di ricaricarla. Questo è corretto.
        GameStateManager.Instance.lastSceneName = SceneManager.GetActiveScene().name;

        // Dopo un delay, ricarica la scena
        StartCoroutine(ReloadSceneAfterDelay(4f)); // Delay totale (animazione + zoom)
    }

    private void NotifyHealthChange()
    {
        //Debug.Log($"NotifyHealthChange fired: {currentHealth}/{maxHealth}");
        onHealthChanged?.Invoke(currentHealth, maxHealth);
    }
}
