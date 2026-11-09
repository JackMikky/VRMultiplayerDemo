public class AssassinAttackState : IState
{
    private AssassinNPC npc;

    public AssassinAttackState(AssassinNPC npc)
    {
        this.npc = npc;
    }

    public void Enter()
    {
        npc.AttackBehavior?.Enter(npc);
    }

    public void Exit()
    {
        npc.AttackBehavior?.Exit(npc);
    }

    public void Update()
    {
        npc.AttackBehavior?.UpdateBehavior(npc);
    }

    public void Dispose()
    {
    }
}