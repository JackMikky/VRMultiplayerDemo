using UnityEngine;

public class AssassinStayingState : BaseTimedState<AssassinNPC>
{
    public AssassinStayingState(AssassinNPC npc, float duration) : base(npc, duration)
    {
    }

    public override void Enter()
    {
        base.Enter();

        if (npc.Agent != null) npc.Agent.isStopped = true;

        npc.ResetMovementAnimationFlags();
        if (npc.Anim != null)
            npc.Anim.SetBool(AnimationConstants.IsIdling, true);
    }

    public override void Update()
    {
        base.Update();

        if (npc.Target != null)
        {
            Vector3 lookTarget = new Vector3(npc.Target.position.x, npc.transform.position.y, npc.Target.position.z);
            npc.transform.LookAt(lookTarget);
        }
    }

    protected override void OnTimeOut()
    {
        npc.TargetDistanceCheckAndTransition();
    }
}