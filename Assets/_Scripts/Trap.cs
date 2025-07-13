using UnityEngine;
using System.Collections;
using UnityEngine.AI;

public enum TrapType { Spring, Glue, Slide }

public class Trap : MonoBehaviour
{
    [Header("Tipo di trappola")]
    [SerializeField] private TrapType trapType;

    [Header("Valori configurabili")]
    [SerializeField] private int springDamage = 30;
    [SerializeField] private float glueDuration = 2f;
    [SerializeField] private float slideForce = 10f;

    [SerializeField] private float springCooldown = 2f;
    private bool springReady = true;

    [Header("VFX")]
    [Tooltip("Assegna qui il prefab VFX specifico per questa trappola")]
    [SerializeField] private GameObject trapVFXPrefab;

    [SerializeField] private float requiredWiggle = 2f; // quanta "energia" serve per liberarsi
    [SerializeField] private float wiggleDecay = 0.5f;  // quanto si scarica nel tempo se non ti dimeni
    [SerializeField] private float wiggleStrength = 0.05f;
    [SerializeField] private float wiggleSpeed = 20f;
    [Header("Replaceable Spring")]
    [SerializeField] private bool isReplaceable = false;
    [SerializeField] private MeshRenderer originalRenderer;
    [SerializeField] private MeshRenderer usedRenderer;
    [SerializeField] private Collider usedTrapCollider; // Il BoxCollider sulla mesh usata
    [SerializeField] private Collider attractionCollider; // Lo SphereCollider per attrarre il pirata
    private bool trapUsed = false;

    [Header("Pirate Reset Behavior")]
    [SerializeField] private float pirateResetStopDuration = 0.5f; // Tempo che il pirata si ferma sopra la trappola
    private bool isPirateBeingAttracted = false; // Flag per permettere l'attrazione di un solo pirata alla volta

    private Transform stuckModel; // riferimento al modello visivo del topo
    private Vector3 initialModelLocalPos;

    private bool isStuck = false;
    private float wiggleAmount = 0f;
    private RatInputHandler stuckPlayer = null;

    private GameObject glueVFXInstance;

    [Header("suoni trappole")]
    [SerializeField] private AudioClip springSound;
    [SerializeField] private AudioClip glueSound;
    [SerializeField] private AudioClip slideSound;
    private AudioSource audioSource;
    private AudioClip lastClipPlayed = null; // Per evitare di riprodurre lo stesso clip consecutivamente

    private void OnTriggerEnter(Collider other)
    {
        // === LOGICA PER IL RATTO (ATTIVAZIONE DELLA TRAPPOLA) ===

        // Condizioni per l'attivazione della trappola da parte del ratto:
        // 1. La trappola deve essere "pronta" (springReady = true).
        // 2. L'oggetto che entra nel trigger deve avere il tag "Player".
        if (!springReady) return; // Se la molla non è pronta, il ratto non attiva
        if (!other.CompareTag("Player")) return; // Solo il player attiva la trappola per scattare

        // Abbiamo un ratto che è entrato nel collider principale della trappola e la trappola è pronta.
        RatInteractionManager rim = other.GetComponent<RatInteractionManager>();
        if (rim != null)
        {
            Debug.Log($"TRAP: RIM trovato, isBackflipping = {rim.isBackflipping}");

            // Controlla anche se l'animatore sta facendo un backflip
            Animator ratAnimator = other.GetComponent<Animator>();
            bool isBackflipAnimation = false;

            if (ratAnimator != null)
            {
                AnimatorStateInfo stateInfo = ratAnimator.GetCurrentAnimatorStateInfo(0);
                isBackflipAnimation = stateInfo.IsName("Backflip") || stateInfo.IsTag("Backflip");
                Debug.Log($"TRAP: Animazione backflip attiva = {isBackflipAnimation}");
            }

            if (rim.isBackflipping || isBackflipAnimation)
            {
                Debug.Log("Backflip attivo: aspetto che finisca prima di attivare la trappola.");

                // Disabilita il collider principale della trappola per evitare attivazioni multiple durante il backflip
                Collider mainTrapCollider = GetComponent<Collider>(); // Questo è il collider principale della trappola
                if (mainTrapCollider != null)
                {
                    mainTrapCollider.enabled = false;
                }

                StartCoroutine(WaitForBackflipEnd(rim));
                return; // Esci, la trappola verrà processata dopo il backflip
            }
            else
            {
                Debug.Log("TRAP: isBackflipping è FALSE, procedo normalmente");
            }
        }
        else
        {
            Debug.Log("TRAP: RIM è NULL!");
        }

        // Processa normalmente la trappola per il ratto (danno, swap mesh, ecc.)
        ProcessTrap(other);
    }

