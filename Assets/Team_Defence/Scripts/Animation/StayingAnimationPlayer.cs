using UnityEngine;

/// <summary>
/// Plays one random "Staying" animation selected by StayingIndex and reports when it has finished and returned to Idle.
/// States on the Staying sub-state machine must use the "Staying" tag.
/// </summary>
public class StayingAnimationPlayer
{
    private const string StayingTag = "Staying";
    private const float StartTimeout = 3f;

    private readonly NPCBase npc;
    private int previousAnimatorStateHash;
    private bool animationStarted;
    private bool transitionRequested;
    private float beginTime;

    public StayingAnimationPlayer(NPCBase npc)
    {
        this.npc = npc;
    }

    public void Begin(int animationCount)
    {
        animationStarted = false;
        transitionRequested = false;
        beginTime = Time.time;

        Animator animator = npc.Anim;
        if (animator == null) return;

        previousAnimatorStateHash = animator.GetCurrentAnimatorStateInfo(0).fullPathHash;
        animator.SetBool(AnimationConstants.IsIdling, false);
        animator.SetInteger(AnimationConstants.StayingIndex, Random.Range(0, Mathf.Max(1, animationCount)));
        animator.SetBool(AnimationConstants.Staying, true);
    }

    /// <summary>Returns true once the animation has finished and the animator has left the Staying states.</summary>
    public bool Tick()
    {
        Animator animator = npc.Anim;
        if (animator == null) return true;

        AnimatorStateInfo currentState = animator.GetCurrentAnimatorStateInfo(0);

        if (transitionRequested)
        {
            return !animator.IsInTransition(0) && !currentState.IsTag(StayingTag);
        }

        if (!animationStarted && animator.IsInTransition(0))
        {
            animationStarted = animator.GetNextAnimatorStateInfo(0).IsTag(StayingTag);
        }
        else if (!animationStarted
            && currentState.IsTag(StayingTag)
            && currentState.fullPathHash != previousAnimatorStateHash)
        {
            animationStarted = true;
        }

        if (!animationStarted && Time.time - beginTime > StartTimeout)
        {
            animator.SetBool(AnimationConstants.IsIdling, true);
            return true;
        }

        if (animationStarted
            && currentState.IsTag(StayingTag)
            && currentState.normalizedTime >= 1f)
        {
            transitionRequested = true;
            animator.SetBool(AnimationConstants.IsIdling, true);
        }

        return false;
    }

    /// <summary>Clears the Staying flag. Call when leaving the state or after the animation has finished.</summary>
    public void End()
    {
        if (npc.Anim != null) npc.Anim.SetBool(AnimationConstants.Staying, false);
    }
}
