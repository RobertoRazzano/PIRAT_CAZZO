using UnityEngine;

public class CaptainDeathTrigger : MonoBehaviour
{
    [SerializeField] private PirateController captain;
    [SerializeField] private DialogueManagerPonte dialogueManager;

    void Start()
    {
        if (captain != null)
            captain.OnPirateDeath += HandleCaptainDeath;
    }

    private void HandleCaptainDeath(PirateController deadPirate)
    {
        Debug.Log("💀 Capitano è morto, avvio dialogo.");
        dialogueManager.StartDialogue(dialogueManager.GetFinalDialogue()); // o un metodo dedicato
    }

    private void OnDestroy()
    {
        if (captain != null)
            captain.OnPirateDeath -= HandleCaptainDeath;
    }
}