    private void Awake()
    {
        // se non assegnati in inspector, cerchiamo automaticamente i renderer nel caso non siano figli diretti
        if (originalRenderer == null)
            originalRenderer = GetComponentInChildren<MeshRenderer>(); // Trova il primo MeshRenderer figlio

        // Assicurati che la mesh "usata" sia disabilitata all'inizio (trappola pronta)
        if (usedRenderer != null)
            usedRenderer.enabled = false;

        // Assicurati che i collider per l'interazione con il pirata siano disabilitati all'inizio
        // La trappola parte come "pronta", non "usata" e non "in attrazione"
        if (usedTrapCollider != null)
            usedTrapCollider.enabled = false;
        if (attractionCollider != null)
            attractionCollider.enabled = false;
    }
    private void Start()
    {
        audioSource = GetComponent<AudioSource>();
    }   
    private IEnumerator WaitForBackflipEnd(RatInteractionManager rim)
    {
        Animator ratAnimator = rim.GetComponent<Animator>();

        // Aspetta che ENTRAMBI isBackflipping sia false E l'animazione sia finita
        while (rim != null && (rim.isBackflipping || IsBackflipAnimationActive(ratAnimator)))
        {
            Debug.Log($"WAITING: isBackflipping = {rim.isBackflipping}, Animation = {IsBackflipAnimationActive(ratAnimator)}");
            yield return new WaitForFixedUpdate(); // Usa FixedUpdate per physics
        }

        yield return new WaitForFixedUpdate(); // Un frame extra per sicurezza
        Debug.Log("BACKFLIP COMPLETAMENTE TERMINATO");

        // Riabilita il collider
        Collider trapCollider = GetComponent<Collider>();
        if (trapCollider != null)
        {
            trapCollider.enabled = true;
        }

        // Controlla se il rat � ancora sopra la trappola
        if (rim != null)
        {
            Collider ratCollider = rim.GetComponent<Collider>();
            if (ratCollider != null && trapCollider.bounds.Intersects(ratCollider.bounds))
            {
                Debug.Log("Rat ancora sopra la trappola, attivo l'effetto.");
                ProcessTrap(ratCollider);
            }
            else
            {
                Debug.Log("Rat non pi� sopra la trappola, nessun effetto.");
            }
        }
    }

    private bool IsBackflipAnimationActive(Animator animator)
    {
        if (animator == null) return false;

        AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
        bool isBackflipAnim = stateInfo.IsName("Backflip") || stateInfo.IsTag("Backflip");

        // Controlla anche se siamo in transizione verso un altro stato
        if (animator.IsInTransition(0))
        {
            AnimatorStateInfo nextState = animator.GetNextAnimatorStateInfo(0);
            if (nextState.IsName("Backflip") || nextState.IsTag("Backflip"))
                isBackflipAnim = true;
        }

        return isBackflipAnim;
    }

    private void Update()
    {
        // Gestione del wiggle per la trappola di colla
        if (isStuck && stuckPlayer != null)
        {
            Vector2 input = stuckPlayer.GetMoveInputRaw();
            //effetto visivo del wiggle
            Vector3 wiggleOffset = new Vector3(input.x, 0, input.y) * Mathf.Sin(Time.time * wiggleSpeed) * wiggleStrength;
            stuckModel.localPosition = initialModelLocalPos + wiggleOffset;

            wiggleAmount += input.magnitude * Time.deltaTime * 5f; // aumenta "barra di fuga"
            wiggleAmount -= wiggleDecay * Time.deltaTime;          // decadenza
            wiggleAmount = Mathf.Clamp(wiggleAmount, 0f, requiredWiggle);

            if (wiggleAmount >= requiredWiggle)
            {
                // Riattiva animator
                Animator anim = stuckPlayer.GetComponent<Animator>();
                if (anim != null) anim.enabled = true;

                // Riattiva controllo
                stuckPlayer.enabled = true;

                if (stuckModel != null)
                    stuckModel.localPosition = initialModelLocalPos;

                stuckModel = null;

                isStuck = false;
                stuckPlayer = null;

                if (glueVFXInstance != null)
                {
                    Destroy(glueVFXInstance);
                    glueVFXInstance = null;
                }
            }
        }
    }

