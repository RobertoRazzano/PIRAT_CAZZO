using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;


[RequireComponent(typeof(Rigidbody))]
public class RatInteractionManager : MonoBehaviour
{
    [SerializeField] private float _ratRay = 5f;
    [SerializeField] private float biteCooldown = 1f; // cooldown in secondi
    private bool canBite = true;
    [SerializeField] private int Damage = 30;
    private int bonusDamage = 0;

    private GameObject damageVFXInstance;
    private GameObject peeVFXInstance;

    private Animator _ratAnimator;
    private RatInputHandler _ratInputHandler;
    private Rigidbody rb;
    public bool invincible = false;

    [Header("Config ScriptableObject")]
    public PowerUpConfig powerUpConfig;
    // speed
    public bool speedBoostActive { get; private set; }
    public float currentSpeedBoostMultiplier { get; private set; }
    public float speedBoostRemainingTime { get; private set; }

    // damage
    public bool IsDamageBoostActive => bonusDamage > 0;
    public int GetCurrentDamageBoostAmount() => bonusDamage;

    // poison
    public bool CanPee { get; private set; }
    public static bool HasCompletedFirstQuickTime { get; private set; } = false;


    [SerializeField] private QuickTimeUIManager quickTimeUIManager;
    private bool quickTimeConfirmed = false;
    private bool isQuickTimeActive = false;

    [SerializeField] private QuickTimeVFXManager vfxManager;


    [Header("Effetti dell' attacco")]
    public bool biting = false;
    private PirateController enemyController;

    private CameraControlManager cameraControlManager;


    //  Nuovo: lista dei pirati infettati
    public List<Transform> infectedPirates = new List<Transform>();

    private GameObject poisonPrefab;
    public bool isBackflipping = false;
    public bool allowBite = true;


    void Start()
    {
        _ratAnimator = GetComponent<Animator>();
        _ratInputHandler = GetComponent<RatInputHandler>();
        rb = GetComponent<Rigidbody>();
    }


