using UnityEngine;
using UnityEngine.AI;
using System;

public class TrailRatController : MonoBehaviour
{
    [Header("Impostazioni Trail")]
    public float speed = 5f;                // velocità di movimento
    public float stoppingDistance = 1f;   // distanza minima per considerare “arrivato”

    private NavMeshAgent agent;             // l’agente NavMesh
    private Transform target;               // il Transform del pirata da inseguire
    private bool isMoving = false;          // flag di movimento

    // Evento a cui iscriversi per sapere quando arrivo
    public event Action OnArrived;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        if (agent == null)
            Debug.LogError("NavMeshAgent mancante su TrailRatPrefab!");
    }

    void Start()
    {
        if (agent != null)
        {
            agent.speed = speed;
            agent.stoppingDistance = stoppingDistance;
        }
    }

    void Update()
    {
        if (isMoving && agent != null && target != null)
        {
            // Aggiorna destinazione se il target si muove
            agent.SetDestination(target.position);

            // Se ho finito il path e sono alla distanza di stoppingDistance
            if (!agent.pathPending && agent.remainingDistance <= stoppingDistance)
            {
                isMoving = false;
                OnArrived?.Invoke();
            }
        }
    }

    // Chiamare per far partire l’inseguimento
    public void MoveTo(Transform targetTransform)
    {
        target = targetTransform;
        isMoving = true;
        if (agent != null)
            agent.SetDestination(target.position);
    }

    // Facoltativo: per interrompere il movimento
    public void StopMovement()
    {
        if (agent != null)
            agent.ResetPath();
        isMoving = false;
        target = null;
    }
}
