public class AssassinInteractedState : IState
{
    private AssassinNPC npc;

    public AssassinInteractedState(AssassinNPC npc)
    {
        this.npc = npc;
    }

    public void Dispose()
    {
        // no-op
    }

    public void Enter()
    {
        // stop everything and show mark
        npc.StopAllCoroutines();
        npc.SetAgentVelocity(0f, isStopped: true);
        npc.ShowMark(true);

        npc.ResetMovementAnimationFlags();
        if (npc.Anim != null) npc.Anim.SetBool(AnimationConstants.IsIdling, true);
        npc.InteractedBehavior?.Enter(npc);
    }

    public void Exit()
    {
        // no-op
    }

    public void Update()
    {
        // idle
    }
}