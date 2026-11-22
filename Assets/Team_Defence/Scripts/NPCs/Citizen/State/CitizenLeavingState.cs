using UnityEngine;
using UnityEngine.AI;

public class CitizenLeavingState : IState
{
    private const float NavMeshSampleDistance = 2f;
    private const float ArrivalDistance = 0.5f;

    private readonly CitizenNPC npc;
    private bool hasDestination;

    public CitizenLeavingState(CitizenNPC npc)
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
            Debug.LogWarning($"[{npc.name}] No exit point assigned; despawning immediately.", npc);
            Despawn();
            return;
        }

        if (NavMesh.SamplePosition(leavePoint.position, out NavMeshHit navHit, NavMeshSampleDistance, npc.Agent.areaMask)
            && npc.Agent.SetDestination(navHit.position))
        {
            hasDestination = true;
            npc.SetAgentVelocity(npc.Agent.speed, isStopped: false);
            if (npc.Anim != null) npc.Anim.SetBool(AnimationConstants.IsWalking, true);
        }
        else
        {
            Debug.LogWarning($"[{npc.name}] Failed to set destination to exit point; despawning immediately.", npc);
            Despawn();
        }
    }

    public void Update()
    {
        if (!hasDestination) return;
        if (npc.Agent == null || !npc.Agent.enabled || !npc.Agent.isOnNavMesh) return;
        if (npc.Agent.pathPending) return;

        if (npc.Agent.pathStatus == NavMeshPathStatus.PathInvalid
            || (npc.Agent.hasPath && npc.Agent.remainingDistance <= ArrivalDistance))
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
        hasDestination = false;

        if (NPCManager.Instance != null)
        {
            NPCManager.Instance.UnregisterCitizen(npc);
        }

        Object.Destroy(npc.gameObject);
    }
}
