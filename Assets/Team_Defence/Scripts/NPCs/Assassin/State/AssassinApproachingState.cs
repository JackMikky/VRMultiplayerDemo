using UnityEngine;

public class AssassinApproachingState : IState
{
    private AssassinNPC npc;

    private float pathUpdateTimer;

    private const float PathUpdateInterval = 0.25f;

    public AssassinApproachingState(AssassinNPC npc)
    {
        this.npc = npc;
    }

    public void Dispose()
    {
        // no-op
    }

    public void Enter()
    {
        npc.SetAgentVelocity(npc.walkSpeed, isStopped: false);
        npc.ResetMovementAnimationFlags();
        if (npc.Anim != null) npc.Anim.SetBool(AnimationConstants.IsWalking, true);

        npc.SetDestinationToTarget();
        pathUpdateTimer = PathUpdateInterval;
        npc.ApproachingBehavior?.Enter(npc);
    }

    public void Exit()
    {
        npc.ApproachingBehavior?.Exit(npc);
    }

    public void Update()
    {
        if (npc.Target == null) return;

        float distance = Vector3.Distance(npc.transform.position, npc.Target.position);
        if (distance <= npc.startRunningDistance)
        {
            npc.ChangeToState(npc.RushingState, AssassinState.Rushing);
            return;
        }

        pathUpdateTimer -= Time.deltaTime;
        if (pathUpdateTimer <= 0f)
        {
            pathUpdateTimer = PathUpdateInterval;
            npc.SetDestinationToTarget();
        }

        npc.SyncMovementAnimation();

        npc.ApproachingBehavior?.UpdateBehavior(npc);
    }
}