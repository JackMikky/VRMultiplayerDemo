public class PolicePatrolingState : IState
{
    private PoliceNPC npc;

    public PolicePatrolingState(PoliceNPC npc)
    {
        this.npc = npc;
    }

    public void Enter()
    {
        npc.PatrolingBehavior?.Enter(npc);
    }

    public void Update()
    {
        npc.PatrolingBehavior?.UpdateBehavior(npc);

        if (npc.Agent != null && npc.Agent.enabled && !npc.Agent.pathPending)
        {
            if (npc.Agent.remainingDistance <= npc.Agent.stoppingDistance)
            {
                npc.ChangeToState(npc.StayingState, PoliceState.Patrolling);
            }
        }
    }

    public void Exit()
    {
        npc.PatrolingBehavior?.Exit(npc);
    }

    public void Dispose()
    { }
}