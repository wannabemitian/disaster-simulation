using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class NPC_RandomWander : MonoBehaviour
{
    public float wanderRadius = 15f; // How far they can pick a new point
    private NavMeshAgent agent;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        SetNewRandomDestination();
    }

    void Update()
    {
        // If the agent reaches the destination (or gets stuck), pick a new point
        if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
        {
            SetNewRandomDestination();
        }
    }

    void SetNewRandomDestination()
    {
        // Generate a random point in a sphere around the NPC
        Vector3 randomDirection = Random.insideUnitSphere * wanderRadius;
        randomDirection += transform.position;
        
        NavMeshHit hit;
        // Snap that random point to the nearest valid NavMesh floor
        if (NavMesh.SamplePosition(randomDirection, out hit, wanderRadius, 1))
        {
            agent.SetDestination(hit.position);
        }
    }
}
