using UnityEngine;

public class CitizenStayingState : BaseTimedState<CitizenNPC>
{
    public CitizenStayingState(CitizenNPC npc, float duration) : base(npc, duration)
    {
    }

    public override void Enter()
    {
        this.duration = Random.Range(npc.minStayDuration, npc.maxStayDuration);
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
        npc.ChangeToState(npc.WalkingState, CitizenState.Wandering);
    }
}