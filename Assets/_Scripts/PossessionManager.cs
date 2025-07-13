using UnityEngine;
using System.Collections.Generic;
using UnityEngine.InputSystem;
using UnityEngine.AI;
using UnityEngine.VFX;

public enum PossessionState { Idle, Selecting, FollowingTrail, Possessing }


public class PossessionManager : MonoBehaviour
{
    [Header("Riferimenti")]
    public RatInteractionManager ratInteraction;
    public GameObject sciaPrefab;
    public DynamicRevealerFollower dynamicRevealerFollower;    // assegna lo script che segue il buco di nebbia
    public float sciaHeightOffset = 1f;                        // quanto alzare le linee sopra quel punto


    public Transform ratTransform;
    public RatInputHandler ratInput;
    public CameraControlManager cameraManager;

    public GameObject trailPrefab;                  // prefabricato con NavMeshAgent
    public float trailStoppingDistance = 0.5f;      // distanza di arrivo del trail
    private TrailRatController currentTrail;        // componente del trail istanziato
    private Transform currentTrailTarget;           // pirata da inseguire


    [Header("Impostazioni selezione")]
    public float maxSelectionDistance = 15f;

    private PossessionState currentState = PossessionState.Idle;
    private int selectedIndex = -1;
    private bool canSwitchBackToRat = true;

    // private List<LineRenderer> scieAttive = new List<LineRenderer>();
    private List<VisualEffect> scieAttive = new List<VisualEffect>();
    private Animator ratAnimator;

    public PlayerInput playerInput;
    private InputAction moveAction;

    public PossessionState CurrentState => currentState;
    public GameObject oculiVolume;
    public GameObject globalVolume;
    public Color defaultColor;
    public Color selectedColor;

    [Header("OculiSound")]
    [SerializeField] private AudioSource oculiAudioSource;
    [SerializeField] private AudioSource oculiRiserSource;

    [SerializeField] private AudioClip oculiRiserIn;
    [SerializeField] private AudioClip oculiRiserOut;
    [SerializeField] private AudioClip oculiNexumAudio;

    void Start()
    {
        if (cameraManager != null)
            cameraManager.OnSwitchedToRat += HandleReturnToRat;

        ratAnimator = ratTransform.GetComponent<Animator>();

        // Ottieni riferimento al PlayerInput
        playerInput = GetComponent<PlayerInput>();
        moveAction = playerInput.actions["Move"];

        globalVolume.SetActive(true);
        oculiVolume.SetActive(false);
    }

    void OnDestroy()
    {
        if (cameraManager != null)
            cameraManager.OnSwitchedToRat -= HandleReturnToRat;
    }

    void Update()
    {
        if (currentState == PossessionState.Selecting)
        {
            HandleSelectionInput();
        }
    }

    public void EnablePossessionInput(bool enable)
    {
        var selection = playerInput.actions["Selection"];
        var confirm = playerInput.actions["Possess"];
        var exit = playerInput.actions["Exit Selection"];

        if (enable)
        {
            selection.performed += EnterSelectionMode_Input;
            confirm.performed += ConfirmPossess_Input;
            exit.performed += ExitSelectionMode_Input;
        }
        else
        {
            selection.performed -= EnterSelectionMode_Input;
            confirm.performed -= ConfirmPossess_Input;
            exit.performed -= ExitSelectionMode_Input;
        }
    }
    void EnterSelectionMode()
    {
        var piratesInRange = GetPiratesInRange();

        if (piratesInRange.Count == 0)
        {
            Debug.Log("Nessun pirata infetto nel raggio di selezione.");
            return;
        }

        currentState = PossessionState.Selecting;

        // 🎵 Play audio quando si entra in modalità Selecting
        if (oculiRiserSource != null && oculiRiserIn != null)
        {
            oculiRiserSource.loop = false;
            oculiRiserSource.clip = oculiRiserIn;
            oculiRiserSource.Play();
            
        }

        if (oculiAudioSource != null && oculiNexumAudio != null)
        {
            oculiAudioSource.clip = oculiNexumAudio;
            oculiAudioSource.loop = true;
            oculiAudioSource.Play();
        }

        // zoom out camera per vedere tutti gli strands del topo
        cameraManager.ApplySelectionZoom();

        // ✅ Auto-selezione se c’è solo un pirata
        selectedIndex = 0;


        AggiornaScie(piratesInRange);
        ShowScie();

        if (ratInput != null)
        {
            ratInput.enabled = false;
            ratInput.movementLocked = true;
        }

        if (ratAnimator != null)
            ratAnimator.SetBool("isWalking", false);

        currentState = PossessionState.Selecting;
        UpdateVolumes();
    }

