using UnityEngine;

public class PrisonerDialogueTrigger : MonoBehaviour
{
    [Header("Riferimenti")]
    [SerializeField] private DialogueManager dialogueManager;
    [SerializeField] private DialogueSequence prisonerDialogue;

    private bool hasStarted = false;

    public void TriggerPrisonerDialogue()
    {
        if (!hasStarted)
        {
            hasStarted = true;
            dialogueManager.ForceRightBoxOnly(true);
            dialogueManager.StartDialogue(prisonerDialogue);
        }
    }

}