using UnityEngine;

public class EmpowermentTrigger : MonoBehaviour
{
    [SerializeField] private DialogueManager dialogueManager;
    [SerializeField] private DialogueSequence empowermentDialogue;
    
    private bool hasTriggered = false;

    private void OnTriggerEnter(Collider other)
    {
        if (this.hasTriggered) return;

        if (other.CompareTag("Player") && RatInteractionManager.HasCompletedFirstQuickTime)
        {
            hasTriggered = true;
            dialogueManager.StartDialogue(empowermentDialogue);
        }
    }
}