    void HandleSelectionInput()
    {
        var piratesInRange = GetPiratesInRange();

        if (piratesInRange.Count == 0)
        {
            Debug.Log("Nessun pirata nel raggio di selezione: uscita dalla modalità.");
            ExitSelectionMode();
            return;
        }

        // Se il selezionato non è più valido, resetta selezione
        if (selectedIndex >= piratesInRange.Count || !piratesInRange.Contains(ratInteraction.infectedPirates[selectedIndex]))
        {
            Debug.Log("Il pirata selezionato è uscito dal raggio. Reset selezione.");
            selectedIndex = 0; // oppure -1 se vuoi che non ci sia selezione automatica
        }

        // Input direzionale per cambiare target
        Vector2 inputDir = moveAction.ReadValue<Vector2>();
        if (inputDir != Vector2.zero)
            SelectClosestInDirection(inputDir.normalized, piratesInRange);

        // Aggiorna le scie solo per i pirati validi
        AggiornaScie(piratesInRange);
    }


    private void ConfirmSelection(List<Transform> piratesInRange)
    {
        HideScie();

        // disabilita input e animator del ratto
        if (ratInput != null)
        {
            ratInput.enabled = false;
            ratInput.movementLocked = true;
        }
        if (ratAnimator != null)
            ratAnimator.SetBool("isWalking", false);

        // cattura il target PRIMA di resettare selectedIndex
        Transform target = piratesInRange[selectedIndex];
        currentTrailTarget = target;

        ExitSelectionMode(skipVolumeUpdate: true);
        currentState = PossessionState.FollowingTrail;

        Vector3 spawnPos = ratTransform.position;
        if (NavMesh.SamplePosition(spawnPos, out NavMeshHit hit, 1f, NavMesh.AllAreas))
            spawnPos = hit.position;
        GameObject go = Instantiate(trailPrefab, spawnPos, Quaternion.identity);
        currentTrail = go.GetComponent<TrailRatController>();
        // <-- nuova patch qui
        var agent = go.GetComponent<NavMeshAgent>();
        agent.stoppingDistance = trailStoppingDistance;
        // <-- fine patch
        currentTrail.OnArrived += OnTrailArrived;
        currentTrail.MoveTo(currentTrailTarget);

        cameraManager.ResetZoom();
        cameraManager.FollowTrail(currentTrail.transform);
        cameraManager.LockRotation(true);
        UpdateVolumes();
    }

    void ExitSelectionMode(bool skipVolumeUpdate = false)
    {
        selectedIndex = -1;
        HideScie();
        cameraManager.ResetZoom();

        if (currentState == PossessionState.Selecting)
        {
            currentState = PossessionState.Idle;

            // 🎵 Play uscita e stop loop
            if (oculiRiserSource != null && oculiRiserOut != null)
            {
                oculiRiserSource.loop = false;
                oculiRiserSource.clip = oculiRiserOut;
                oculiRiserSource.Play();
            }

            if (oculiAudioSource != null)
            {
                oculiAudioSource.Stop();
                oculiAudioSource.loop = false;
            }

            if (ratInput != null)
            {
                ratInput.enabled = true;
                ratInput.movementLocked = false;
            }

            if (ratAnimator != null)
                ratAnimator.SetBool("isWalking", false);
        }

        currentState = PossessionState.Idle;
        if (!skipVolumeUpdate)
            UpdateVolumes();
    }

    void SwitchToRat()
    {
        foreach (Transform p in ratInteraction.infectedPirates)
        {
            PirateController pc = p.GetComponent<PirateController>();
            if (pc != null) pc.isPossessed = false;

            SetEyesActive(p, false); // 👈 aggiungi questa riga
        }

        cameraManager.SwitchToRat();

        if (ratInput != null)
        {
            ratInput.enabled = true;
            ratInput.movementLocked = false;
        }

        if (ratAnimator != null)
            ratAnimator.SetBool("isWalking", false);

        currentState = PossessionState.Idle;
        canSwitchBackToRat = false;
        Invoke(nameof(EnableSwitchBack), 0.2f);

        currentState = PossessionState.Idle;
        UpdateVolumes();
    }

