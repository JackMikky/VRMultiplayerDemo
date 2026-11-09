public class PoliceKeepingWatchState : IState
{
    private PoliceNPC npc;

    public PoliceKeepingWatchState(PoliceNPC npc)
    {
        this.npc = npc;
    }

    public void Enter()
    {
        npc.KeepingWatchBehavior?.Enter(npc);
    }

    public void Update()
    {
        npc.KeepingWatchBehavior?.UpdateBehavior(npc);
    }

    public void Exit()
    {
        npc.KeepingWatchBehavior?.Exit(npc);
    }

    public void Dispose()
    { }
}