using UnityEngine;
using UnityEngine.InputSystem;

public class TriggerFinePossessione : MonoBehaviour
{
    private bool hasTriggered = true;
    public PromptUIManager promptUIManager;
    public DialogueManager dialogueManager;
    public DialogueSequence dialogueStartGame;
    private InputAction exitSelectionMode;
    [SerializeField] private PlayerInput playerInput;
    private bool firstTime = true;
    public PossessionManager possessionManager;
    public GameObject boccaporto;

    void Start()
    {
        exitSelectionMode = playerInput.actions["Exit Selection"];
    }

    void Update()
    {
        if (exitSelectionMode.triggered && firstTime && possessionManager.CurrentState == PossessionState.Possessing)
        {
            promptUIManager.HidePrompt();
            dialogueManager.StartDialogue(dialogueStartGame);
            firstTime = false;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!hasTriggered) return;
        if (other.CompareTag("Pirate") && other.gameObject == boccaporto)
        {
            hasTriggered = false;
            promptUIManager.ShowPrompt(InputKeyType.ButtonSouth, "Return to rat with this button or ESCAPE", true);
        }
    }
}
