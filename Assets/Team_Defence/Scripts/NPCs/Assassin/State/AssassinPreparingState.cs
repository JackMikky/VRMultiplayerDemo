public class AssassinPreparingState : IState
{
    private readonly AssassinNPC npc;

    public AssassinPreparingState(AssassinNPC npc)
    {
        this.npc = npc;
    }

    public void Enter()
    {
        npc.PreparationBehavior?.Enter(npc);
    }

    public void Update()
    {
        npc.PreparationBehavior?.UpdateBehavior(npc);
    }

    public void Exit()
    {
        npc.PreparationBehavior?.Exit(npc);
    }

    public void Dispose()
    { }
}