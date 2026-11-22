using UnityEngine;

public class CitizenPanicState : IState
{
    private readonly CitizenNPC npc;
    private float endTime;

    public CitizenPanicState(CitizenNPC npc)
    {
        this.npc = npc;
    }

    public void Enter()
    {
        endTime = Time.time + npc.panicDuration;

        npc.ResetMovementAnimationFlags();

        Animator animator = npc.Anim;
        if (animator != null)
        {
            animator.SetBool(AnimationConstants.Staying, false);
            int animationCount = Mathf.Max(1, npc.PanicAnimationCount);
            animator.SetInteger(AnimationConstants.PanicIndex, Random.Range(0, animationCount));
            animator.SetBool(AnimationConstants.IsPanic, true);
        }

        npc.PanicBehavior?.Enter(npc);
    }

    public void Update()
    {
        npc.PanicBehavior?.UpdateBehavior(npc);

        if (Time.time >= endTime)
        {
            npc.RecoverFromPanic();
        }
    }

    public void Exit()
    {
        npc.PanicBehavior?.Exit(npc);

        if (npc.Anim != null)
        {
            npc.Anim.SetBool(AnimationConstants.IsPanic, false);
        }
    }

    public void Dispose()
    {
    }
}
