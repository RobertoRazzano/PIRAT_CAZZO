using UnityEngine;
using UnityEngine.UI;
using System.Collections;



public class DocManager : MonoBehaviour

{
    private enum State { Idle, LookingFor, Healing }

    [Header("References")]
    [SerializeField] private Animator animator;
    [SerializeField] private UnityEngine.AI.NavMeshAgent agent;
    private GameObject currentTarget;
    private Animator pirateAnim;
    private AudioSource audioSource;
    private AudioClip lastClipPlayed = null; // Per evitare di riprodurre lo stesso clip consecutivamente

    // ------------

    [Header("Idle State")]
    [SerializeField] private BoxCollider idleArea;
    [SerializeField] private float idleWalkDelay = 2f;  // tempo iniziale di attesa
    [SerializeField] private float idleWalkInterval = 4f; // tempo tra un movimento e l'altro

    // -----------------

    [Header("Heal State")]

    [SerializeField] private Transform[] healArea;
    [SerializeField] private float healRay = 10.0f;
    [SerializeField] private int recoveryPoints = 40;
    [SerializeField] private GameObject healVFXPrefab;
    [SerializeField] private AudioClip healClip;
    private bool hasHealed = false; // flag per evitare più cure
    public bool isHealing = false; // flag per indicare se il pirata è in cura

    // -----------------

    [Header("Health")]
    [SerializeField] private int maxHealth = 100;
    private int currentHealth;
    [SerializeField] private Image healthBar; // Riferimento al Canvas della salute del pirata
    [SerializeField] private Image healthFill;

    private bool _isDead = false; // flag per lo stato di morte
    public bool IsDead => _isDead; // proprietà per accedere allo stato di morte

    // --------- STATI INTERNI 
    private State currentState = State.Idle;
    private float idleTimer;              // timer per attendere tra un punto e l'altro
    private Vector3 nextIdlePoint;
    private bool hasTakenDamage = false; // flag per sapere se il pirata ha preso danni


