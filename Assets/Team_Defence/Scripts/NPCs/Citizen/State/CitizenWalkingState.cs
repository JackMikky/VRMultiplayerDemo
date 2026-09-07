public class CitizenWalkingState : IState
{
    private CitizenNPC npc;

    public CitizenWalkingState(CitizenNPC npc)
    {
        this.npc = npc;
    }

    public void Enter()
    {
        npc.MoveBehavior?.Enter(npc);
    }

    public void Update()
    {
        npc.MoveBehavior?.UpdateBehavior(npc);

        if (npc.Agent != null && npc.Agent.enabled && !npc.Agent.pathPending)
        {
            if (npc.Agent.remainingDistance <= npc.Agent.stoppingDistance)
            {
                npc.ChangeToState(npc.StayingState, CitizenState.Staying);
            }
        }
    }

    public void Exit()
    {
        npc.MoveBehavior?.Exit(npc);
    }

    public void Dispose()
    { }
}