using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class DialogueManagerPonte : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI leftSpeakerText;
    [SerializeField] private TextMeshProUGUI leftDialogueText;
    [SerializeField] private GameObject leftDialogueBox;

    [SerializeField] private TextMeshProUGUI rightSpeakerText;
    [SerializeField] private TextMeshProUGUI rightDialogueText;
    [SerializeField] private GameObject rightDialogueBox;

    [Header("Dialogue Data")]
    [SerializeField] private DialogueSequence finalDialogue;
    private DialogueSequence currentSequence;
    private int currentIndex = 0;
    private bool isDialogueActive = false;
    public System.Action OnDialogueEnded;
    private bool waitingForEndConfirmation = false;
    public PromptUIManager promptUIManager;
    public QuickTimeUIManager quickTimeUIManager;
    private InputAction continueDialogue;
    [SerializeField] private PlayerInput playerInput;

    void Start()
    {
        continueDialogue = playerInput.actions["ContinueDialogue"];
        leftDialogueBox.SetActive(false);
        rightDialogueBox.SetActive(false);
    }


    void Update()
    {
        if (isDialogueActive && continueDialogue != null && continueDialogue.triggered)
        {
            ShowNextLine();
        }
    }

    public void StartDialogue(DialogueSequence sequence)
    {
        promptUIManager.HidePrompt();
        isDialogueActive = true;
        waitingForEndConfirmation = false;

        currentIndex = 0;
        currentSequence = sequence; // 👈 AGGIUNGI QUESTA RIGA

        leftDialogueBox.SetActive(false);
        rightDialogueBox.SetActive(false);

        ShowNextLine();
    }

    private void ShowNextLine()
    {
        if (currentSequence == null) return;
        if (currentIndex >= currentSequence.lines.Count)
        {
            EndDialogue(); // subito, senza attesa doppia
            return;
        }

        DialogueLine line = currentSequence.lines[currentIndex];

        bool showLeft = line.speakerName.Trim().ToUpper() == "PRISONER";

        leftDialogueBox.SetActive(showLeft);
        rightDialogueBox.SetActive(!showLeft);

        if (showLeft)
        {
            leftSpeakerText.text = line.speakerName;
            leftDialogueText.text = line.text;
        }
        else
        {
            rightSpeakerText.text = line.speakerName;
            rightDialogueText.text = line.text;
        }

        currentIndex++;
    }

    private void EndDialogue()
    {
        isDialogueActive = false;
        currentSequence = null;

        leftDialogueBox.SetActive(false);
        rightDialogueBox.SetActive(false);

        OnDialogueEnded?.Invoke();
        quickTimeUIManager.tutorialMode = false;
    }

    public bool IsDialogueActive()
    {
        return isDialogueActive;
    }

    public DialogueSequence GetFinalDialogue()
    {
        return finalDialogue;
    }
}