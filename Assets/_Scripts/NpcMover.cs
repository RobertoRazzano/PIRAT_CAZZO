using UnityEngine;
using UnityEngine.AI;

public class NpcMover : MonoBehaviour
{
    [SerializeField] private Transform destinazione;

    private NavMeshAgent agente;
    private Animator animatore;

    [SerializeField] private float distanzaStop = 0.2f;

    private void Awake()
    {
        agente = GetComponent<NavMeshAgent>();
        animatore = GetComponent<Animator>();
    }

    private void Update()
    {
        if (destinazione == null) return;

        agente.SetDestination(destinazione.position);

        bool staCamminando = agente.remainingDistance > distanzaStop && agente.pathPending == false;
        animatore.SetBool("isWalking", staCamminando);
    }
}
