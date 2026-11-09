using UnityEngine;

[CreateAssetMenu(fileName = "NewPatrolRandom", menuName = BehaviorConstants.MoveBehaviorMenuName + "PatrolRandom")]
public class PatrolRandomBehavior : ScriptableMoveBehavior
{
    [SerializeField] private float patrolSpeed = 2.5f;
    [SerializeField] private float stoppingDistance = 1.2f;

    public override void Enter(NPCBase npc)
    {
        if (npc is PoliceNPC police)
        {
            if (police.patrolWaypoints == null || police.patrolWaypoints.Length == 0) return;

            police.currentWaypointIndex = Random.Range(0, police.patrolWaypoints.Length);

            Transform currentTarget = police.patrolWaypoints[police.currentWaypointIndex];
            police.SetNavMeshTarget(currentTarget);
            police.SetNavigationMode(true);
            police.SetAgentVelocity(patrolSpeed, isStopped: false);
            police.Agent.stoppingDistance = stoppingDistance;

            if (police.Agent.enabled && police.Agent.isOnNavMesh)
                police.Agent.SetDestination(currentTarget.position);

            if (police.Anim != null)
            {
                police.Anim.SetBool(AnimationConstants.IsWalking, true);
                police.Anim.SetBool(AnimationConstants.IsIdling, false);
            }
        }
    }

    public override void UpdateBehavior(NPCBase npc)
    {
        if (npc is PoliceNPC police)
        {
            if (police.Agent == null || !police.Agent.enabled || !police.Agent.isOnNavMesh) return;

            if (!police.Agent.pathPending && police.Agent.hasPath && police.Agent.remainingDistance <= police.Agent.stoppingDistance)
            {
                Transform completedWaypoint = police.patrolWaypoints[police.currentWaypointIndex];

                if (completedWaypoint.TryGetComponent<PatrolWaypoint>(out var waypointData))
                    police.currentWaypointStayDuration = waypointData.stayDuration;
                else
                    police.currentWaypointStayDuration = Random.Range(police.minStayDuration, police.maxStayDuration);

                police.currentWaypointIndex = Random.Range(0, police.patrolWaypoints.Length);
            }
        }
    }

    public override void Exit(NPCBase npc)
    {
        if (npc.Agent != null && npc.Agent.enabled && npc.Agent.isOnNavMesh)
        {
            npc.Agent.ResetPath();
        }
    }
}