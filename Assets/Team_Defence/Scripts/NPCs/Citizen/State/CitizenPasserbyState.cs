using UnityEngine;
using UnityEngine.AI;

public class CitizenPasserbyState : IState
{
    private enum Phase
    {
        Standing,
        Moving
    }

    private const float ChanceToLookAtVipWhilePausing = 0.5f;
    private const int StandingAvoidancePriority = 30;
    private const int MovingAvoidancePriority = 50;

    private readonly CitizenNPC npc;
    private readonly StayingAnimationPlayer player;
    private Phase phase;
    private float phaseEndTime;
    private float visitEndTime = -1f;
    private float nextReactionTime;
    private bool isReacting;
    private bool lookAtVipWhilePausing;

    public CitizenPasserbyState(CitizenNPC npc)
    {
        this.npc = npc;
        player = new StayingAnimationPlayer(npc);
    }

    public void Enter()
    {
        npc.SetNavigationMode(useAgent: true);

        if (visitEndTime < 0f)
        {
            visitEndTime = Time.time + Random.Range(npc.minVisitDuration, npc.maxVisitDuration);
        }

        BeginStanding(npc.minWatchDuration, npc.maxWatchDuration, lookAtVip: true);
    }

    public void Update()
    {
        if (Time.time >= visitEndTime)
        {
            if (isReacting)
            {
                if (lookAtVipWhilePausing) LookAtVip();
                UpdateReaction();
                if (isReacting) return;
            }

            npc.ChangeToState(npc.LeavingState, CitizenState.Leaving);
            return;
        }

        switch (phase)
        {
            case Phase.Standing:
                if (lookAtVipWhilePausing) LookAtVip();
                UpdateReaction();
                if (!isReacting && Time.time >= phaseEndTime && !BeginMoving())
                {
                    BeginStanding(npc.minPauseDuration, npc.maxPauseDuration, Random.value < ChanceToLookAtVipWhilePausing);
                }
                break;

            case Phase.Moving:
                UpdateMoving();
                break;
        }
    }

    public void Exit()
    {
        EndReaction();
        SetAvoidancePriority(MovingAvoidancePriority);
    }

    public void Dispose()
    {
    }

    private void BeginStanding(float minDuration, float maxDuration, bool lookAtVip)
    {
        EndReaction();
        phase = Phase.Standing;
        phaseEndTime = Time.time + Random.Range(minDuration, maxDuration);
        lookAtVipWhilePausing = lookAtVip;
        StopMoving();
        ScheduleReaction();
    }

    private bool BeginMoving()
    {
        VenueManager venue = VenueManager.Instance;
        if (venue == null || npc.Agent == null || !npc.Agent.enabled || !npc.Agent.isOnNavMesh) return false;
        if (!venue.TryGetRandomRoamingPoint(out Vector3 destination)) return false;

        EndReaction();
        npc.SetAgentVelocity(npc.Agent.speed, isStopped: false);
        if (!npc.Agent.SetDestination(destination)) return false;

        SetAvoidancePriority(MovingAvoidancePriority);
        phase = Phase.Moving;
        npc.ResetMovementAnimationFlags();
        if (npc.Anim != null) npc.Anim.SetBool(AnimationConstants.IsWalking, true);
        return true;
    }

    private void UpdateMoving()
    {
        if (npc.Agent == null || !npc.Agent.enabled || !npc.Agent.isOnNavMesh)
        {
            BeginStanding(npc.minWatchDuration, npc.maxWatchDuration, lookAtVip: true);
            return;
        }

        if (npc.Agent.pathPending) return;

        if (npc.Agent.pathStatus == NavMeshPathStatus.PathInvalid)
        {
            BeginStanding(npc.minWatchDuration, npc.maxWatchDuration, lookAtVip: true);
        }
        else if (npc.Agent.remainingDistance <= npc.Agent.stoppingDistance)
        {
            BeginStanding(npc.minPauseDuration, npc.maxPauseDuration, Random.value < ChanceToLookAtVipWhilePausing);
        }
    }

    private void StopMoving()
    {
        if (npc.Agent != null && npc.Agent.enabled && npc.Agent.isOnNavMesh)
        {
            npc.Agent.isStopped = true;
        }

        SetAvoidancePriority(StandingAvoidancePriority);
        npc.ResetMovementAnimationFlags();
        if (npc.Anim != null) npc.Anim.SetBool(AnimationConstants.IsIdling, true);
    }

    private void SetAvoidancePriority(int priority)
    {
        if (npc.Agent != null) npc.Agent.avoidancePriority = priority;
    }

    private void LookAtVip()
    {
        if (npc.LookTarget != null) npc.LookAtTarget(npc.LookTarget);
    }

    private void ScheduleReaction()
    {
        nextReactionTime = Time.time + Random.Range(npc.minReactionInterval, npc.maxReactionInterval);
    }

    private void UpdateReaction()
    {
        if (npc.Anim == null) return;

        if (isReacting)
        {
            if (player.Tick())
            {
                player.End();
                isReacting = false;
                nextReactionTime = Random.value < npc.animationRepeatChance
                    ? Time.time + Random.Range(npc.minReactionInterval, npc.maxReactionInterval)
                    : float.MaxValue;
            }
        }
        else if (Time.time >= nextReactionTime && Time.time < phaseEndTime)
        {
            isReacting = true;
            player.Begin(npc.StayingAnimationCount);
        }
    }

    private void EndReaction()
    {
        if (!isReacting) return;

        isReacting = false;
        player.End();
    }
}
