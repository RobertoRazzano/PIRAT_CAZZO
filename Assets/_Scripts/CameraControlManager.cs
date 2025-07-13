using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using System.Collections;


public class CameraControlManager : MonoBehaviour
{

    private bool rotationLocked = false;
    [Header("References (assign in Inspector)")]
    public RatInputHandler ratController;
    public Transform ratTransform;

    [Header("Settings")]
    public KeyCode toggleKey = KeyCode.Escape;

    //[Header("Camera Offset & Rotation")]
    //public Vector3 cameraOffset = new Vector3(0f, 5f, -10f);
    //public Vector3 cameraEulerAngles = new Vector3(20f, 0f, 0f);

    public Coroutine deathZoomCoroutine;

    [Header("Transition Settings")]
    [Range(0.1f, 10f)] public float transitionSpeed = 3f;

    //private Transform camTransform;
    private bool followPirate = false;
    private Transform currentTarget;
    private Transform pirateTransform;

    [Header("Offset")]
    [Tooltip("Offset locale rispetto al target: X = spostamento laterale, Y = altezza, Z = distanza dietro")]
    public Vector3 offset = new Vector3(0f, 13f, -13f);

    [Header("Selection Zoom")]
    [Tooltip("Offset della camera in modalità selezione del topo")]
    public Vector3 selectionOffset = new Vector3(0f, 20f, -20f);

    // campo privato per salvare l’offset di default
    private Vector3 defaultOffset;
    private Vector3 currentOffset;
    private Vector3 targetOffset;

    public Vector3 cameraInitialPosition;
    public Vector3 cameraInitialRotation;


    [Header("Settings")]
    [Tooltip("Velocità di rotazione orizzontale")]
    public float sensitivity = 120f;

    [Header("Collisione Camera")]
    public LayerMask cameraCollisionMask;
    public float cameraMinDistance = 1f; // distanza minima di sicurezza dal topo

    private InputAction _lookAction;

    float yaw;
    Vector2 lookInput;



    void Start()
    {
        currentTarget = ratTransform;
        if (currentTarget == null)
        {
            Debug.LogError("CameraController: manca il riferimento a Target!");
            enabled = false;
            return;
        }

        yaw = currentTarget.eulerAngles.y;
        defaultOffset = offset;
        currentOffset = offset;
        targetOffset = offset;

        // Forza la posizione corretta al primo frame
        transform.position = currentTarget.position + (Quaternion.Euler(0f, yaw, 0f) * offset);
        transform.LookAt(currentTarget.position);
    }

    // --- NUOVO METODO ---
    /// <summary>
    /// Aggiorna il target che la camera deve seguire. Essenziale dopo il caricamento di una scena.
    /// </summary>
    public void UpdateTarget(Transform newTarget)
    {
        currentTarget = newTarget;
        ratTransform = newTarget; // Mantiene la coerenza
        Debug.Log($"Camera target updated to: {newTarget.name}");
    }




    public void LockRotation(bool locked)
    {
        rotationLocked = locked;
    }
    void Update()
    {
        /*if (Input.GetKeyDown(toggleKey))
        {
            followPirate = !followPirate;
            ratController.enabled = !followPirate;
            if (pirateTransform != null) currentTarget = followPirate ? pirateTransform : ratTransform;
        }*/
    }

    // Invocato dal PlayerInput → Invoke Unity Events sulla action "Look"
    public void OnLook(InputAction.CallbackContext ctx)
    {
        lookInput = ctx.ReadValue<Vector2>();
        Debug.Log($"Look input received: {lookInput}"); // Add this to debug
    }

    void LateUpdate()
    {

        if (currentTarget == null)
        {
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
                currentTarget = player.transform;
            else
                return;
        }
        // anche con rotationLocked=true, aggiorno sempre posizione e look,
        // soltanto la rotazione (yaw) viene bloccata
        if (!rotationLocked)
        {
            // aggiorna solo yaw (rotazione orizzontale)
            yaw += lookInput.x * sensitivity * Time.deltaTime;
        }

        // costruisci la rotazione orizzontale a partire dal yaw
        Quaternion rot = Quaternion.Euler(0f, yaw, 0f);

        // interpola lo zoom in modo smooth
        currentOffset = Vector3.Lerp(currentOffset, targetOffset, transitionSpeed * Time.deltaTime);

        // posizione e look
        Vector3 desiredCameraPos = currentTarget.position + rot * currentOffset;
        transform.position = desiredCameraPos;
        transform.LookAt(currentTarget.position);

    }


    public void SwitchToPirate(Transform pirate)
    {
        Debug.Log("Switching to pirate: " + pirate.name);
        pirateTransform = pirate;
        followPirate = true;
        ratController.enabled = false;
        currentTarget = pirateTransform;
    }

