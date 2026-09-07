using UnityEngine;

[CreateAssetMenu(fileName = "NewPatrolPingPong", menuName = BehaviorConstants.MoveBehaviorMenuName + "PatrolPingPong")]
public class PatrolPingPongBehavior : ScriptableMoveBehavior
{
    [SerializeField] private float patrolSpeed = 2.5f;
    [SerializeField] private float stoppingDistance = 1.2f;

    public override void Enter(NPCBase npc)
    {
        if (npc is PoliceNPC police)
        {
            if (police.patrolWaypoints == null || police.patrolWaypoints.Length == 0)
            {
                Debug.LogWarning($"[PatrolPingPong] {police.name} No patrol waypoints configured.");
                return;
            }

            int actualIndex = GetPingPongIndex(police.currentWaypointIndex, police.patrolWaypoints.Length);
            Transform currentTarget = police.patrolWaypoints[actualIndex];

            police.SetNavMeshTarget(currentTarget);
            police.SetNavigationMode(true);
            police.SetAgentVelocity(patrolSpeed, isStopped: false);
            police.Agent.stoppingDistance = stoppingDistance;

            if (police.Agent.enabled && police.Agent.isOnNavMesh)
            {
                police.Agent.SetDestination(currentTarget.position);
            }

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
            if (police.Agent == null || police.Target == null || !police.Agent.enabled || !police.Agent.isOnNavMesh)
                return;

            police.Agent.SetDestination(police.Target.position);

            if (!police.Agent.pathPending && police.Agent.remainingDistance <= police.Agent.stoppingDistance)
            {
                int length = police.patrolWaypoints.Length;

                int actualIndex = GetPingPongIndex(police.currentWaypointIndex, length);
                Transform completedWaypoint = police.patrolWaypoints[actualIndex];

                if (completedWaypoint.TryGetComponent<PatrolWaypoint>(out var waypointData))
                {
                    police.currentWaypointStayDuration = waypointData.stayDuration;
                }
                else
                {
                    police.currentWaypointStayDuration = Random.Range(police.minStayDuration, police.maxStayDuration);
                }

                int cycleLength = (length * 2) - 2;

                if (cycleLength > 0)
                {
                    police.currentWaypointIndex = (police.currentWaypointIndex + 1) % cycleLength;
                }
                else
                {
                    police.currentWaypointIndex = 0;
                }
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

    /// <summary>
    /// Ping-pong mapping core algorithm: converting an infinitely incrementing virtual counter into a bouncing array index.
    /// </summary>
    private int GetPingPongIndex(int virtualIndex, int waypointCount)
    {
        if (waypointCount <= 1) return 0;

        int cycle = (waypointCount * 2) - 2;
        int absIndex = virtualIndex % cycle;

        if (absIndex >= waypointCount)
        {
            return cycle - absIndex;
        }

        return absIndex;
    }
}