    void EnableSwitchBack() => canSwitchBackToRat = true;

    void HandleReturnToRat()
    {
        currentState = PossessionState.Idle;
    }
    // ─────────────────────────────────────────────────────────────────
    // Chiamato quando il giocatore subisce un attacco:
    // esce da Selection o da Possession a prescindere dallo stato corrente.
    public void OnAttacked()
    {
        if (currentState == PossessionState.Selecting)
        {
            ExitSelectionMode();
        }
        else if (currentState == PossessionState.FollowingTrail)
        {
            InterruptTrail();
            cameraManager.SwitchToRat();
            cameraManager.ResetZoom();
            cameraManager.LockRotation(false);
            ExitSelectionMode();
        }

        else if (currentState == PossessionState.Possessing)
        {
            // torna automaticamente al ratto
            SwitchToRat();
        }

        UpdateVolumes();
    }
    // ─────────────────────────────────────────────────────────────────

    void SelectClosestInDirection(Vector2 inputDir, List<Transform> piratesInRange)
    {
        float bestDot = -1f;
        int bestIndex = -1;

        for (int i = 0; i < piratesInRange.Count; i++)
        {
            Vector3 toPirate = piratesInRange[i].position - ratTransform.position;
            Vector2 toPirate2D = new Vector2(toPirate.x, toPirate.z).normalized;
            float dot = Vector2.Dot(inputDir, toPirate2D);

            if (dot > bestDot)
            {
                bestDot = dot;
                bestIndex = i;
            }
        }

        if (bestIndex != -1)
        {
            selectedIndex = bestIndex;
            Debug.Log("Pirata selezionato: " + piratesInRange[selectedIndex].name);
        }
    }

    void AggiornaScie(List<Transform> piratesInRange)
    {
        if (currentState != PossessionState.Selecting) return;

        while (scieAttive.Count < piratesInRange.Count)
        {
            var newSciaInstance = Instantiate(sciaPrefab);
            var newVFX = newSciaInstance.GetComponentInChildren<VisualEffect>();
            if (newVFX != null)
            {
                var renderer = newVFX.GetComponent<Renderer>();
                if (renderer != null && renderer.sharedMaterial != null)
                    renderer.sharedMaterial.renderQueue = 4000;

                newSciaInstance.SetActive(true);
                scieAttive.Add(newVFX);
            }
            else // <-- AGGIUNGI QUESTO BLOCCO ELSE
            {
                //Debug.LogError("Il prefab 'sciaPrefab' non contiene un componente VisualEffect! Impossibile creare la scia. Interrompo il ciclo per evitare un crash.");
                Destroy(newSciaInstance); // Distruggi l'istanza inutile appena creata
                break; // <-- Esci forzatamente dal ciclo
            }
            /* var newScia = Instantiate(sciaPrefab).GetComponent<LineRenderer>();
            newScia.material.renderQueue = 4000;
            newScia.gameObject.SetActive(false);
            scieAttive.Add(newScia); */
        }

        while (scieAttive.Count > piratesInRange.Count)
        {
            Destroy(scieAttive[scieAttive.Count - 1].gameObject);
            scieAttive.RemoveAt(scieAttive.Count - 1);
        }

        for (int i = 0; i < piratesInRange.Count; i++)
        {
            var scia = scieAttive[i];
            var target = piratesInRange[i];

            // calcola il punto di partenza sopra il buco di nebbia (o fallback sul topo)
            Vector3 start = ratTransform.position + Vector3.up * sciaHeightOffset;
            if (dynamicRevealerFollower != null)
                start = dynamicRevealerFollower.transform.position + Vector3.up * sciaHeightOffset;

            // calcola il punto di arrivo leggermente più in alto sul pirata
            Vector3 end = target.position + Vector3.up * sciaHeightOffset;

            //scia.SetPosition(0, start);
            //scia.SetPosition(1, end);
            var startWorld = (dynamicRevealerFollower != null ? dynamicRevealerFollower.transform.position : ratTransform.position) + Vector3.up * sciaHeightOffset;
            var endWorld = target.position + Vector3.up * sciaHeightOffset;

            var localStart = scia.transform.InverseTransformPoint(startWorld);
            var localEnd = scia.transform.InverseTransformPoint(endWorld);

            scia.SetVector3("Pos1", localStart);
            scia.SetVector3("Pos2", localEnd);

            /* if (scia.material != null)
            {
                scia.material.color = (i == selectedIndex) ? selectedColor : defaultColor;
            } */
            scia.SetVector4("Color1", (i == selectedIndex) ? selectedColor : defaultColor);
        }
    }

