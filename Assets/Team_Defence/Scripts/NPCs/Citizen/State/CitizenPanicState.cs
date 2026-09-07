public class CitizenPanicState : IState
{
    private CitizenNPC npc;

    public CitizenPanicState(CitizenNPC npc)
    {
        this.npc = npc;
    }

    public void Enter()
    {
        npc.PanicBehavior?.Enter(npc);
    }

    public void Update()
    {
        npc.PanicBehavior?.UpdateBehavior(npc);
    }

    public void Exit()
    {
        npc.PanicBehavior?.Exit(npc);
    }

    public void Dispose()
    {
    }
}