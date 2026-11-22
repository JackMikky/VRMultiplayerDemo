public class CitizenStayingState : IState
{
    private readonly CitizenNPC npc;
    private readonly StayingAnimationPlayer player;

    public CitizenStayingState(CitizenNPC npc)
    {
        this.npc = npc;
        player = new StayingAnimationPlayer(npc);
    }

    public void Enter()
    {
        npc.SetNavigationMode(useAgent: false);
        npc.ResetMovementAnimationFlags();
        player.Begin(npc.StayingAnimationCount);
    }

    public void Update()
    {
        if (player.Tick())
        {
            player.End();
            npc.ChangeToState(npc.WalkingState, CitizenState.Wandering);
        }
    }

    public void Exit()
    {
        player.End();
    }

    public void Dispose()
    { }
}
