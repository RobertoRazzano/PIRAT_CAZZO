using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

[RequireComponent(typeof(Rigidbody))]
public class RatInputHandler : MonoBehaviour
{
    private GameObject speedVFXInstance;


    private float walkBufferTimer = 0f;
    private const float walkBufferDuration = 0.15f;

    [Header("Movement")]
    public float walkSpeed = 5f;
    public float sprintMultiplier = 1.5f;

    [Tooltip("Gradi al secondo per ruotare verso la direzione di movimento")]
    public float rotationSpeed = 360f;

    [Header("Animation")]
    [Tooltip("Parametro Animator per gestire la velocit� dell'animazione")]
    public string animSpeedParam = "SpeedMultiplier";
    [Tooltip("Moltiplicatore minimo della velocit� dell'animazione quando l'input � minimo")]
    public float minAnimSpeed = 0.5f;
    [Tooltip("Moltiplicatore massimo della velocit� dell'animazione quando l'input � a intensit� massima")]
    public float maxAnimSpeed = 1.6f;

    private Rigidbody rb;
    public Vector2 moveInput { get; private set; }
    [HideInInspector]
    public bool movementLocked = false;

    private bool isSprinting;
    private Animator _ratAnimator;
    public bool speedBoostActive { get; private set; } = false;
    // — Tracciamento SpeedBoost —
    public float currentSpeedBoostMultiplier { get; private set; }
    public float speedBoostRemainingTime { get; private set; }

    
    public LayerMask stairLayer; // Layer delle scale/zone dove la Y non deve essere ancorata
    private RigidbodyConstraints originalConstraints; // Per salvare i vincoli originali del Rigidbody
    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        _ratAnimator = GetComponent<Animator>();
        // --- MODIFICA ---
        // Salva i vincoli originali del Rigidbody.
        // Assicurati che nel Rigidbody del topo, in Unity Editor,
        // la "Position Y" sia INIZIALMENTE spuntata sotto "Constraints" -> "Freeze Position".
        // Questo sarà il comportamento predefinito.
        originalConstraints = rb.constraints;
        // --- FINE MODIFICA ---
    }

    private void Start()
    {
        _ratAnimator = GetComponent<Animator>();
        _ratAnimator.SetBool("isWalking", false);
        _ratAnimator.SetFloat(animSpeedParam, minAnimSpeed);
    }

    // Invocato da PlayerInput
    public void OnMove(InputAction.CallbackContext ctx)
    {
        moveInput = ctx.ReadValue<Vector2>();
    }

    public void OnSprint(InputAction.CallbackContext ctx)
    {
        isSprinting = ctx.ReadValueAsButton();
    }

    void FixedUpdate()
    {
        // Calcola direzione di movimento in world space
        Vector3 forward = Camera.main.transform.forward;
        Vector3 right = Camera.main.transform.right;
        forward.y = 0; right.y = 0;
        forward.Normalize(); right.Normalize();


        Vector3 desiredMove;
        if (movementLocked)
        {
            // se bloccato, nessun movimento
            desiredMove = Vector3.zero;
        }
        else
        {
            desiredMove = forward * moveInput.y + right * moveInput.x;
        }

        float currentSpeed = walkSpeed * (isSprinting ? sprintMultiplier : 1f);


        // Rotazione in loco verso desiredMove
        if (desiredMove.sqrMagnitude > 0.001f)
        {
            Quaternion targetRot = Quaternion.LookRotation(desiredMove, Vector3.up);
            // usa Rigidbody per ruotare, mantenendo la sim physics
            Quaternion newRot = Quaternion.Slerp(
                transform.rotation,
                targetRot,
                rotationSpeed * Time.fixedDeltaTime
            );
            
            rb.MoveRotation(newRot);
        }

        // Movimento
        rb.MovePosition(rb.position + desiredMove * currentSpeed * Time.fixedDeltaTime);

        // Animazioni
        UpdateWalkingAnimation(desiredMove.magnitude);

        
    }


    private void UpdateWalkingAnimation(float inputMagnitude)
    {
        if (movementLocked)
        {
            _ratAnimator.SetBool("isWalking", false); // Blocca l'animazione
            return; // Non aggiornare ulteriormente
        }

        bool isInputActive = moveInput.magnitude > 0.05f;

        if (isInputActive)
        {
            walkBufferTimer = walkBufferDuration;
        }
        else
        {
            walkBufferTimer -= Time.fixedDeltaTime;
        }

        bool walking = walkBufferTimer > 0f;
        _ratAnimator.SetBool("isWalking", walking);

        // Aggiusta velocità dell'animazione
        var state = _ratAnimator.GetCurrentAnimatorStateInfo(0);
        if (state.IsName("WalkRatAnimation"))
        {
            float t = Mathf.Clamp01(inputMagnitude);
            float baseSpeed = 5f;
            float speedFactor = walkSpeed / baseSpeed;
            float animSpeed = Mathf.Lerp(minAnimSpeed, maxAnimSpeed, t) * speedFactor;
            _ratAnimator.SetFloat(animSpeedParam, animSpeed);
        }
        else
        {
            _ratAnimator.SetFloat(animSpeedParam, 1f);
        }
    }

    void OnTriggerEnter(Collider other)
    {
        // Se il collider appartiene al layer delle scale, disabilita l'ancoraggio Y
        if (((1 << other.gameObject.layer) & stairLayer) != 0)
        {
            rb.constraints &= ~RigidbodyConstraints.FreezePositionY;
            Debug.Log("Ancoraggio Y disabilitato (su scale/rampe)");
        }
    }

    void OnTriggerExit(Collider other)
    {
        // Quando esci dal collider delle scale, riabilita l'ancoraggio Y
        if (((1 << other.gameObject.layer) & stairLayer) != 0)
        {

            rb.constraints |= RigidbodyConstraints.FreezePositionY; // Aggiungi il vincolo FreezePositionY
            Debug.Log("Ancoraggio Y riabilitato (fuori da scale/rampe)");
            // Opzionale: Resetta la Y al valore iniziale quando esci dalle scale per "riportarlo a terra"
            // initialY = transform.position.y; // Se vuoi che la nuova "quota zero" sia dove esci dalle scale
        }
    }



    public void SetSpeedVFX(GameObject prefab)
    {
        if (prefab == null) return;
        if (speedVFXInstance != null) Destroy(speedVFXInstance);
        speedVFXInstance = Instantiate(prefab, transform.position, Quaternion.identity, transform);
        speedVFXInstance.transform.localPosition = Vector3.zero;
    }

    public IEnumerator SpeedBoostRoutine(float multiplier, float duration)
    {
        currentSpeedBoostMultiplier = multiplier;
        speedBoostRemainingTime = duration;
        speedBoostActive = true;

        float originalSpeed = walkSpeed;
        walkSpeed = walkSpeed * multiplier;

        // Gestione manuale del countdown
        while (speedBoostRemainingTime > 0f)
        {
            speedBoostRemainingTime -= Time.deltaTime;
            yield return null;
        }

        walkSpeed = originalSpeed;
        speedBoostActive = false;

        if (speedVFXInstance != null)
        {
            Destroy(speedVFXInstance);
            speedVFXInstance = null;
        }
    }


    public Vector2 GetMoveInputRaw() => moveInput;
}
