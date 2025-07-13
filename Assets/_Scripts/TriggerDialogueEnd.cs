using UnityEngine;

public class TriggerDialogueEnd : MonoBehaviour
{
    [Header("Dialogue Settings")]
    public DialogueSequence dialogueToStart;
    public DialogueManagerFloor1 dialogueUI; // script che mostra il dialogo

    [Header("One-time trigger")]
    public bool triggerOnlyOnce = true;

    private bool hasTriggered = false;

    private void OnTriggerEnter(Collider other)
    {
        if (hasTriggered && triggerOnlyOnce) return;
        if (!other.CompareTag("Player")) return;

        hasTriggered = true;

        if (dialogueUI != null && dialogueToStart != null)
        {
            dialogueUI.StartDialogue(dialogueToStart);
        }
    }
}