    public void ShowScie()
    {
        foreach (var scia in scieAttive)
            scia.gameObject.SetActive(true);
    }

    public void HideScie()
    {
        foreach (var scia in scieAttive)
            scia.gameObject.SetActive(false);
    }

    private List<Transform> GetPiratesInRange()
    {
        if (ratInteraction == null || ratInteraction.infectedPirates == null)
            return new List<Transform>();

        return ratInteraction.infectedPirates.FindAll(p =>
        {
            // Escludi pirati morti
            PirateController pc = p.GetComponent<PirateController>();
            if (pc == null || pc.IsDead)
            {
                Debug.Log($"{p.name} escluso: {(pc == null ? "nessun PirateController" : "è morto")}");
                return false;
            }

            // Dentro il raggio di selezione
            return Vector3.Distance(p.position, ratTransform.position) <= maxSelectionDistance;
        });
    }


    // Metodo per entrare nella modalità selezione
    public void EnterSelectionMode_Input(InputAction.CallbackContext context)
    {
        // reagisci soltanto alla fase "performed" dell'azione Selection
        if (!context.performed)
            return;

        // da Idle → Selecting
        if (currentState == PossessionState.Idle)
            EnterSelectionMode();
    }

    // Metodo per uscire dalla modalità selezione o possessione
    public void ExitSelectionMode_Input(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            if (currentState == PossessionState.Selecting)
                ExitSelectionMode();
            else if (currentState == PossessionState.Possessing && canSwitchBackToRat)
                SwitchToRat();
        }
    }

    // Metodo per confermare la selezione del pirata da possedere
    public void ConfirmPossess_Input(InputAction.CallbackContext context)
    {
        if (!context.performed)
            return;

        // conferma solo se sei già in Selecting
        if (currentState != PossessionState.Selecting)
            return;

        // conferma il possesso
        var piratesInRange = GetPiratesInRange();
        if (piratesInRange.Count == 0)
            return;

        ConfirmSelection(piratesInRange);
    }

    private void OnTrailArrived()
    {
        // 1) riattacca camera al pirata
        cameraManager.SwitchToPirate(currentTrailTarget);
        cameraManager.ResetZoom();
        cameraManager.LockRotation(false);

        // 2) distruggi il Trail
        currentTrail.OnArrived -= OnTrailArrived;
        Destroy(currentTrail.gameObject);
        currentTrail = null;

        // 3) completa il possesso
        currentState = PossessionState.Possessing;
        PirateController pc = currentTrailTarget.GetComponent<PirateController>();
        if (pc != null) pc.isPossessed = true;
        SetEyesActive(currentTrailTarget, true);

        currentTrailTarget = null;

        currentState = PossessionState.Possessing;
        UpdateVolumes();
    }


    private void InterruptTrail()
    {
        if (currentTrail != null)
        {
            currentTrail.OnArrived -= OnTrailArrived;
            Destroy(currentTrail.gameObject);
            currentTrail = null;
        }
    }

    private void UpdateVolumes()
    {
        bool showOculi = currentState == PossessionState.Selecting
                    || currentState == PossessionState.FollowingTrail
                    || currentState == PossessionState.Possessing;

        oculiVolume.SetActive(showOculi);
        globalVolume.SetActive(!showOculi);

        Debug.Log($"[UpdateVolumes] Stato: {currentState}, OculiVolume: {(showOculi ? "ON" : "OFF")}");
    }


    private void SetEyesActive(Transform pirate, bool active)
    {
        var eyes = FindChildByNameRecursive(pirate, "Eyes");
        if (eyes != null)
        {
            eyes.gameObject.SetActive(active);
        }
        else
        {
            Debug.LogWarning($"Nessun oggetto 'Eyes' trovato in {pirate.name}");
        }
    }

    private Transform FindChildByNameRecursive(Transform parent, string targetName)
    {
        foreach (Transform child in parent)
        {
            if (child.name == targetName)
                return child;

            var result = FindChildByNameRecursive(child, targetName);
            if (result != null)
                return result;
        }
        return null;
    }
}