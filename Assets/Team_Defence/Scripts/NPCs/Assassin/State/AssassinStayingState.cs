using UnityEngine;

public class AssassinStayingState : IState
{
    private readonly AssassinNPC npc;
    private int previousAnimatorStateHash;
    private bool animationStarted;
    private bool transitionRequested;

    private const float NoticeDistance = 10f;
    private const float NoticeHalfAngle = 40f;

    public AssassinStayingState(AssassinNPC npc)
    {
        this.npc = npc;
    }

    public void Enter()
    {
        if (npc.Agent != null) npc.Agent.isStopped = true;

        npc.ResetMovementAnimationFlags();
        animationStarted = false;
        transitionRequested = false;
        if (npc.Anim != null)
        {
            previousAnimatorStateHash = npc.Anim.GetCurrentAnimatorStateInfo(0).fullPathHash;
            int animationCount = Mathf.Max(1, npc.StayingAnimationCount);
            npc.Anim.SetInteger(AnimationConstants.StayingIndex, Random.Range(0, animationCount));
            npc.Anim.SetBool(AnimationConstants.Staying, true);
        }
    }

    public void Update()
    {
        Animator animator = npc.Anim;
        if (animator == null)
        {
            npc.WanderingState.ResumeAfterStaying();
            return;
        }

        if (transitionRequested)
        {
            if (!animator.IsInTransition(0)
                && !animator.GetCurrentAnimatorStateInfo(0).IsTag("Staying"))
            {
                npc.WanderingState.ResumeAfterStaying();
                return;
            }
        }
        else
        {
            AnimatorStateInfo currentState = animator.GetCurrentAnimatorStateInfo(0);
            if (!animationStarted && animator.IsInTransition(0))
            {
                animationStarted = animator.GetNextAnimatorStateInfo(0).IsTag("Staying");
            }
            else if (!animationStarted
                && currentState.IsTag("Staying")
                && currentState.fullPathHash != previousAnimatorStateHash)
            {
                animationStarted = true;
            }

            if (animationStarted
                && currentState.IsTag("Staying")
                && currentState.normalizedTime >= 1f)
            {
                RequestTransition();
            }
        }

        if (npc.Target != null)
        {
            Vector3 direction = npc.Target.position - npc.transform.position;
            direction.y = 0f;

            if (direction.sqrMagnitude <= NoticeDistance * NoticeDistance
                && Vector3.Angle(npc.transform.forward, direction) <= NoticeHalfAngle)
            {
                npc.LookAtTarget(npc.Target);
            }
        }
    }

    public void Exit()
    {
        if (npc.Agent != null) npc.Agent.isStopped = false;
        if (npc.Anim != null)
        {
            npc.Anim.SetBool(AnimationConstants.Staying, false);
        }
    }

    public void Dispose()
    {
    }

    private void RequestTransition()
    {
        transitionRequested = true;
        npc.Anim.SetBool(AnimationConstants.IsIdling, true);
    }
}