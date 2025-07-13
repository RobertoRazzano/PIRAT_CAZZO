using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;
using System;
using System.Linq;
using System.Collections;

public class PirateController : MonoBehaviour
{
    public enum State { Patrol, Suspicious, Chasing, Attacking, BeingHealed, Dead }

    public string CurrentState => state.ToString();

    public State state;

    public event Action<PirateController> OnPirateDeath;
    public bool isPossessed { get; set; }

    [Header("References")]
    [SerializeField] private Animator animator;
    [SerializeField] private NavMeshAgent agent;
    [SerializeField] private Transform[] patrolPoints;
    [SerializeField] private Transform ratTransform;
    [SerializeField] private RatInteractionManager ratManager;
    [SerializeField] private BonusMalus ratHealt;
    [SerializeField] private float minDistanceFromRat = 1.0f; // distanza minima per fermarsi dal ratto

    [Header("Sight")]
    [SerializeField] private float viewDistance = 10f;
    [SerializeField] private float viewAngle = 90f;
    [SerializeField] private float eyeHeight = 1.6f;
    [SerializeField] private float viewOriginBackOffset = 0.5f;
    private float lostSightTimer = 0f;
    [SerializeField] private float lostSightGracePeriod = 2.0f; // tempo in secondi prima di smettere di inseguire


    [Header("Alert UI")]
    [SerializeField] private GameObject alertIndicator;
    [SerializeField] private Image alertFillImage;

    [Header("Alert Timings")]
    [SerializeField] private float attachTime = 5f;
    [SerializeField, Range(0f, 1f)] private float moveThreshold = 0.7f;
    [SerializeField] private float baseFillSpeed = 1f;


    [Header("Chase")]
    [SerializeField] private float chaseSpeed = 3.0f;

    [Header("Attacking")]
    [SerializeField] private float attackRange = 2.0f;
    [SerializeField] private float attackCooldown = 2.0f;
    [SerializeField] private int attackDamage = 10;
    [SerializeField] private float damageRange = 1.5f; // distanza massima per infliggere danni

    private float lastAttackTime;
    private bool canAttack = true;

    [Header("Health")]
    public float maxHealth = 100f;
    [SerializeField] private Image healthFill;
    [SerializeField] private int biteTickDamage;
    [SerializeField] private float biteTickInterval;
    [SerializeField] private float biteDuration;
    [SerializeField] Image healthBar;
    [SerializeField] private GameObject damageVFXPrefab; // Prefab per gli effetti visivi del danno
    public bool infected = false;

    [Header("HEAL STATUS")]
    [SerializeField] private float healingCooldown = 5f; // tempo in secondi dopo la guarigione
    private float lastHealedTime = -Mathf.Infinity;
    public bool alreadyHealing = false;
    public float healingEndTime = 3.0f; // usata per fermare il movimento di camminata del pirata per un certo tempo 

    [Header("Death VFX")]
    [SerializeField] private GameObject deathVFXPrefab;

    public event Action OnDeathAnimationEndEvent;


    // STATI INTERNI 
    private Coroutine infectionCoroutine;
    private int patrolIdx;
    private float suspicionTimer;
    private Vector3 suspicionTarget;
    private bool hasStartedInvestigating;
    private bool hasDealtDamageThisAttack = false;
    private bool ratWasRecentlyInvincible = false;
    private float retryAttackTime = 0f;
    private bool _isDead = false;

    public float currentHealth;

    public bool IsDead => _isDead;
    private bool hasTakenDamage = false; // per attivare la barra della vita al primo danno



    private void Awake()
    {
        if (!agent) agent = GetComponent<NavMeshAgent>();
        currentHealth = maxHealth;
        healthFill.fillAmount = 1f;
        ResetAlert();
        ratManager = ratTransform.GetComponent<RatInteractionManager>();

        ratHealt = ratTransform.GetComponent<BonusMalus>();

        if (ratHealt == null)
            Debug.LogError($"{name} → ratHealt è NULL");

        if (healthBar != null)
        {
            healthBar.gameObject.SetActive(false); // Assicurati che la barra della vita sia disabilitata all'inizio
            Debug.Log($"{name} → healthBar disabilitata all'inizio");
            }
        ; // Assicurati che la barra della vita sia disabilitata all'inizio
        
        

    }

