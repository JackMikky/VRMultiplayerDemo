using UnityEngine;

using UnityEngine.AI;

public class AssassinLeavingState : IState
{
    private const float NavMeshSampleDistance = 2f;
    private const float ArrivalDistance = 0.5f;

    private readonly AssassinNPC npc;
    private bool hasDestination;

    public AssassinLeavingState(AssassinNPC npc)
    {
        this.npc = npc;
    }

    public void Enter()
    {
        hasDestination = false;

        npc.SetNavigationMode(useAgent: true);
        npc.ResetMovementAnimationFlags();

        if (npc.Agent == null || !npc.Agent.enabled || !npc.Agent.isOnNavMesh)
        {
            Debug.LogWarning($"[{npc.name}] Agent is not available for leaving the scene.", npc);
            Despawn();
            return;
        }

        Transform leavePoint = NPCManager.Instance != null ? NPCManager.Instance.NPCLeavePoint : null;
        if (leavePoint == null)
        {
            Debug.LogWarning($"[{npc.name}] No AssassinLeavePoint assigned on NPCManager; despawning immediately.", npc);
            Despawn();
            return;
        }

        if (NavMesh.SamplePosition(leavePoint.position, out NavMeshHit navHit, NavMeshSampleDistance, npc.Agent.areaMask)
            && npc.Agent.SetDestination(navHit.position))
        {
            hasDestination = true;
            npc.SetAgentVelocity(npc.walkSpeed, isStopped: false);
        }
        else
        {
            Debug.LogWarning($"[{npc.name}] Failed to set destination to leave point; despawning immediately.", npc);
            Despawn();
        }
    }

    public void Update()
    {
        if (!hasDestination)
        {
            return;
        }

        if (npc.Agent == null || !npc.Agent.enabled || !npc.Agent.isOnNavMesh)
        {
            return;
        }

        npc.SyncMovementAnimation();

        if (!npc.Agent.pathPending
            && npc.Agent.hasPath
            && npc.Agent.remainingDistance <= ArrivalDistance)
        {
            Despawn();
            return;
        }

        if (!npc.Agent.pathPending && npc.Agent.pathStatus == NavMeshPathStatus.PathInvalid)
        {
            Despawn();
        }
    }

    public void Exit()
    {
    }

    public void Dispose()
    {
    }

    private void Despawn()
    {
        if (npc.LeavingBehavior != null)
        {
            npc.LeavingBehavior.Despawn(npc);
            return;
        }

        if (NPCManager.Instance != null)
        {
            NPCManager.Instance.UnregisterAssassin(npc, wasCaptured: false);
        }

        Object.Destroy(npc.gameObject);
    }
}