    // Questo metodo è pubblico e viene chiamato dal TrapAttractionTrigger.cs
    public void OnPirateEnterAttractionTrigger(Collider pirateCollider)
    {
        // === LOGICA PER IL PIRATA (ATTRAZIONE E RESET DELLA TRAPPOLA USATA) ===

        // Condizioni per l'attrazione e il reset da parte del pirata:
        // 1. La trappola deve essere "ricaricabile" (isReplaceable = true).
        // 2. La trappola deve essere "usata" (trapUsed = true).
        // 3. Nessun altro pirata deve essere già in fase di attrazione/reset per questa trappola (isPirateBeingAttracted = false).
        // 4. Il collider che ha triggerato deve essere un pirata.
        // 5. Il pirata deve avere un NavMeshAgent.
        // 6. Il pirata deve essere in stato "Patrol" o "Suspicious" (non in combattimento o morto).
        // 7. Aggiunto controllo per 'alreadyHealing' se presente nel PirateController.

        if (isReplaceable && trapUsed && !isPirateBeingAttracted && pirateCollider.CompareTag("Pirate"))
        {
            PirateController pc = pirateCollider.GetComponent<PirateController>();
            if (pc != null)
            {
                string state = pc.CurrentState;
                // Controlla anche se il pirata è già in uno stato di "cura" (healing)
                // Assicurati che 'alreadyHealing' esista nel tuo PirateController
                if ((state == "Patrol" || state == "Suspicious") && !pc.alreadyHealing)
                {
                    NavMeshAgent pirateAgent = pirateCollider.GetComponent<NavMeshAgent>();
                    if (pirateAgent != null)
                    {
                        Debug.Log($"PIRATE: Pirata {pc.name} in stato '{state}' ha rilevato trappola usata e ricaricabile. Avvio attrazione e reset.");
                        isPirateBeingAttracted = true; // Imposta il flag per bloccare altri pirati

                        // Disabilita immediatamente il collider di attrazione per evitare altri trigger
                        if (attractionCollider != null)
                        {
                            attractionCollider.enabled = false;
                            Debug.Log("TRAP: Attraction Collider disabilitato.");
                        }

                        // Avvia la coroutine che gestirà il movimento del pirata e il reset
                        StartCoroutine(AttractAndResetTrapRoutine(pirateAgent));
                    }
                }
            }
        }
    }
    private void ProcessTrap(Collider other)
    {
        switch (trapType)

        {
            case TrapType.Spring:
                if (!springReady)
                    break; // La trappola non è pronta, non fare nulla

                // 1) Effetti VFX e danno al ratto
                if (trapVFXPrefab != null)
                    SpawnVFX(trapVFXPrefab, other.transform);
                PlayClip(springSound, false); // Riproduci suono di scatto

                var hp = other.GetComponent<BonusMalus>();
                if (hp != null)
                    hp.TakeDamage(springDamage);

                // 2) Disabilito il collider principale della trappola (per il ratto)
                // Questo impedisce al ratto di subire ulteriori danni dalla stessa trappola usata.
                springReady = false; // La trappola non è più pronta
                Collider mainTrapCol = GetComponent<Collider>(); // Ottieni il collider principale su questo GameObject
                if (mainTrapCol != null)
                {
                    mainTrapCol.enabled = false;
                    Debug.Log("TRAP: Collider principale (per ratto) disabilitato.");
                }

                // 3) SWAP della mesh: mostra il modello "usato"
                if (originalRenderer != null) originalRenderer.enabled = false;
                if (usedRenderer != null) usedRenderer.enabled = true;
                Debug.Log("TRAP: Mesh scambiata in 'usata'.");

                // 4) Se la trappola è ricaricabile, la marchiamo come "usata"
                // e abilitiamo i collider per il pirata.
                if (isReplaceable)
                {
                    trapUsed = true; // La trappola è ora in stato "usata" e attende il reset

                    // NEW: Abilita il collider sulla mesh "usata" (il BoxCollider per il pirata)
                    if (usedTrapCollider != null)
                    {
                        usedTrapCollider.enabled = true;
                        Debug.Log("TRAP: Collider 'usato' (per il pirata) abilitato.");
                    }
                    // NEW: Abilita lo SphereCollider di attrazione
                    if (attractionCollider != null)
                    {
                        attractionCollider.enabled = true;
                        // Resetta il flag per permettere a un nuovo pirata di essere attratto
                        isPirateBeingAttracted = false;
                        Debug.Log("TRAP: Attraction Collider abilitato.");
                    }
                }
                break;


            case TrapType.Glue:
                var pc = other.GetComponent<RatInputHandler>();
                if (pc != null && !isStuck)
                {
                    isStuck = true;
                    stuckPlayer = pc;

                    // Salva riferimento al modello
                    stuckModel = pc.transform; // <-- metti qui il nome esatto del figlio con la mesh
                    if (stuckModel != null)
                        initialModelLocalPos = stuckModel.localPosition;

                    // Blocca input
                    pc.enabled = false;

                    // Blocca animator
                    Animator anim = pc.GetComponent<Animator>();
                    if (anim != null) anim.enabled = false;

                    wiggleAmount = 0f;

                    if (trapVFXPrefab != null)
                    {
                        glueVFXInstance = Instantiate(trapVFXPrefab, other.transform.position, Quaternion.identity, other.transform);
                        PlayClip(glueSound, false); // Riproduci suono di colla
                    }
                }
                break;

            case TrapType.Slide:
                if (trapVFXPrefab != null)
                    SpawnVFX(trapVFXPrefab, other.transform);
                PlayClip(slideSound, false);

                var rb = other.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    Vector3 direction = other.transform.forward;
                    rb.AddForce(direction * slideForce, ForceMode.Impulse);
                }
                break;
        }
    }

    private void SpawnVFX(GameObject prefab, Transform parent)
    {
        // Istanzia il VFX come figlio del ratto, così segue sempre la sua posizione
        var vfx = Instantiate(prefab, parent.position, Quaternion.identity, parent);
        Destroy(vfx, 2f);
    }

    private void OnTriggerExit(Collider other)
    {
        // Non pi� necessario con la versione coroutine
        // Il collider viene gestito automaticamente nella WaitForBackflipEnd
    }

    private IEnumerator GlueEffect(RatInputHandler pc)
    {
        pc.enabled = false;
        yield return new WaitForSeconds(glueDuration);
        pc.enabled = true;
    }

    /// <summary>
    /// Muove il pirata verso la trappola usata, lo fa “lavorare” per un breve
    /// periodo, quindi ripristina la trappola e libera il pirata.
    /// </summary>
    private IEnumerator AttractAndResetTrapRoutine(NavMeshAgent pirateAgent)
    {
        Transform pirateTransform = pirateAgent.transform;
        PirateController pc = pirateAgent.GetComponent<PirateController>();

        // Se il pirata è già in fase di cura, annulla subito.
        if (pc != null && pc.alreadyHealing)
        {
            isPirateBeingAttracted = false;
            if (isReplaceable && trapUsed && attractionCollider != null)
                attractionCollider.enabled = true;
            yield break;
        }

        /* -----------------------------------------------------------
         * 1. Calcola il punto di destinazione valido su NavMesh
         * --------------------------------------------------------- */
        Vector3 rawTarget = usedTrapCollider.bounds.center; // centro della mesh "usata"
        Vector3 targetPosition;
        NavMeshHit hit;
        if (NavMesh.SamplePosition(rawTarget, out hit, 1.0f, NavMesh.AllAreas))
            targetPosition = hit.position;      // punto proiettato sulla NavMesh
        else
            targetPosition = transform.position; // fallback: posizione del GO trap

        pirateAgent.isStopped = false;
        pirateAgent.SetDestination(targetPosition);
        pirateTransform.GetComponent<Animator>()?.SetBool("isWalking", true);
        Debug.DrawLine(pirateTransform.position, targetPosition, Color.blue, 5f);
        Debug.Log($"TRAP: {pirateAgent.name} si dirige verso la trappola → {targetPosition}");

        /* -----------------------------------------------------------
         * 2. Attendi arrivo o cambio di stato
         * --------------------------------------------------------- */


        float arriveDistance = Mathf.Max(pirateAgent.stoppingDistance, 0.3f); // minimo 30 cm

        while (true)
        {
            // se il pirata cambia stato → abort
            if (pc != null &&
                (pc.CurrentState == "Chasing" || pc.CurrentState == "Attacking" || pc.CurrentState == "Dead"))
            {
                Debug.Log($"TRAP: {pirateAgent.name} ha cambiato stato → {pc.CurrentState}. Abort.");
                isPirateBeingAttracted = false;
                if (isReplaceable && trapUsed && attractionCollider != null)
                    attractionCollider.enabled = true;
                yield break;
            }

            // attendi che il path sia calcolato
            if (pirateAgent.pathPending)
            {
                yield return null;
                continue;
            }

            // usa remainingDistance (2D sul NavMesh) invece di distanza 3D
            if (pirateAgent.remainingDistance <= arriveDistance)
                break;          // arrivato!

            // se il path diventa invalido o stalla, termina
            if (pirateAgent.pathStatus == NavMeshPathStatus.PathInvalid ||
                pirateAgent.pathStatus == NavMeshPathStatus.PathPartial)
            {
                Debug.Log("TRAP: percorso invalido, abort attrazione.");
                isPirateBeingAttracted = false;
                if (isReplaceable && trapUsed && attractionCollider != null)
                    attractionCollider.enabled = true;
                yield break;
            }

            yield return null;  // aspetta prossimo frame

            /* -----------------------------------------------------------
             * 3. Simula il “lavoro” di reset
             * --------------------------------------------------------- */
            pirateAgent.isStopped = true;
            pirateTransform.GetComponent<Animator>()?.SetBool("isWalking", false);
            Debug.Log($"TRAP: {pirateAgent.name} resetta la trappola per {pirateResetStopDuration} s");
            yield return new WaitForSeconds(pirateResetStopDuration);

            /* -----------------------------------------------------------
             * 4. Ripristino effettivo della trappola
             * --------------------------------------------------------- */
            if (originalRenderer != null) originalRenderer.enabled = true;
            if (usedRenderer != null) usedRenderer.enabled = false;

            if (usedTrapCollider != null) usedTrapCollider.enabled = false;
            if (attractionCollider != null) attractionCollider.enabled = false;   // disattiva il trigger finché la trappola non viene ri-usata

            Collider mainCol = GetComponent<Collider>();
            if (mainCol != null) mainCol.enabled = true;

            springReady = true;
            trapUsed = false;
            Debug.Log("TRAP: reset completato con successo.");

            /* -----------------------------------------------------------
             * 5. Libera il pirata
             * --------------------------------------------------------- */
            pirateAgent.isStopped = false;
            pirateTransform.GetComponent<Animator>()?.SetBool("isWalking", true);
            isPirateBeingAttracted = false;
            pirateAgent.ResetPath(); // Reset del path per evitare problemi futuri
            Debug.Log("TRAP: il pirata torna al suo pattugliamento.");
        }



#if UNITY_EDITOR
        void OnDrawGizmos()
        {
            UnityEditor.Handles.color = Color.yellow;
            UnityEditor.Handles.Label(
                transform.position + Vector3.up * 1.5f,
                trapType.ToString()
            );
        }
#endif
    }

    // FUNZIONE USATA PER LA RIPRODUZIONE DEL SUONO 
    private void PlayClip(AudioClip clip, bool loop = true)
    {
        if (clip == null)
            return;

        audioSource.Stop(); //Ferma immediatamente qualsiasi suono che l'AudioSource sta attualmente riproducendo.
        audioSource.clip = clip; // Imposta il nuovo AudioClip da riprodurre.
        audioSource.loop = loop; // Specifica se il suono deve essere ripetuto in loop continuo (true) o suonato una volta sola (false).
        audioSource.Play(); //riproduce l'audio 

        lastClipPlayed = clip;
    }
}