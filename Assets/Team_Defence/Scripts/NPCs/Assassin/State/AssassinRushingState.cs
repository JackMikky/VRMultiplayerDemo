public class AssassinRushingState : IState
{
    private AssassinNPC npc;

    public AssassinRushingState(AssassinNPC npc)
    {
        this.npc = npc;
    }

    public void Enter()
    {
        npc.RushingBehavior?.Enter(npc);
    }

    public void Update()
    {
        npc.RushingBehavior?.UpdateBehavior(npc);
    }

    public void Exit()
    {
        npc.RushingBehavior?.Exit(npc);
    }

    public void Dispose()
    {
        // no-op
    }
}