    private void Start()
    {
        if (patrolPoints.Length > 0)
        {
            agent.SetDestination(patrolPoints[0].position);
            animator.SetBool("isWalking", true);
        }
    }

    private void Update()
    {
        if (_isDead)
        {
            return;
        }

        if (ratHealt.currentHealth == 0f)
        {
            state = State.Patrol; // Se il ratto è morto, torna in pattugliamento
        }
        
        float distanceToRat = Vector3.Distance(transform.position, ratTransform.position);

        bool shouldStop = distanceToRat < minDistanceFromRat;
        if (agent.isStopped != shouldStop)
        {
            agent.isStopped = shouldStop;

            if (!shouldStop && state == State.Chasing)
            {
                agent.SetDestination(ratTransform.position);
            }
        }

        switch (state)
        {
            case State.Patrol: PatrolUpdate(); break;
            case State.Suspicious: SuspiciousUpdate(); break;
            case State.Chasing: ChasingUpdate(); break;
            case State.Attacking: UpdateAttacking(); break;
            case State.BeingHealed: UpdateBeingHealed(); break;
        }
    }

    #region Patrol

    private void EnterPatrol()
    {
        if (state == State.Dead) return; // prevenzione pattugliamento dopo la morte

        state = State.Patrol;
        ResetAlert();

        if (patrolPoints.Length > 0)
        {
            agent.isStopped = false;
            agent.SetDestination(patrolPoints[patrolIdx].position);
            animator.SetBool("isWalking", true);
            agent.updatePosition = true;
            agent.updateRotation = true;
        }
    }

    private void PatrolUpdate()
    {
        if (CanSeeRat())
        {
            EnterSuspicious();
            return;
        }

        if (!agent.isOnNavMesh)
        {
            Debug.LogWarning($"{name} NON è su una NavMesh! (posizione: {transform.position})");
            return;
        }



        if (!agent.pathPending && agent.remainingDistance < 0.3f && patrolPoints.Length > 0)
        {
            patrolIdx = (patrolIdx + 1) % patrolPoints.Length;
            agent.SetDestination(patrolPoints[patrolIdx].position);
        }
    }

    #endregion

    #region Suspicious


    private void EnterSuspicious()
    {
        if (_isDead) return; // prevenzione sospetto dopo la morte
        state = State.Suspicious;
        suspicionTimer = 0f;
        suspicionTarget = ratTransform.position;
        hasStartedInvestigating = false;

        agent.isStopped = true;
        animator.SetBool("isWalking", false);

        alertIndicator.SetActive(true);
        alertFillImage.fillAmount = 0f;
    }

    private void SuspiciousUpdate()
    {
        bool seesRat = CanSeeRat();

        float distance = Vector3.Distance(GetEyeOrigin(), ratTransform.position);
        float proximity = 1f + ((viewDistance - distance) / viewDistance);
        float delta = Time.deltaTime * baseFillSpeed * (seesRat ? 1f : -1f);

        suspicionTimer = Mathf.Clamp(suspicionTimer + delta * (seesRat ? proximity : 1f), 0f, attachTime);
        alertFillImage.fillAmount = suspicionTimer / attachTime;

        if (!hasStartedInvestigating && suspicionTimer >= attachTime * moveThreshold)
        {
            hasStartedInvestigating = true;
            agent.isStopped = false;
            agent.SetDestination(suspicionTarget);
            animator.SetBool("isWalking", true);
        }

        if (seesRat)
        {
            suspicionTarget = ratTransform.position;
            if (hasStartedInvestigating)
                agent.SetDestination(suspicionTarget);
        }

        if (suspicionTimer >= attachTime)
        {
            EnterChasing();
        }
        else if (suspicionTimer <= 0f && !seesRat)
        {
            EnterPatrol();
        }
    }

    #endregion


    #region Chasing
    private void EnterChasing()
    {
        if (state == State.Dead) return; // prevenzione inseguimento dopo la morte
        state = State.Chasing;
        //alertIndicator.SetActive(false);
        agent.speed = chaseSpeed;
        agent.updatePosition = true;
        agent.updateRotation = true;
        agent.isStopped = false;
        animator.SetBool("isWalking", true);
        SendMessage("CancelAttractionFromPuddle", this, SendMessageOptions.DontRequireReceiver);
    }