    public event System.Action OnSwitchedToRat;

    public void SwitchToRat()
    {
        followPirate = false;
        currentTarget = ratTransform;
        ratController.enabled = true;
        OnSwitchedToRat?.Invoke();
    }

    /// <summary>
    /// Zoom out per mostrare tutti gli strands dal topo in modalità selezione
    /// </summary>
    public void ApplySelectionZoom()
    {
        targetOffset = selectionOffset;
    }


    /// <summary>
    /// Ripristina lo zoom di default (usato quando entri in modalità possessione)
    /// </summary>
    public void ResetZoom()
    {
        targetOffset = defaultOffset;
    }

    /// <summary>
    /// Fa seguire alla camera un transform arbitrario (es. il TrailRat)
    /// </summary>
    public void FollowTrail(Transform trailTransform)
    {
        currentTarget = trailTransform;
        ratController.enabled = false;
    }

    void OnEnable()
    {
        // 1) Ogni volta che carica una scena, ricollego il Look
        SceneManager.sceneLoaded += OnSceneLoaded;

        // 2) Se la scena è già aperta (al primo avvio), collega subito
        TrySubscribeLook();
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        UnsubscribeLook();
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Ricollego l'azione Look del nuovo PlayerInput
        TrySubscribeLook();
    }

    /// <summary>
    /// Cerca in scena il PlayerInput, estrae l'azione "Look" e si iscrive al suo evento.
    /// </summary>
    private void TrySubscribeLook()
    {
        // Prima pulisco l'eventuale subscription precedente
        UnsubscribeLook();

        // Trova il PlayerInput (assume tu lo usi per il tuo ratto)
        var pi = FindFirstObjectByType<PlayerInput>();
        if (pi == null) return;

        // Ottengo l'azione "Look" da quella action map
        _lookAction = pi.actions["Look"];
        if (_lookAction == null) return;

        // Iscrivo il tuo metodo OnLook (già presente nella classe)
        _lookAction.performed += OnLook;
        _lookAction.Enable();
    }

    /// <summary>
    /// Rimuove la subscription alla Look action se esiste.
    /// </summary>
    private void UnsubscribeLook()
    {
        if (_lookAction != null)
        {
            _lookAction.performed -= OnLook;
            _lookAction.Disable();
            _lookAction = null;
        }
    }

    public void StartDeathZoom(float zoomMultiplier, float zoomSpeed, float duration)
    {
        LockRotation(true); // Blocca la rotazione durante lo zoom
        StartCoroutine(ZoomInOnRat(zoomMultiplier, zoomSpeed, duration));
    }

    public IEnumerator ZoomInOnRat(float zoomMultiplier, float zoomSpeed, float duration)
    {
        LockRotation(false); // La rotazione resta attiva

        Vector3 originalOffset = currentOffset;
        Vector3 zoomOffset = currentOffset / zoomMultiplier;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            targetOffset = Vector3.Lerp(originalOffset, zoomOffset, elapsed / duration);
            elapsed += Time.deltaTime * zoomSpeed;
            yield return null;
        }
        targetOffset = zoomOffset;
    }

    // --- METODO AGGIORNATO ---
    public void ResetCameraAfterRespawn()
    {
        // 1. Interrompi la coroutine dello zoom se è attiva
        if (deathZoomCoroutine != null)
        {
            StopCoroutine(deathZoomCoroutine);
            deathZoomCoroutine = null;
        }

        // 2. Sblocca la rotazione
        LockRotation(false);

        // 3. ⚠️ PRIMA resetta il yaw se il target è valido
        if (currentTarget != null)
        {
            yaw = currentTarget.eulerAngles.y;
        }

        // 4. Resetta l'offset dello zoom immediatamente
        targetOffset = defaultOffset;
        currentOffset = defaultOffset;

        // 5. Controlla che il target sia valido PRIMA di usarlo
        if (currentTarget == null)
        {
            Debug.LogError("CameraControlManager: Impossibile resettare la camera, currentTarget è nullo!");
            return;
        }

        // 6. Riposiziona la camera usando il yaw appena resettato
        transform.position = currentTarget.position + Quaternion.Euler(0f, yaw, 0f) * defaultOffset;
        transform.LookAt(currentTarget.position);

        Debug.Log($"Camera resettata: yaw={yaw}, position={transform.position}");
    }

    // --- METODO SEMPLIFICATO (ora non serve più chiamarlo separatamente) ---
    public void ResetYawOnRespawn()
    {
        if (currentTarget != null)
        {
            yaw = currentTarget.eulerAngles.y;
            Debug.Log($"Yaw resettato a: {yaw}");
        }
    }


}