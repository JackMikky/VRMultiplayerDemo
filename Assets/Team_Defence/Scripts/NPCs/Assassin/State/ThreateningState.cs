using System.Collections;
using UnityEngine;

public class ThreateningState : IState
{
    private AssassinNPC npc;
    private Coroutine waitCoroutine;

    public ThreateningState(AssassinNPC npc)
    {
        this.npc = npc;
    }

    public void Enter()
    {
        // stop movement and play threaten animation, then wait for it to finish
        npc.SetAgentVelocity(0f, isStopped: true);
        if (npc.Anim != null)
        {
            npc.Anim.SetTrigger(AnimationConstants.Threatening);
            waitCoroutine = npc.StartCoroutine(WaitForThreatenAnimation());
        }
        else
        {
            npc.ChangeToState(npc.StayingState, AssassinState.Staying);
        }
    }

    public void Exit()
    {
        if (waitCoroutine != null)
        {
            npc.StopCoroutine(waitCoroutine);
            waitCoroutine = null;
        }
    }

    public void Update()
    {
    }

    public void Dispose()
    {
    }

    private IEnumerator WaitForThreatenAnimation()
    {
        yield return new WaitForSeconds(0.1f);

        if (npc.Anim != null)
        {
            AnimatorStateInfo stateInfo = npc.Anim.GetCurrentAnimatorStateInfo(0);
            if (stateInfo.IsTag("Threatening") || stateInfo.shortNameHash == AssassinNPC.ThreateningTrigger)
            {
                float remainingTime = stateInfo.length * (1f - Mathf.Clamp01(stateInfo.normalizedTime));
                yield return new WaitForSeconds(remainingTime + 0.2f);

                if (npc != null)
                {
                    npc.ChangeToState(npc.StayingState, AssassinState.Staying);
                }
            }
            else
            {
                yield return new WaitForSeconds(2.5f);
                if (npc != null)
                {
                    npc.ChangeToState(npc.StayingState, AssassinState.Staying);
                }
            }
        }
    }
}