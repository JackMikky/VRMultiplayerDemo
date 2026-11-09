using UnityEngine;
using UnityEngine.AI;

public class AssassinWanderingState : IState
{
    private const int DestinationAttempts = 8;
    private const float DestinationRetryInterval = 0.5f;
    private const float ArrivalBuffer = 0.3f;
    private const float NavMeshSampleDistance = 2f;

    private readonly AssassinNPC npc;
    private float wanderEndTime;
    private float nextDestinationAttemptTime;
    private float nextStayCheckTime;
    private float remainingWanderDuration;
    private bool resumeAfterStaying;

    public AssassinWanderingState(AssassinNPC npc)
    {
        this.npc = npc;
    }

    public void Enter()
    {
        if (resumeAfterStaying)
        {
            wanderEndTime = Time.time + remainingWanderDuration;
            resumeAfterStaying = false;
        }
        else
        {
            float minDuration = Mathf.Max(0f, npc.MinWanderDuration);
            float maxDuration = Mathf.Max(minDuration, npc.MaxWanderDuration);
            wanderEndTime = Time.time + Random.Range(minDuration, maxDuration);
        }

        nextDestinationAttemptTime = Time.time;
        ScheduleNextStayCheck();

        npc.SetNavigationMode(useAgent: true);
        npc.ResetMovementAnimationFlags();
        npc.SetAgentVelocity(npc.walkSpeed, isStopped: false);
        TrySetRandomDestination();
    }

    public void Update()
    {
        if (Time.time >= wanderEndTime)
        {
            if (npc.Target == null) return;

            npc.TargetDistanceCheckAndTransition();

            return;
        }

        if (Time.time >= nextStayCheckTime)
        {
            ScheduleNextStayCheck();
            if (Random.value < npc.WanderPauseChance)
            {
                remainingWanderDuration = Mathf.Max(0f, wanderEndTime - Time.time);
                resumeAfterStaying = true;
                npc.ChangeToState(npc.StayingState, AssassinState.Staying);
                return;
            }
        }

        NavMeshAgent agent = npc.Agent;
        if (agent == null || !agent.enabled || !agent.isOnNavMesh)
        {
            return;
        }

        bool needsDestination = !agent.pathPending
            && (!agent.hasPath
                || agent.pathStatus != NavMeshPathStatus.PathComplete
                || agent.remainingDistance <= agent.stoppingDistance + ArrivalBuffer);

        if (needsDestination && Time.time >= nextDestinationAttemptTime)
        {
            TrySetRandomDestination();
        }

        npc.SyncMovementAnimation();
    }

    public void Exit()
    {
        NavMeshAgent agent = npc.Agent;
        if (agent != null && agent.enabled)
        {
            if (agent.isOnNavMesh)
            {
                agent.ResetPath();
            }

            agent.autoTraverseOffMeshLink = false;
        }
    }

    public void Dispose()
    {
    }

    public void ResumeAfterStaying()
    {
        npc.ChangeToState(this, AssassinState.Wandering);
    }

    private void ScheduleNextStayCheck()
    {
        float minInterval = Mathf.Max(0.1f, npc.MinWanderPauseInterval);
        float maxInterval = Mathf.Max(minInterval, npc.MaxWanderPauseInterval);
        nextStayCheckTime = Time.time + Random.Range(minInterval, maxInterval);
    }

    private void TrySetRandomDestination()
    {
        nextDestinationAttemptTime = Time.time + DestinationRetryInterval;

        NavMeshAgent agent = npc.Agent;
        if (agent == null || !agent.enabled || !agent.isOnNavMesh)
        {
            return;
        }

        float radius = Mathf.Max(0.1f, npc.WanderRadius);
        for (int attempt = 0; attempt < DestinationAttempts; attempt++)
        {
            Vector2 offset = Random.insideUnitCircle * radius;
            Vector3 candidate = npc.transform.position + new Vector3(offset.x, 0f, offset.y);

            if (!NavMesh.SamplePosition(candidate, out NavMeshHit hit, NavMeshSampleDistance, agent.areaMask))
            {
                continue;
            }

            NavMeshPath path = new NavMeshPath();
            if (!agent.CalculatePath(hit.position, path) || path.status != NavMeshPathStatus.PathComplete)
            {
                continue;
            }

            if (agent.SetDestination(hit.position))
            {
                return;
            }
        }
    }
}
