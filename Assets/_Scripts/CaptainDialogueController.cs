using UnityEngine;
using UnityEngine.AI;

public class CaptainDialogueController : MonoBehaviour
{
    [Header("Dipendenze")]
    [SerializeField] private DialogueManager dialogueManager;
    [SerializeField] private Transform exitPoint;
    [SerializeField] private PrisonerDialogueTrigger prisonerTrigger;
    [Header("Audio")]
    [SerializeField] private AudioClip footstepClip;

    private NavMeshAgent agent;
    private Animator animator;
    private bool hasWalkedAway = false;
    private bool hasTriggeredPrisonerDialogue = false;
    private AudioSource audioSource;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
        audioSource = GetComponent<AudioSource>();
        agent.isStopped = true;
        SetWalking(false);

        dialogueManager.OnDialogueEnded += HandleDialogueEnded;
    }

    void OnDestroy()
    {
        dialogueManager.OnDialogueEnded -= HandleDialogueEnded;
    }

    void HandleDialogueEnded()
    {
        if (!hasWalkedAway)
        {
            WalkAway();
        }
    }

    void Update()
    {
        if (hasWalkedAway && !hasTriggeredPrisonerDialogue)
        {
            if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
            {
                SetWalking(false);
                hasTriggeredPrisonerDialogue = true;

                if (prisonerTrigger != null)
                {
                    prisonerTrigger.TriggerPrisonerDialogue();
                }
            }
        }
    }

    void WalkAway()
    {
        hasWalkedAway = true;
        agent.isStopped = false;
        SetWalking(true);
        if (exitPoint != null)
            agent.SetDestination(exitPoint.position);
    }

    void SetWalking(bool isWalking)
    {
        if (animator != null)
        {
            animator.SetBool("isWalking", isWalking);
        }

        if (audioSource != null && footstepClip != null)
        {
            if (isWalking)
            {
                audioSource.clip = footstepClip;
                audioSource.loop = true;
                audioSource.Play();
            }
            else
            {
                audioSource.Stop();
            }
        }
    }
}