    void Awake()
    {
        if (!agent) agent = GetComponent<UnityEngine.AI.NavMeshAgent>();
        currentHealth = maxHealth;

        // NUOVO: Inizializza la barra di riempimento della vita al massimo
        

        // Se la healthBar (che ora è un'immagine) è il contenitore principale e vuoi vederla sempre all'inizio:
        if (healthBar != null)
        {
            healthBar.gameObject.SetActive(false); // Assicurati che il GameObject dell'immagine di sfondo sia attivo
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (_isDead) return; // Se il pirata è morto, non fare nulla
        switch (currentState)
        {
            case State.Idle: UpdateIdle(); break;
            case State.LookingFor: UpdateLookingFor(); break;
            case State.Healing: UpdateHealing(); break;

        }

    }

    #region Idle

    private void EnterIdle()

    {
        if(_isDead) return; // Non entrare in idle se il pirata è morto
        
        currentState = State.Idle;
        currentTarget = null;

        agent.isStopped = false;
        
        animator.SetBool("isWalking", true);

        idleTimer = idleWalkDelay; // aspetta prima di iniziare a muoversi
        nextIdlePoint = transform.position; // resta fermo inizialmente

    }

    private void UpdateIdle()
    {
        
        WanderInIdleArea();

        GameObject best = FindBestPirateInHealAreas();
        if (best != null)
        {
            Debug.Log("Pirata trovato");
            currentTarget = best;
            
            EnterLookingFor();
        }
        
    }


    private void WanderInIdleArea()
    {
        
        // Se sta già andando da qualche parte, aspetta che arrivi
        if (agent.pathPending || agent.remainingDistance > 0.5f)
            return;

        // Conta il tempo d’attesa
        idleTimer -= Time.deltaTime;

        // Se ha aspettato abbastanza, scegli un nuovo punto e vai
        if (idleTimer <= 0f)
        {
            Vector3 nextPoint = GetRandomPointInIdleArea();
            agent.SetDestination(nextPoint);
            agent.isStopped = false;
            animator.SetBool("isWalking", true);
            

            idleTimer = idleWalkInterval; // reset del timer
        } else {
              animator.SetBool("isWalking", false);
        }
    }

    private Vector3 GetRandomPointInIdleArea()

    {
        if (idleArea == null)
            return transform.position;

        Bounds bounds = idleArea.bounds;

        for (int i = 0; i < 10; i++)
        {
            Vector3 randomPoint = new Vector3(
                Random.Range(bounds.min.x, bounds.max.x),
                transform.position.y,
                Random.Range(bounds.min.z, bounds.max.z)
            );

            if (UnityEngine.AI.NavMesh.SamplePosition(randomPoint, out var hit, 1.0f, UnityEngine.AI.NavMesh.AllAreas))
            {
                return hit.position;
            }
        }

        return transform.position; // se non trova nulla
    }

    #endregion Idle


    #region LookingFor
    private void EnterLookingFor()
    {
        if (_isDead) return; // Non entrare in LookingFor se il pirata è morto
        currentState = State.LookingFor;
        isHealing = false;

        if (currentTarget != null)
        {
            agent.SetDestination(currentTarget.transform.position);
            agent.isStopped = false;
        }
        else
        {
            EnterIdle();
            Debug.Log("PIRATA PERSO");
        }
    }

    private void UpdateLookingFor()
    {
        if (currentTarget == null)
        {
            EnterIdle();
            return;
        }

        PirateController pc = currentTarget.GetComponent<PirateController>();
        if (pc != null && pc.IsDead) // Aggiungi questo controllo
        {
            //Debug.Log("Target pirate died while looking for them. Re-entering Idle.");
            currentTarget = null;
            EnterIdle(); // Torna in idle per cercare un nuovo target
            return;
        }

        agent.SetDestination(currentTarget.transform.position);

        float distance = Vector3.Distance(transform.position, currentTarget.transform.position);

        if (distance <= 1.5f)
        {
            EnterHealing();
        }
    }



    #endregion LookingFor

    #region Healing


    private void EnterHealing()
    {
        
        if (_isDead) return; // Non entrare in Healing se il pirata è morto
        currentState = State.Healing;

        // Ferma il medico
        agent.isStopped = true;
        
        animator.SetTrigger("Heal");
        isHealing = true;

        hasHealed = false;
    }

    private void UpdateHealing()
    {
        if (currentTarget != null)
        {
            PirateController pc = currentTarget.GetComponent<PirateController>();
            if (pc != null)
            {
                if (pc.IsDead) // Aggiungi questo controllo
                {
                    // Debug.Log("Pirate died during healing. Finding new target or going idle.");
                    currentTarget = null;
                }
                else
                {
                    pc.EnterBeingHealed(transform.position, 2f);
                    pc.Heal(recoveryPoints);
                    //Debug.Log("PirataCurato");

                    //VFX HEAL

                    if (healVFXPrefab != null)
                    {
                        GameObject vfx = Instantiate(
                            healVFXPrefab,
                            pc.transform.position + Vector3.up * 1f, // leggermente sopra il pirata
                            Quaternion.identity
                        );

                        Destroy(vfx, 2f); // distrugge il VFX dopo 2 secondi per pulizia
                    }
                    
                }
            }
            currentTarget = null;
        }

        GameObject nextTarget = FindBestPirateInHealAreas();
        if (nextTarget != null)
        {
            currentTarget = nextTarget;
            EnterLookingFor();
            
        }
        else
        {
            EnterIdle();
        }
    }


    #endregion Healing

    // ------------------------ FIND PIRATE

    private GameObject FindBestPirateInHealAreas()
    {
        GameObject best = null;
        float lowestHealth = float.MaxValue;

        foreach (Transform area in healArea)
        {
            Collider[] hits = Physics.OverlapSphere(area.position, healRay);

            foreach (Collider col in hits)
            {
                //Debug.Log("Sto cercando pirati");

                if (!col.CompareTag("Pirate")) continue;

                PirateController pc = col.GetComponent<PirateController>();
                // Modifica qui: Aggiungi pc.IsDead() nel controllo iniziale per filtrare subito i pirati morti
                if (pc == null || pc.IsDead || pc.alreadyHealing || pc.currentHealth > pc.maxHealth * 0.5f || pc.currentHealth == 0.0f)
                    continue;

                if (pc.currentHealth < lowestHealth)
                {
                    lowestHealth = pc.currentHealth;
                    best = pc.gameObject;
                }
            }
        }
        // Rimuovi il ciclo while che controllava i pirati morti alla fine,
        // dato che sono già stati filtrati all'inizio.
        return best;
    }

    private void Die()

    {

        Debug.Log($"{name} sta morendo...");
        _isDead = true; // Imposta lo stato a morto


        //OnPirateDeath?.Invoke(this);

        if (agent != null) // Essential: Check if the agent reference is valid
        {
            // First, stop the agent if it's enabled and active on the NavMesh.
            // This avoids the error if it's already disabled.
            if (agent.enabled && agent.isOnNavMesh)
            {
                agent.isStopped = true; // Attempt to stop it
                agent.updatePosition = false; // Disable position updates
                agent.updateRotation = false; // Disable rotation updates
                
            }

            // Then, disable the agent entirely.
            // This is safe even if isStopped couldn't be set.
            agent.enabled = false;
        }
        
        if (animator != null) animator.SetTrigger("Die");
    
        // Attiva la gravità se il pirata ha un Rigidbody
        Rigidbody rb = GetComponent<Rigidbody>();
        Debug.Log("Rigidbody trovato: " + (rb != null));
        if (rb != null)
        {
            rb.useGravity = true; // Riattiva la gravità per far cadere il pirata
            rb.isKinematic = false; // Disabilita la modalità Kinematic per far interagire il Rigidbody con la fisica
            rb.linearVelocity = Vector3.zero; // Ferma il movimento corrente
            rb.angularVelocity = Vector3.zero; // Ferma la rotazione corrente
            //rb.constraints = RigidbodyConstraints.None;
            //transform.position = new Vector3(transform.position.x, 2.0f, transform.position.z); // Forza la posizione a terra

            // Poi riblocca tutto
            rb.constraints = RigidbodyConstraints.FreezeAll;
        }

        SphereCollider sphere = GetComponent<SphereCollider>();
        if (sphere != null)
        {
            sphere.enabled = false;
        }

        this.enabled = false; // Disabilita lo script per fermare ulteriori aggiornamenti
    
        // Imposta la posizione a terra (se necessario)
        //transform.position = new Vector3(transform.position.x, 0.0f, transform.position.z); // Imposta Y a 0 (terra)

        // Disattiva il Canvas del pirata (UI)
        if (healthBar != null) // Assicurati che il riferimento al Canvas sia stato assegnato
        {
            healthBar.enabled = false; // Disattiva il Canvas del pirata
        }
        
        
    }

    public void OnDeathAnimationEnd()
    {
        Debug.Log("Animazione di morte terminata per " + gameObject.name);
        // Qui puoi aggiungere logica per rimuovere il pirata dalla scena o gestire la sua morte
        DestroyAfterDelay(3.0f);
    }
    private IEnumerator DestroyAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        gameObject.SetActive(false); // Disattiva il GameObject dopo il ritardo
    }


