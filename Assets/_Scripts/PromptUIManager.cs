using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.InputSystem;

public enum InputKeyType
{
    Down,
    Left,
    Right,
    Up,
    LeftTrigger,
    RightTrigger,
    LeftStick,
    RightStick,
    ButtonEast,
    ButtonWest,
    ButtonSouth,
    ButtonNorth
}

public class PromptUIManager : MonoBehaviour
{
    [Header("Root UI Elements")]
    [Tooltip("GameObject che contiene KeyIndicator + Text (TMP) + icons")]
    public GameObject inputContainer;       // es. InputContainer sotto il Canvas
    public TextMeshProUGUI promptText;      // il Text (TMP) per il messaggio

    [Header("Icon Buttons")]
    public GameObject downButton;
    public GameObject leftButton;
    public GameObject rightButton;
    public GameObject upButton;
    public GameObject leftTrigger;
    public GameObject rightTrigger;
    public GameObject leftStick;
    public GameObject rightStick;
    public GameObject buttonEast;
    public GameObject buttonWest;
    public GameObject buttonSouth;
    public GameObject buttonNorth;


    private Dictionary<InputKeyType, GameObject> _iconMap;
    private bool _isFrozen = false;
    private float _prevTimeScale = 1f;

    private InputKeyType _expectedKey;
    private bool _waitingForInput = false;
    public UnityEngine.UI.Image bersaglio;

    private InputAction continueDialogue;
    private InputAction rotateCamera;
    private InputAction exitSelectionMode;
    private InputAction piss;
    [SerializeField] private PlayerInput playerInput;

    private void Awake()
    {
        // build the map
        _iconMap = new Dictionary<InputKeyType, GameObject>()
        {
            { InputKeyType.Down,         downButton },
            { InputKeyType.Left,         leftButton },
            { InputKeyType.Right,        rightButton },
            { InputKeyType.Up,           upButton },
            { InputKeyType.LeftTrigger,  leftTrigger },
            { InputKeyType.RightTrigger, rightTrigger },
            { InputKeyType.LeftStick,    leftStick },
            { InputKeyType.RightStick,   rightStick },
            { InputKeyType.ButtonEast,   buttonEast },
            { InputKeyType.ButtonWest,   buttonWest },
            { InputKeyType.ButtonSouth,  buttonSouth },
            { InputKeyType.ButtonNorth,  buttonNorth }
        };

        // everything off at start
        inputContainer.SetActive(false);
        foreach (var go in _iconMap.Values)
            if (go != null) go.SetActive(false);

        continueDialogue = playerInput.actions["ContinueDialogue"];
        rotateCamera = playerInput.actions["Look"];
        exitSelectionMode = playerInput.actions["Exit Selection"];
        piss = playerInput.actions["Piss"];

    }

    void Update()
    {
        if (!_waitingForInput) return;

        switch (_expectedKey)
        {
            case InputKeyType.LeftStick:
                float x = Input.GetAxisRaw("Horizontal");
                float y = Input.GetAxisRaw("Vertical");
                if (x != 0 || y != 0 ||
                    Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.A) ||
                    Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.D))
                {
                    ConfirmPromptInput();
                }
                break;

            case InputKeyType.RightStick:
                // Solo movimento del mouse (valido per tastiera/mouse)
                if (Input.GetAxis("Mouse X") != 0 || Input.GetAxis("Mouse Y") != 0 || rotateCamera.triggered)
                    ConfirmPromptInput();
                break;

            case InputKeyType.ButtonSouth:
                if (Input.GetKeyDown(KeyCode.Escape) || exitSelectionMode.triggered) // A / Cross
                {
                    ConfirmPromptInput();
                }
                break;

            case InputKeyType.ButtonEast:
                if (
                    Input.GetKeyDown(KeyCode.Space) ||
                    Input.GetKeyDown(KeyCode.Return) ||
                    (continueDialogue != null && continueDialogue.triggered)
                )
                {
                    bersaglio.gameObject.SetActive(false);
                    ConfirmPromptInput();
                }
                break;
            
            case InputKeyType.ButtonWest:
                if (Input.GetKeyDown(KeyCode.P) || piss.triggered)
                {
                    ConfirmPromptInput();
                }
                break;

            case InputKeyType.LeftTrigger:
                if (Input.GetKeyDown(KeyCode.LeftShift) || Input.GetKeyDown(KeyCode.JoystickButton6) || Input.GetKeyDown(KeyCode.Tab)) // LT
                {
                    ConfirmPromptInput();
                }
                break;

            case InputKeyType.RightTrigger:
                if (Input.GetKeyDown(KeyCode.Mouse0) || Input.GetKeyDown(KeyCode.JoystickButton7))
                {
                    ConfirmPromptInput();
                }
                break;


            // Aggiungi altri case se servono

            default:
                Debug.LogWarning($"Input non gestito per {_expectedKey}");
                break;
        }
    }

    private void ConfirmPromptInput()
    {
        _waitingForInput = false;
        HidePrompt(); // questo già ripristina timeScale
    }

    /// <summary>
    /// Mostra la UI prompt con il testo e l�icon selezionato.
    /// Se freezeTime=true, blocca Time.timeScale.
    /// </summary>
    public void ShowPrompt(InputKeyType key, string message, bool freezeTime = false)
    {
        _expectedKey = key;
        _waitingForInput = freezeTime;  // ci interessa solo se è bloccato

        // freeze time?
        if (freezeTime && !_isFrozen)
        {
            _prevTimeScale = Time.timeScale;
            Time.timeScale = 0f;
            _isFrozen = true;
        }

        // testo
        promptText.text = message;

        // icon
        foreach (var kv in _iconMap)
            if (kv.Value != null)
                kv.Value.SetActive(kv.Key == key);

        // container on
        inputContainer.SetActive(true);
    }

    public void ShowText(string message, bool freezeTime = true)
    {
        _expectedKey = InputKeyType.ButtonEast;
        _waitingForInput = freezeTime;  // ci interessa solo se è bloccato

        // freeze time?
        if (freezeTime && !_isFrozen)
        {
            _prevTimeScale = Time.timeScale;
            Time.timeScale = 0f;
            _isFrozen = true;
        }

        // testo
        promptText.text = message;

        // container on
        inputContainer.SetActive(true);
    }

    /// <summary>
    /// Nasconde la UI prompt e ripristina il Time.timeScale se era frozen.
    /// </summary>
    public void HidePrompt()
    {
        // 1) riporta il timeScale, se era congelato
        if (_isFrozen)
        {
            Time.timeScale = _prevTimeScale;
            _isFrozen = false;
        }

        // 2) pulisce immediatamente il testo
        promptText.text = "";

        // 3) nasconde il container e tutte le icone
        inputContainer.SetActive(false);
        foreach (var go in _iconMap.Values)
            if (go != null)
                go.SetActive(false);
    }
}