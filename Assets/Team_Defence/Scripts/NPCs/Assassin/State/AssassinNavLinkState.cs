public class AssassinNavLinkState : IState
{
    private readonly AssassinNPC npc;

    public AssassinNavLinkState(AssassinNPC npc)
    {
        this.npc = npc;
    }

    public void Enter()
    {
        npc.NavLinkingBehavior?.Enter(npc);
    }

    public void Update()
    {
        npc.NavLinkingBehavior?.UpdateBehavior(npc);
    }

    public void Exit()
    {
        npc.NavLinkingBehavior?.Exit(npc);
    }

    public void Dispose()
    { }
}