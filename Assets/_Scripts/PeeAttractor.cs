using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;

public class PeeAttractor : MonoBehaviour
{
    [Header("Attrazione pirati")]
    

    [HideInInspector] public bool spawnTrapOnFirstInfection = false;
    [HideInInspector] public GameObject[] possibleTraps;

    [SerializeField] private SphereCollider attractionCollider;

    private HashSet<PirateController> attractedPirates = new HashSet<PirateController>();
    private PirateController firstToReach = null;
    private bool trapSpawned = false;

    private void Start()
    {
        if (attractionCollider == null)
        {
            Debug.LogError("❌ SphereCollider non assegnato in PeeAttractor!");
            return;
        }

        if (!attractionCollider.isTrigger)
        {
            Debug.LogWarning("⚠️ SphereCollider non è trigger, lo forzo via script");
            attractionCollider.isTrigger = true;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Pirate")) return;

        PirateController pirate = other.GetComponentInParent<PirateController>();
        if (pirate != null
            && !pirate.infected
            && !attractedPirates.Contains(pirate)
            && (pirate.CurrentState == "Patrol" || pirate.CurrentState == "Suspicious"))

        {
            attractedPirates.Add(pirate);
            NavMeshAgent agent = pirate.GetComponent<NavMeshAgent>();
            if (agent != null)
            {
                agent.SetDestination(transform.position);
                Debug.Log($"🧲 Pirate {pirate.name} attratto dalla pozza");
            }
        }
    }

    public bool WasLegitForTrap(PirateController pirate)
    {
        return pirate != null
            && (pirate.CurrentState == "Patrol" || pirate.CurrentState == "Suspicious")
            && spawnTrapOnFirstInfection
            && possibleTraps != null
            && possibleTraps.Length > 0;
    }


    public void OnPuddleConsumed(PirateController consumer)
    {
        Debug.Log($"🔵 OnPuddleConsumed chiamato da: {consumer.name}");

        if (firstToReach != null && firstToReach != consumer)
        {
            Debug.Log("⛔ Trappola già gestita da un altro pirata");
            return;
        }

        firstToReach = consumer;

        Debug.Log($"👉 Stato pirata: {consumer.CurrentState}");
        Debug.Log($"👉 spawnTrapOnFirstInfection: {spawnTrapOnFirstInfection}");
        Debug.Log($"👉 trapSpawned: {trapSpawned}");
        Debug.Log($"👉 possibleTraps.Length: {possibleTraps?.Length}");

        if (!spawnTrapOnFirstInfection)
        {
            Debug.Log("⛔ spawnTrapOnFirstInfection è disattivo");
            return;
        }

        if (trapSpawned)
        {
            Debug.Log("⛔ Una trappola è già stata instanziata");
            return;
        }

        if (possibleTraps == null || possibleTraps.Length == 0)
        {
            Debug.Log("⛔ Nessuna trappola assegnata");
            return;
        }

        if (consumer.CurrentState != "Patrol" && consumer.CurrentState != "Suspicious")
        {
            Debug.Log("⛔ Il pirata non è in uno stato valido per piazzare trappole");
            return;
        }

        int index = Random.Range(0, possibleTraps.Length);
        GameObject trap = Instantiate(possibleTraps[index], transform.position, Quaternion.identity);
        trapSpawned = true;

        Debug.Log($"✅ Trappola instanziata: {trap.name}");
    }


    public void SetTrapMechanic(bool enable, GameObject[] traps)
    {
        spawnTrapOnFirstInfection = enable;
        possibleTraps = traps;
    }
    // Questo riceve il SendMessage da PirateController
    private void CancelAttractionFromPuddle(PirateController pirate)
    {
        CancelAttraction(pirate);
    }

    public void CancelAttraction(PirateController pirate)
    {
        if (attractedPirates.Contains(pirate))
        {
            attractedPirates.Remove(pirate);
            Debug.Log($"❌ Pirate {pirate.name} ha abbandonato l'attrazione verso la pipì");
        }
    }

    public bool IsAttracted(PirateController pirate)
    {
        return attractedPirates.Contains(pirate);
    }


}

