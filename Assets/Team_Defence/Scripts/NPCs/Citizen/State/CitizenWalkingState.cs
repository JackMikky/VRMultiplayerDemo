public class CitizenWalkingState : IState
{
    private CitizenNPC npc;

    public CitizenWalkingState(CitizenNPC npc)
    {
        this.npc = npc;
    }

    public void Enter()
    {
        if (npc.IsMovingToGatheringPoint)
            npc.StartGatheringMovement();
        else
            npc.MoveBehavior?.Enter(npc);
    }

    public void Update()
    {
        if (!npc.IsMovingToGatheringPoint)
            npc.MoveBehavior?.UpdateBehavior(npc);

        if (npc.Agent != null && npc.Agent.enabled && !npc.Agent.pathPending)
        {
            if (npc.Agent.remainingDistance <= npc.Agent.stoppingDistance)
            {
                if (npc.IsMovingToGatheringPoint)
                    npc.CompleteAudienceGathering();
                else
                    npc.ChangeToState(npc.StayingState, CitizenState.Staying);
            }
        }
    }

    public void Exit()
    {
        if (!npc.IsMovingToGatheringPoint)
            npc.MoveBehavior?.Exit(npc);
    }

    public void Dispose()
    { }
}