    private void ChasingUpdate()
    {
        float distance = Vector3.Distance(transform.position, ratTransform.position);

        if (CanSeeRat())
        {
            lostSightTimer = 0f; // Reset timer quando lo vedi
            agent.isStopped = false;
            agent.SetDestination(ratTransform.position);

            if (distance <= attackRange)
            {
                EnterAttacking();
            }
        }
        else
        {
            lostSightTimer += Time.deltaTime;

            if (lostSightTimer >= lostSightGracePeriod)
            {
                EnterSuspicious(); // solo dopo il grace period
            }
            else
            {
                agent.SetDestination(ratTransform.position); // continua a inseguire alla cieca
            }
        }
    }


    #endregion

    #region Attack

    private void EnterAttacking()

    {

        if (state == State.Dead) return; // prevenzione attacco dopo la morte
        if (Time.time < retryAttackTime)
            return;

        if (ratManager != null && ratManager.invincible)
        {
            ratWasRecentlyInvincible = true;
            retryAttackTime = Time.time ;
            EnterChasing();
            return;
        }

        state = State.Attacking;

        agent.isStopped = true;
        agent.velocity = Vector3.zero;
        agent.updatePosition = false;
        agent.updateRotation = false;
        hasDealtDamageThisAttack = false;

        animator.SetBool("isWalking", false);
    }


    public void InflictDamageEvent()
    {
        Debug.Log("📢 Animation Event ricevuto correttamente da " + gameObject.name);

        if (ratTransform == null || ratHealt == null || ratManager == null) return;

        float distance = Vector3.Distance(transform.position, ratTransform.position);
        if (distance <= damageRange && !ratManager.invincible)
        {
            ratHealt.TakeDamage(attackDamage);
            hasDealtDamageThisAttack = true;
            Debug.Log("💥 Danno inflitto al ratto (via AnimationEvent)");
        }
        else
        {
            Debug.Log("❌ Danno NON inflitto: distanza o invincibilità");

        }

    }





    private void UpdateAttacking()
    {
        Debug.Log(canAttack);
        if (ratTransform == null) return;

        // 🔒 Se l'attacco è in corso, non rilanciare il trigger
        if (!canAttack) return;

        // Controllo vista
        if (CanSeeRat())
        {
            lostSightTimer = 0f;
        }
        else
        {
            lostSightTimer += Time.deltaTime;
            if (lostSightTimer >= lostSightGracePeriod)
            {
                EnterPatrol(); // smette di inseguire
                return;
            }
        }

        float distance = Vector3.Distance(transform.position, ratTransform.position);

        // Attendi che l'animazione finisca prima di fare qualcos'altro
        if (!hasDealtDamageThisAttack && distance > attackRange)
        {
            return; // resta fermo in attesa di finire l'attacco
        }

        // Cooldown: evita che attacchi troppo in fretta
        if (Time.time < lastAttackTime + attackCooldown)
        {
            return;
        }

        // Se il topo è invincibile, non attaccare
        if (ratManager != null && ratManager.invincible)
        {
            Debug.Log("❌ ATTACCO NON PARTITO: ratto invincibile");
            return;
        }

        // ✅ Rilancia l'attacco (una sola volta)
        canAttack = false; // 🔒 blocca altri attacchi finché l’animazione non finisce
        hasDealtDamageThisAttack = false;

        // Ruota verso il ratto
        Vector3 dir = (ratTransform.position - transform.position).normalized;
        Quaternion lookRotation = Quaternion.LookRotation(new Vector3(dir.x, 0f, dir.z));
        transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * 5f);

        // Avvia l’animazione
        animator.SetTrigger("AttackTrigger");
        lastAttackTime = Time.time;