    void Update()
    {
        //  Mostra cerchio di rilevamento
        int segments = 32;
        float angleStep = 360f / segments;
        for (int i = 0; i < segments; i++)
        {
            float angle = i * angleStep;
            float nextAngle = (i + 1) * angleStep;
            Vector3 p1 = transform.position + Quaternion.Euler(0, angle, 0) * Vector3.forward * _ratRay;
            Vector3 p2 = transform.position + Quaternion.Euler(0, nextAngle, 0) * Vector3.forward * _ratRay;
            Debug.DrawLine(p1, p2, Color.red);
        }



        // Premi 1-9 per entrare nei pirati infettati
        for (int i = 0; i < infectedPirates.Count && i < 9; i++)
        {
            if (Input.GetKeyDown(KeyCode.Alpha1 + i))
            {
                if (cameraControlManager != null)
                {
                    cameraControlManager.SwitchToPirate(infectedPirates[i]);
                }
            }
        }

    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, _ratRay);
    }

    public void OnBite(InputAction.CallbackContext context)
    {
        if (!allowBite) return;
        
        if (context.performed && canBite)
        {
            AttemptInfection();
            //Debug.Log("Input morso ricevuto");

        }
    }

    private void AttemptInfection()
    {
        RaycastHit hit;
        Vector3 origin = transform.position + Vector3.up * 0.5f;
        float biteDistance = 2f;
        Vector3 direction = transform.forward;

        bool successfulBite = false;

        // 1. 🔍 Raggio frontale
        if (Physics.Raycast(origin, direction, out hit, biteDistance, LayerMask.GetMask("PirateHittable")))
        {
            Debug.Log($"Raggio morso colpito: {hit.collider.name}");
            PirateController pirate = hit.collider.GetComponent<PirateController>();
            DocManager doctor = hit.collider.GetComponent<DocManager>(); // Prova a prendere il DocManager

            if (pirate != null)
            {
                TryStartBite(pirate); // Morso al Pirata
                successfulBite = true;
            }
            else if (doctor != null) // Se è un Medico
            {
                Debug.Log($"Raggio morso colpito un Medico: {doctor.name}");
                TryBiteDoctor(doctor); // Nuovo metodo per mordere il Medico
                successfulBite = true;
            }
            else if (hit.collider.CompareTag("Cheese"))
            {
                TryCheeseBite(hit.collider.GetComponent<CheesePowerUp>());
                successfulBite = true;
            }
        }

        // 2. 🟠 Nessun hit col raggio → tentativo sferico ravvicinato
        if (!successfulBite)
        {
            PirateController bestTarget = null;
            float bestDot = -1f;

            Collider[] hits = Physics.OverlapSphere(transform.position, 0.8f, LayerMask.GetMask("PirateHittable"));

            foreach (Collider c in hits)
            {
                if (c.CompareTag("Pirate"))
                {
                    Vector3 toPirate = (c.transform.position - transform.position).normalized;
                    float dot = Vector3.Dot(transform.forward, toPirate);
                    if (dot > 0.5f && dot > bestDot)
                    {
                        Vector3 rayOrigin = origin;
                        Vector3 rayTarget = c.transform.position + Vector3.up * 0.5f;
                        Vector3 dir = (rayTarget - rayOrigin).normalized;
                        float dist = Vector3.Distance(rayOrigin, rayTarget);

                        // Occlusione → ignora se qualcosa blocca
                        if (!Physics.Raycast(rayOrigin, dir, dist, LayerMask.GetMask("Default", "Wall")))
                        {
                            bestDot = dot;
                            bestTarget = c.GetComponentInParent<PirateController>();
                        }
                    }
                }
            }

            if (bestTarget != null)
            {
                TryStartBite(bestTarget);
                successfulBite = true;
            }
            else
            {
                // 🧀 Tentativo ravvicinato su Cheese
                Collider[] cheeseHits = Physics.OverlapSphere(transform.position, 0.8f);
                foreach (Collider c in cheeseHits)
                {
                    if (c.CompareTag("Cheese"))
                    {
                        TryCheeseBite(c.GetComponent<CheesePowerUp>());
                        successfulBite = true;
                        break;
                    }
                }
            }
        }

        // 3. ❌ Fallito → comunque trigger l'animazione del morso
        if (!successfulBite)
        {
            TriggerFailedBite();
        }

        canBite = false;
        Invoke(nameof(ResetBiteCooldown), biteCooldown);
    }





    private void TryStartBite(PirateController controller )
    {
        if (controller == null) return;
        enemyController = controller;
        _ratInputHandler.movementLocked = true;
        biting = true;
        _ratAnimator.SetTrigger("Bite");
        StartCoroutine(StartQuickTimeEvent(enemyController));
        SetInvincibleForSeconds(3.0f);

        // ✅ Avvia un timer per resettare `biting`
        StartCoroutine(ResetBitingAfterDelay(1.2f)); // ← adatta il tempo alla durata dell’animazione
    }

    // NUOVO metodo per mordere un Medico (senza QuickTime Event)
    private void TryBiteDoctor(DocManager doctor)
    {
        if (doctor == null) return;

        _ratInputHandler.movementLocked = true;
        biting = true;
        _ratAnimator.SetTrigger("Bite");

        doctor.TakeDamage(Damage + bonusDamage);
        Debug.Log($"Il ratto ha morso il Medico {doctor.name} infliggendo {Damage + bonusDamage} danni.");
        StartCoroutine(StartQuickTimeEventDoctor(doctor));

        SetInvincibleForSeconds(3.0f);
        StartCoroutine(ResetBitingAfterDelay(1.2f));
    }

    private IEnumerator ResetBitingAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        biting = false;
        _ratInputHandler.movementLocked = false;  // 🔓 sblocca il ratto
        Debug.Log("biting = false dopo morso (timer)");
    }

    public void SetInvincibleForSeconds(float duration)
    {
        invincible = true;
        StartCoroutine(DisableInvincibilityAfterDelay(duration));
    }

    private IEnumerator DisableInvincibilityAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        invincible = false;
        Debug.Log("🐭 Il ratto non è più invincibile");
    }

    public void EndBite()
    {
        biting = false;
        Debug.Log("Fine animazione morso → biting = false");
    }

    private void TryCheeseBite(CheesePowerUp cheese)
    {
        if (cheese == null) return;
        _ratInputHandler.movementLocked = true;
        cheese.ActivatePowerUp(this);
        _ratAnimator.SetTrigger("BiteWithJumpBack");
        StartCoroutine(UnlockAfterAnimationFixed(1.5f));
    }

    private void TriggerFailedBite()
    {
        // Debug.Log("Nessun bersaglio trovato!");
        _ratInputHandler.movementLocked = true;
        _ratAnimator.SetTrigger("BiteWithJumpBack");
        invincible = false;
        StartCoroutine(UnlockAfterAnimationFixed(1.8f));
    }



    private void ResetBiteCooldown()
    {
        canBite = true;
    }


    // 👇 Nuovo: registra un pirata nella lista e si sottoscrive alla sua morte
    private void Infect(PirateController pirate)
    {
        if (!infectedPirates.Contains(pirate.transform))
        {
            infectedPirates.Add(pirate.transform);
            pirate.OnPirateDeath += RemoveDeadPirate;

        }
    }



    public void ActivateDamageBoost(int amount, GameObject prefab)
    {
        bonusDamage = amount;

        if (prefab != null)
        {

            if (damageVFXInstance != null) Destroy(damageVFXInstance);
            damageVFXInstance = Instantiate(prefab, transform.position, Quaternion.identity, transform);
            damageVFXInstance.transform.localPosition = Vector3.zero;
        }
    }

    private IEnumerator StartQuickTimeEvent(PirateController targetPirate)
    {
        Debug.Log("QTE INIZIATO");
        quickTimeConfirmed = false;
        isQuickTimeActive = true;

        quickTimeUIManager.StartQuickTime();
        float timer = 0f;

        while (quickTimeUIManager.IsQuickTimeActive)
        {
            timer += Time.deltaTime;
            if (quickTimeConfirmed) break;
            yield return null;
        }

        float precision = quickTimeUIManager.Precision;
        quickTimeUIManager.StopQuickTime();
        isQuickTimeActive = false;

        HandleQuickTimeResult(precision, quickTimeConfirmed, targetPirate);
    }

    private IEnumerator StartQuickTimeEventDoctor(DocManager targetPirate)
    {
        Debug.Log("QTE INIZIATO");
        quickTimeConfirmed = false;
        isQuickTimeActive = true;

        quickTimeUIManager.StartQuickTime();
        float timer = 0f;

        while (quickTimeUIManager.IsQuickTimeActive)
        {
            timer += Time.deltaTime;
            if (quickTimeConfirmed) break;
            yield return null;
        }

        float precision = quickTimeUIManager.Precision;
        quickTimeUIManager.StopQuickTime();
        isQuickTimeActive = false;

        HandleQuickTimeResultDoctor(precision, quickTimeConfirmed, targetPirate);
    }

    private void HandleQuickTimeResult(float precision, bool buttonPressed, PirateController targetPirate)
    {
        if (!buttonPressed)
        {
            Debug.Log("QuickTime fallito, nessun pulsante premuto.");
            _ratAnimator.SetTrigger("JumpBack"); // Animazione fallita
            StartCoroutine(UnlockAfterAnimationFixed(1f));


            return;
        }

        if (!HasCompletedFirstQuickTime && precision >= 0.5f && buttonPressed)
        {
            HasCompletedFirstQuickTime = true;
        }

        float currentScale = quickTimeUIManager.CurrentScale;
        float startScale = quickTimeUIManager.StartingScale;
        float scaleRatio = currentScale / startScale;

        Debug.Log("QuickTime scale ratio: " + scaleRatio);

        // Zone mapping
        if (scaleRatio > 0.87f || scaleRatio < 0.24f) // zone esterna e interna nere
        {
            //Debug.Log("❌ Fuori bersaglio (fallimento)");
            _ratAnimator.SetTrigger("JumpBack");
            StartCoroutine(UnlockAfterAnimationFixed(1f));
            return;
        }
        else if ((scaleRatio >= 0.75f && scaleRatio <= 0.87f) || (scaleRatio <= 0.38f && scaleRatio >= 0.24f))
        {
            vfxManager.PlayBiteVFX();
            isBackflipping = true;
            Debug.Log("🟡 Zona gialla");
            targetPirate.TakeDamage(Damage + bonusDamage);
            Infect(targetPirate);
            ExecuteBackflip(0.5f, 0.4f);
        }
        else if ((scaleRatio >= 0.63f && scaleRatio < 0.75f) || (scaleRatio <= 0.5 && scaleRatio > 0.38f))
        {
            vfxManager.PlayBiteVFX();
            isBackflipping = true;
            Debug.Log("🔵 Zona blu");
            targetPirate.TakeDamage(Damage + bonusDamage);
            Infect(targetPirate);
            ExecuteBackflip(1f, 0.7f);
        }
        else
        {
            vfxManager.PlayBiteVFX();
            isBackflipping = true;
            Debug.Log("🔴 Zona rossa");
            targetPirate.TakeDamage(Damage + bonusDamage);
            Infect(targetPirate);
            ExecuteBackflip(1.5f, 1f);
        }



        bonusDamage = 0;
        if (damageVFXInstance != null)
        {
            Destroy(damageVFXInstance);
            damageVFXInstance = null;
        }
    }
    private void HandleQuickTimeResultDoctor(float precision, bool buttonPressed, DocManager targetPirate)
    {
        if (!buttonPressed)
        {
            Debug.Log("QuickTime fallito, nessun pulsante premuto.");
            _ratAnimator.SetTrigger("JumpBack"); // Animazione fallita
            StartCoroutine(UnlockAfterAnimationFixed(1f));


            return;
        }

        if (!HasCompletedFirstQuickTime && precision >= 0.5f && buttonPressed)
        {
            HasCompletedFirstQuickTime = true;
        }

        float currentScale = quickTimeUIManager.CurrentScale;
        float startScale = quickTimeUIManager.StartingScale;
        float scaleRatio = currentScale / startScale;

        Debug.Log("QuickTime scale ratio: " + scaleRatio);

        // Zone mapping
        if (scaleRatio > 0.87f || scaleRatio < 0.24f) // zone esterna e interna nere
        {
            //Debug.Log("❌ Fuori bersaglio (fallimento)");
            _ratAnimator.SetTrigger("JumpBack");
            StartCoroutine(UnlockAfterAnimationFixed(1f));
            return;
        }
        else if ((scaleRatio >= 0.75f && scaleRatio <= 0.87f) || (scaleRatio <= 0.38f && scaleRatio >= 0.24f))
        {
            vfxManager.PlayBiteVFX();
            isBackflipping = true;
            Debug.Log("🟡 Zona gialla");
            targetPirate.TakeDamage(Damage + bonusDamage);
            //Infect(targetPirate);
            ExecuteBackflip(0.5f, 0.4f);
        }
        else if ((scaleRatio >= 0.63f && scaleRatio < 0.75f) || (scaleRatio <= 0.5 && scaleRatio > 0.38f))
        {
            vfxManager.PlayBiteVFX();
            isBackflipping = true;
            Debug.Log("🔵 Zona blu");
            targetPirate.TakeDamage(Damage + bonusDamage);
            //Infect(targetPirate);
            ExecuteBackflip(1f, 0.7f);
        }
        else
        {
            vfxManager.PlayBiteVFX();
            isBackflipping = true;
            Debug.Log("🔴 Zona rossa");
            targetPirate.TakeDamage(Damage + bonusDamage);
            //Infect(targetPirate);
            ExecuteBackflip(1.5f, 1f);
        }



        bonusDamage = 0;
        if (damageVFXInstance != null)
        {
            Destroy(damageVFXInstance);
            damageVFXInstance = null;
        }
    }



    private void ExecuteBackflip(float distanceMultiplier, float delayBeforeMove = 0.35f)
    {
        isBackflipping = true; // ← lo mettiamo subito, non dentro la coroutine
        StartCoroutine(PerformBackflip(distanceMultiplier, delayBeforeMove));
    }


    private IEnumerator PerformBackflip(float distanceMultiplier, float delayBeforeMove = 0.35f)
    {
        Debug.Log("BACKFLIP INIZIATO - isBackflipping = true");
        _ratAnimator.SetTrigger("Backflip");

        yield return new WaitForSeconds(delayBeforeMove); // aspetta inizio animazione

        Vector2 inputDir = _ratInputHandler.GetMoveInputRaw();
        Vector3 backwardDir = -transform.forward;

        if (inputDir.magnitude > 0.1f)
        {
            Vector3 desiredDir = new Vector3(inputDir.x, 0, inputDir.y);
            desiredDir = Camera.main.transform.TransformDirection(desiredDir);
            desiredDir.y = 0;
            desiredDir.Normalize();

            float angle = Vector3.Angle(backwardDir, desiredDir);
            if (angle <= 35f)
            {
                backwardDir = desiredDir;
            }
            else
            {
                backwardDir = Vector3.Slerp(backwardDir, desiredDir, 35f / angle);
            }
        }

        float backflipDistance = 2f * distanceMultiplier;
        float safetyMargin = 0.05f;

        // 👇 Raycast preventivo per accorciare la distanza
        if (Physics.Raycast(transform.position, backwardDir, out RaycastHit hit, backflipDistance + 0.1f, LayerMask.GetMask("Default", "Wall")))
        {
            backflipDistance = Mathf.Max(0f, hit.distance - safetyMargin); // non toccare il muro
            Debug.Log($"Backflip accorciato a {backflipDistance:F2} metri per ostacolo");
        }

        Vector3 startPos = rb.position;
        Vector3 targetPos = startPos + backwardDir * backflipDistance;

        float elapsed = 0f;
        float duration = 0.3f;

        while (elapsed < duration)
        {
            Vector3 nextPos = Vector3.Lerp(startPos, targetPos, elapsed / duration);
            rb.MovePosition(nextPos);
            elapsed += Time.fixedDeltaTime;
            yield return new WaitForFixedUpdate();
        }

        rb.MovePosition(targetPos);
        _ratInputHandler.movementLocked = false;
        isBackflipping = false;
        Debug.Log("BACKFLIP TERMINATO - isBackflipping = false");
      
    }




    private IEnumerator UnlockAfterAnimationFixed(float delay)
    {
        yield return new WaitForSeconds(delay);
        _ratInputHandler.movementLocked = false;
    }

    public void OnQuickTimeConfirm(InputAction.CallbackContext context)
    {
        if (context.performed && isQuickTimeActive)
        {
            quickTimeConfirmed = true;
        }
    }

    public GameObject EnablePoisonLeak(GameObject puddlePrefab, GameObject prefab)
    {
        GameObject puddle = Instantiate(poisonPrefab, transform.position, Quaternion.identity);

        // Se abbiamo salvato una configurazione di trappole, la passiamo ora
        if (trapPrefabsForNextPuddle != null)
        {
            PeeAttractor attractor = puddle.GetComponentInChildren<PeeAttractor>();
            if (attractor != null)
            {
                attractor.SetTrapMechanic(true, trapPrefabsForNextPuddle);
            }

            trapPrefabsForNextPuddle = null; // reset
        }


        if (prefab != null)
        {
            if (peeVFXInstance != null) Destroy(peeVFXInstance);
            peeVFXInstance = Instantiate(prefab, transform.position, Quaternion.identity, transform);
            peeVFXInstance.transform.localPosition = Vector3.zero;
        }

        return puddle;
    }


    public void OnPiss(InputAction.CallbackContext ctx)
    {
        if (ctx.performed && CanPee && poisonPrefab != null)
        {
            StartCoroutine(HandlePeeAction());
        }
    }

    private IEnumerator HandlePeeAction()
    {
        _ratInputHandler.movementLocked = true;

        // ❌ RIMUOVI questa riga:
        // GameObject puddle = Instantiate(poisonPrefab, transform.position, Quaternion.identity);

        // ✅ US A questo metodo in modo che:
        //   1. venga instanziata la puddle
        //   2. venga passato automaticamente il trapPrefabsForNextPuddle al PeeAttractor
        GameObject puddle = EnablePoisonLeak(poisonPrefab, null);

        yield return new WaitForSeconds(0.5f);

        _ratInputHandler.movementLocked = false;
        CanPee = false;

        if (peeVFXInstance != null)
        {
            Destroy(peeVFXInstance);
            peeVFXInstance = null;
        }
    }


    public void PreparePoisonLeak(GameObject puddlePrefab, GameObject vfxPrefab)
    {
        CanPee = true;
        poisonPrefab = puddlePrefab;

        if (peeVFXInstance != null)
            Destroy(peeVFXInstance);

        if (vfxPrefab != null)
        {
            peeVFXInstance = Instantiate(vfxPrefab, transform.position, Quaternion.identity, transform);
            peeVFXInstance.transform.localPosition = Vector3.zero;
        }
    }

    private GameObject[] trapPrefabsForNextPuddle;

    public void ConfigurePuddleTrap(GameObject[] trapPrefabs)
    {
        trapPrefabsForNextPuddle = trapPrefabs;
    }



    // 👇 Aggiungi questo metodo alla classe RatInteractionManager
    public void RegisterInfectedPirate(PirateController pirate)
    {
        if (!infectedPirates.Contains(pirate.transform))
        {
            infectedPirates.Add(pirate.transform);
            pirate.OnPirateDeath += RemoveDeadPirate;
            Debug.Log("Pirata infettato tramite pozza di veleno!");
        }
    }
    // 👇 Nuovo: rimuove il pirata morto
    private void RemoveDeadPirate(PirateController deadPirate)
    {
        if (infectedPirates.Contains(deadPirate.transform))
        {
            infectedPirates.Remove(deadPirate.transform);
        }
    }


    // Se ho preparato la pozza, ne recupero qui le trappole
    public GameObject[] GetTrapPrefabsForNextPuddle()
    {
        return trapPrefabsForNextPuddle;
    }


    // — Metodi per salvare e ripristinare i power-up attivi —
    public List<TrapType> GetActivePowerUps()
    {
        var list = new List<TrapType>();
        if (_ratInputHandler.speedBoostActive)
            list.Add(TrapType.Spring);
        if (IsDamageBoostActive)
            list.Add(TrapType.Glue);
        if (CanPee)
            list.Add(TrapType.Slide);
        return list;
    }

    public List<float> GetPowerUpRemainingDurations()
    {
        var durations = new List<float>();
        if (_ratInputHandler.speedBoostActive)
            durations.Add(_ratInputHandler.speedBoostRemainingTime);
        if (IsDamageBoostActive)
            durations.Add(GetCurrentDamageBoostAmount());
        if (CanPee)
            durations.Add(0f);   // flag, la puddle non ha durata
        return durations;
    }

    public List<GameObject[]> GetPuddleTrapPrefabs()
    {
        var list = new List<GameObject[]>();
        if (_ratInputHandler.speedBoostActive)
            list.Add(null);
        if (IsDamageBoostActive)
            list.Add(null);
        if (CanPee)
            list.Add(GetTrapPrefabsForNextPuddle());
        return list;
    }


    /// <summary>
    /// Ripristina i power-up dal GameStateManager usando solo flag + config.
    /// </summary>
    public void ApplyPowerUps(
        bool speedActive, float speedMul, float speedTime,
        bool damageActive, int damageAmt,
        bool poisonReady
    )
    {
        if (powerUpConfig == null) return;

        if (speedActive)
            StartCoroutine(
                _ratInputHandler.SpeedBoostRoutine(speedMul, speedTime)
            );

        if (damageActive)
            ActivateDamageBoost(damageAmt, powerUpConfig.damageVFXPrefab);

        if (poisonReady)
        {
            PreparePoisonLeak(
                powerUpConfig.poisonPuddlePrefab,
                powerUpConfig.poisonVFXPrefab
            );
            ConfigurePuddleTrap(powerUpConfig.poisonTrapPrefabs);
        }
    }



}
