using UnityEngine;
using UnityEngine.Playables;

public class CaptainDeathTrigger : MonoBehaviour
{
    [Header("Riferimenti")]
    [SerializeField] private PirateController captain;
    [SerializeField] private DialogueManagerPonte dialogueManager;
    [SerializeField] private PirateAutoMove pirateAutoMove;
    [SerializeField] private PlayableDirector director;
    private float delay = 0.5f;

    private bool hasTriggered = false;

    void Start()
    {
        if (captain != null)
        {
            // Iscrizione all'evento che scatta quando l'animazione di morte finisce
            captain.OnDeathAnimationEndEvent += HandleDeathAnimationEnd;
        }
    }

    private void HandleDeathAnimationEnd()
    {
        if (hasTriggered) return;
        hasTriggered = true;

        Debug.Log("💀 Animazione di morte del Capitano completata → Avvio dialogo");

        if (dialogueManager != null)
        {
            dialogueManager.StartDialogue(dialogueManager.GetFinalDialogue());
        }
        else
        {
            Debug.LogWarning("❗ DialogueManager non assegnato in CaptainDeathTrigger");
        }
        pirateAutoMove?.MoveToTarget();
        Invoke(nameof(PlayTimeline), delay);
    }

    private void PlayTimeline()
    {
        director.Play();
    }

    private void OnDestroy()
    {
        if (captain != null)
        {
            captain.OnDeathAnimationEndEvent -= HandleDeathAnimationEnd;
        }
    }
}