        Debug.Log("🎯 ATTACK TRIGGERED");
    }






    public void OnAttackAnimationEnd()
    {
        Debug.Log("✅ ATTACK ENDED");

        canAttack = true; // ✅ Ora si può attaccare di nuovo

        agent.isStopped = false;
        agent.updatePosition = true;
        agent.updateRotation = true;

        if (CanSeeRat())
            EnterChasing();
        else
            EnterSuspicious(); // o EnterPatrol()
    }




    #endregion

    #region BeingHealed

    public void EnterBeingHealed(Vector3 medicPosition, float duration)
    {
        if (_isDead) return;
        state = State.BeingHealed;

        agent.isStopped = true;
        animator.SetBool("isWalking", false);

        // Ruota verso il medico
        Vector3 dir = (medicPosition - transform.position).normalized;
        Quaternion lookRotation = Quaternion.LookRotation(new Vector3(dir.x, 0f, dir.z));
        transform.rotation = lookRotation;

        healingEndTime = Time.time + duration;
    }

    private void
    UpdateBeingHealed()
    {
        if (Time.time >= healingEndTime)
        {
            animator.SetBool("isWalking", true);
            agent.isStopped = false;
            EnterPatrol(); // oppure EnterChasing(), in base al contesto
        }
    }

    #endregion






    private void ResetAlert()
    {
        alertIndicator.SetActive(false);
        alertFillImage.fillAmount = 0f;
        suspicionTimer = 0f;
        hasStartedInvestigating = false;
    }

    // ------ VISION

    private Vector3 GetEyeOrigin()
    {
        return transform.position - transform.forward * viewOriginBackOffset + Vector3.up * eyeHeight;
    }

    private bool CanSeeRat()
    {
        Vector3 origin = GetEyeOrigin();
        Vector3 directionToRat = (ratTransform.position - origin).normalized;
        float distance = Vector3.Distance(origin, ratTransform.position);

        // ✅ Controlla se il topo è nel cono visivo
        float angle = Vector3.Angle(transform.forward, directionToRat);
        if (angle > viewAngle * 0.5f)
        {
            Debug.DrawRay(origin, directionToRat * distance, Color.gray, 1.5f); // 🟪 Cono visivo fallito
            return false;
        }

        // ✅ Raycast per occlusione (esegui più tentativi verticali)
        for (float yOffset = 0f; yOffset <= 1f; yOffset += 0.25f)
        {
            Vector3 target = ratTransform.position + Vector3.up * yOffset;
            Vector3 dir = target - origin;

            if (!Physics.Raycast(origin, dir.normalized, out RaycastHit hit, distance, LayerMask.GetMask("Wall")))
            {
                // ✅ Nessun muro → visione libera
                Debug.DrawRay(origin, dir.normalized * distance, Color.green, 1.5f); // 🟩 Raggio valido
                return true;
            }
            else
            {
                // ❌ C'è un ostacolo davanti → visione bloccata
                Debug.DrawRay(origin, dir.normalized * distance, Color.red, 1.5f); // 🟥 Colpito muro o ostacolo
            }
        }

        return false;
    }

    // ---- DAMAGE 

    public void TakeDamage(int dmg)

    {
        if (_isDead) return; // prevenzione danni dopo la morte

        if (ratTransform != null)
        {
            Vector3 dir = (ratTransform.position - transform.position).normalized;
            transform.rotation = Quaternion.LookRotation(new Vector3(dir.x, 0f, dir.z));
        }

        currentHealth = Mathf.Max(0f, currentHealth - dmg);
        healthFill.fillAmount = currentHealth / maxHealth;
        
        if (damageVFXPrefab != null)
        {
            GameObject vfx = Instantiate(
                damageVFXPrefab,
                transform.position + Vector3.up * 1.2f,
                Quaternion.identity
            );

            vfx.transform.SetParent(transform); // lo segue
            vfx.transform.localScale = Vector3.one * 0.5f; // riduci scala

            Destroy(vfx, 2f);
        }
        if (currentHealth <= 0f && !_isDead)
        {
            Die();
            Debug.Log($"{name} è morto!");
        }
        ;

        // NUOVA MODIFICA: Attiva la barra della vita al primo danno
        if (!hasTakenDamage)
        {
            hasTakenDamage = true;
            if (healthBar != null)
            {
               healthBar.gameObject.SetActive(true); // Assicurati che la barra della vita sia visibile
                Debug.Log($"{name} → healthBar attivata al primo danno");
            }
        }


        // Lancia l'infezione al primo danno, se non è già partita
        if (!infected)
        {
            Debug.Log("infettato");
            infected = true;

            if (infectionCoroutine != null)
                StopCoroutine(infectionCoroutine);

            infectionCoroutine = StartCoroutine(
                InfectionDamageRoutine(biteTickDamage, biteTickInterval, biteDuration)
            );
        }

    }

    private IEnumerator InfectionDamageRoutine(int biteTickDamage, float biteTickInterval, float biteDuration)
    {
        Debug.Log("Courutine cominciata");
        float elapsed = 0f;

        while (elapsed < biteDuration)
        {
            yield return new WaitForSeconds(biteTickInterval);

            TakeDamage((int)biteTickDamage);
            Debug.Log($"[Infezione] Vita attuale del pirata: {currentHealth}");

            elapsed += biteTickInterval;

            if (currentHealth <= 0f) Die();
        }
    }

    private void Die()
    {

        Debug.Log($"{name} sta morendo...");
        _isDead = true; // Imposta lo stato a morto

        infected = false;
        OnPirateDeath?.Invoke(this);

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
        
        if (alertIndicator != null)
        {
            alertIndicator.SetActive(false); // Disattiva l'indicatore di allerta
        }
    }

        public void OnDeathAnimationEnd()
    {
        Debug.Log("Animazione di morte terminata per " + gameObject.name);

        // 🔔 Notifica gli ascoltatori
        OnDeathAnimationEndEvent?.Invoke();

        DestroyAfterDelay(3.0f);
    }


    private IEnumerator DestroyAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay); // aspetta i 3 secondi

        // Mostra il VFX quando il pirata sta per sparire
        if (deathVFXPrefab != null)
        {
            GameObject vfx = Instantiate(
                deathVFXPrefab,
                transform.position + Vector3.up * 1f, // leggermente sopra il corpo
                Quaternion.identity
            );

            Destroy(vfx, 2f); // distrugge il VFX dopo 2 secondi
        }

        gameObject.SetActive(false); // il pirata scompare
    }

    //GUARIGIONE

    public void Heal(int recoveryPoints)
    {
        if (_isDead) return;

        currentHealth = Mathf.Min(currentHealth + recoveryPoints, maxHealth);
        healthFill.fillAmount = currentHealth / maxHealth;

        alreadyHealing = true;
        lastHealedTime = Time.time;

        StartHealingCooldown();
    }


    private Coroutine healingCooldownCoroutine;

    private IEnumerator HealingCooldownRoutine()
    {
        yield return new WaitForSeconds(healingCooldown);
        alreadyHealing = false;
    }

    private void StartHealingCooldown()
    {
        if (healingCooldownCoroutine != null)
            StopCoroutine(healingCooldownCoroutine);

        healingCooldownCoroutine = StartCoroutine(HealingCooldownRoutine());
    }



    // ------ GIZMOS

    private void OnDrawGizmosSelected()
    {
        Vector3 origin = Application.isPlaying ? GetEyeOrigin() : transform.position + Vector3.up * eyeHeight;

        Gizmos.color = Color.cyan;
        Gizmos.DrawSphere(origin, 0.1f);

        Gizmos.color = new Color(1f, 1f, 0f, 0.25f);
        Gizmos.DrawWireSphere(origin, viewDistance);

        Gizmos.color = Color.yellow;
        int rays = 30;
        float halfAngle = viewAngle * 0.5f;
        for (int i = 0; i <= rays; i++)
        {
            float angle = -halfAngle + (viewAngle / rays) * i;
            Quaternion rot = Quaternion.Euler(0, angle, 0);
            Vector3 dir = rot * transform.forward;
            Gizmos.DrawRay(origin, dir * viewDistance);
        }

        if (Application.isPlaying && ratTransform != null)
        {
            Vector3 target = ratTransform.position + Vector3.up * 0.4f;
            Vector3 dirToRat = target - origin;
            float dist = dirToRat.magnitude;

            if (Physics.Raycast(origin, dirToRat.normalized, out RaycastHit hit, dist, LayerMask.GetMask("Default")))
            {
                Gizmos.color = Color.red;
                Gizmos.DrawRay(origin, dirToRat.normalized * hit.distance);
                Gizmos.DrawSphere(hit.point, 0.1f);
            }
            else
            {
                Gizmos.color = Color.green;
                Gizmos.DrawRay(origin, dirToRat.normalized * Mathf.Min(dist, viewDistance));
            }

            Gizmos.color = Color.green;
            Collider ratCollider = ratTransform.GetComponent<Collider>();
            if (ratCollider != null)
            {
                Gizmos.matrix = ratCollider.transform.localToWorldMatrix;
                Gizmos.DrawWireCube(ratCollider.bounds.center - ratTransform.position, ratCollider.bounds.size);
                Gizmos.matrix = Matrix4x4.identity;
            }
        }
    }

    // ------ GET CURRENT STATE
    
    public State GetCurrentState()
    {
        return state;
    }
}