    public void TakeDamage(int dmg)

    {
        if (_isDead) return; // prevenzione danni dopo la morte

        /*if (ratTransform != null)
        {
            Vector3 dir = (ratTransform.position - transform.position).normalized;
            transform.rotation = Quaternion.LookRotation(new Vector3(dir.x, 0f, dir.z));
        }*/

        currentHealth = Mathf.Max(0, currentHealth - dmg);
        healthFill.fillAmount = (float)currentHealth / maxHealth;

        if (currentHealth <= 0f && !_isDead)
        {
            Debug.Log($"{name} sta morendo (condizione superata)");
            Die();
            Debug.Log($"{name} è morto!");
        }

        // NUOVA MODIFICA: Attiva la barra della vita al primo danno
        if (!hasTakenDamage)
        {
            hasTakenDamage = true;
            if (healthBar != null)
            {
                healthBar.gameObject.SetActive(true); // Assicurati che la barra della vita sia visibile
                Debug.Log($"{name} → healthBar attivata al primo danno");
                healthFill.fillAmount = (float)currentHealth / maxHealth; // Assicurati che sia 1 (vita piena)
            }
        }





    }

    // SUONO CURA DEL MEDICO 
    public void PlayHealSound()
    {
        if (healClip == null) return;

        audioSource.Stop();
        audioSource.clip = healClip;
        audioSource.loop = false;
        audioSource.Play();

        lastClipPlayed = healClip;
    }





    // GIZMOS

    private void OnDrawGizmosSelected()
    {
        if (healArea == null) return;

        Gizmos.color = Color.cyan;
        foreach (Transform point in healArea)
        {
            if (point != null)
                Gizmos.DrawWireSphere(point.position, healRay);
        }
    }



}
