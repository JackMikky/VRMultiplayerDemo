public class PoliceStayingState : BaseTimedState<PoliceNPC>
{
    public PoliceStayingState(PoliceNPC npc, float duration) : base(npc, duration)
    {
    }

    public override void Enter()
    {
        this.duration = npc.currentWaypointStayDuration;

        base.Enter();

        npc.IdleBehavior?.Enter(npc);
    }

    public override void Update()
    {
        base.Update();
        npc.IdleBehavior?.UpdateBehavior(npc);
    }

    public override void Exit()
    {
        base.Exit();
        npc.IdleBehavior?.Exit(npc);
    }

    protected override void OnTimeOut()
    {
        npc.ChangeToState(npc.PatrolingState, PoliceState.Patrolling);
    }
}