using UnityEngine;
using UnityEngine.AI;

public class PirateFinalMove : MonoBehaviour
{
    [SerializeField] private Transform finalTarget;

    private NavMeshAgent agent;
    private Animator animator;
    private bool isMoving = false;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
    }

    public void MoveToFinalTarget()
    {
        if (finalTarget != null && agent != null)
        {
            agent.SetDestination(finalTarget.position);
            agent.isStopped = false;
            isMoving = true;

            if (animator != null)
                animator.SetBool("isWalking", true);
        }
    }

    void Update()
    {
        if (isMoving && agent.remainingDistance <= agent.stoppingDistance && !agent.pathPending)
        {
            isMoving = false;
            agent.isStopped = true;

            if (animator != null)
                animator.SetBool("isWalking", false);
        }
    }
}