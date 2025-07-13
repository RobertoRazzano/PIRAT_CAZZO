using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class DialogueManagerFloor1 : MonoBehaviour
{
[Header("UI")]
    public GameObject dialoguePanel;
    public TextMeshProUGUI speakerText;
    public TextMeshProUGUI dialogueText;

    [Header("Data")]
    public DialogueSequence dialogueSequence;

    [Header("Al termine del dialogo")]
    public GameObject[] objectsToDisable;

    private int currentIndex = 0;
    private bool isDialogueActive = false;

    private InputAction continueAction;

    [SerializeField] private PlayerInput playerInput;
    public PirateAutoMove pirateAutoMove;

    void Start()
    {
        continueAction = playerInput.actions["ContinueDialogue"];
        StartDialogue(dialogueSequence);
    }

    void Update()
    {
        if (!isDialogueActive) return;
        if (continueAction.triggered)
        {
            ShowNextLine();
        }
    }

    public void StartDialogue(DialogueSequence sequence)
    {
        if (sequence == null || sequence.lines.Count == 0)
        {
            Debug.LogWarning("DialogueSequence vuota o nulla.");
            return;
        }

        dialogueSequence = sequence;
        currentIndex = 0;
        isDialogueActive = true;

        dialoguePanel.SetActive(true);
        ShowNextLine();
    }

    private void ShowNextLine()
    {
        if (currentIndex >= dialogueSequence.lines.Count)
        {
            EndDialogue();
            return;
        }

        DialogueLine line = dialogueSequence.lines[currentIndex];
        speakerText.text = line.speakerName;
        dialogueText.text = line.text;

        currentIndex++;
    }

    private void EndDialogue()
    {
        isDialogueActive = false;
        dialoguePanel.SetActive(false);

        foreach (var go in objectsToDisable)
        {
            if (go != null)
                go.SetActive(false);
        }
        pirateAutoMove?.MoveToTarget();
    }
}
