using UnityEngine;
using UnityEngine.AI;

public class PirateAutoMove : MonoBehaviour
{
    [SerializeField] private Transform target;

    private NavMeshAgent agent;
    private Animator animator;

    private bool isMoving = false;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
    }

    public void MoveToTarget()
    {
        if (target != null && agent != null)
        {
            agent.SetDestination(target.position);
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