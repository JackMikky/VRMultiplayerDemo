public class CitizenWatchingState : IState
{
    private CitizenNPC npc;

    public CitizenWatchingState(CitizenNPC npc)
    {
        this.npc = npc;
    }

    public void Enter()
    {
        npc.IdleBehavior?.Enter(npc);
    }

    public void Update()
    {
        npc.IdleBehavior?.UpdateBehavior(npc);
    }

    public void Exit()
    {
        npc.IdleBehavior?.Exit(npc);
    }

    public void Dispose()
    { }
}