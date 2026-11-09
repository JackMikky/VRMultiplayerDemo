using UnityEngine;

[CreateAssetMenu(fileName = "NewAssassinPreparation", menuName = BehaviorConstants.MoveBehaviorMenuName + "AssassinPreparation")]
public class AssassinPreparationBehavior : ScriptableMoveBehavior
{
    [Header("Hesitation Settings")]
    [SerializeField, Min(0f)] private float minHesitationDuration = 1f;
    [SerializeField, Min(0f)] private float maxHesitationDuration = 8f;

    [Header("Vigilance Settings")]
    [SerializeField, Range(0f, 180f)] private float lookAroundAngle = 45f;
    [SerializeField, Min(0f)] private float lookAroundSpeed = 1.5f;

    [Header("Path Settings")]
    [SerializeField, Min(0.01f)] private float pathUpdateInterval = 0.2f;

    public override void Enter(NPCBase npc)
    {
        if (npc is not AssassinNPC assassin) return;

        assassin.reachedPreparationPoint = false;
        assassin.SetNavigationMode(useAgent: true);
        if (assassin.Agent != null) assassin.Agent.autoTraverseOffMeshLink = true;
        assassin.SetAgentVelocity(assassin.walkSpeed, isStopped: false);
        assassin.ResetMovementAnimationFlags();

        if (assassin.Anim != null) assassin.Anim.SetBool(AnimationConstants.IsWalking, true);

        assassin.SetDestinationToPreparationPoint();
        assassin.nextPathUpdateTime = Time.time + pathUpdateInterval;
    }

    public override void UpdateBehavior(NPCBase npc)
    {
        if (npc is not AssassinNPC assassin) return;

        if (!assassin.reachedPreparationPoint)
        {
            UpdateMovement(assassin);
            return;
        }

        UpdateVigilance(assassin);

        if (Time.time >= assassin.preparationEndTime)
        {
            assassin.ChangeToState(assassin.RushingState, AssassinState.Rushing);
        }
    }

    public override void Exit(NPCBase npc)
    {
        if (npc is AssassinNPC assassin && assassin.Agent != null)
        {
            assassin.Agent.autoTraverseOffMeshLink = false;
        }
    }

    private void UpdateMovement(AssassinNPC assassin)
    {
        if (assassin.Agent == null || !assassin.Agent.enabled || !assassin.Agent.isOnNavMesh) return;

        Vector3 preparationOffset = assassin.PreparationPoint - assassin.transform.position;
        preparationOffset.y = 0f;
        bool isAlreadyAtPreparationPoint = preparationOffset.sqrMagnitude
            <= assassin.Agent.stoppingDistance * assassin.Agent.stoppingDistance;

        if (isAlreadyAtPreparationPoint
            || (!assassin.Agent.pathPending
                && assassin.Agent.remainingDistance <= assassin.Agent.stoppingDistance))
        {
            assassin.reachedPreparationPoint = true;
            float hesitationDuration = Mathf.Lerp(
                maxHesitationDuration,
                minHesitationDuration,
                assassin.Courage);
            assassin.preparationEndTime = Time.time + hesitationDuration;
            assassin.SetAgentVelocity(0f, isStopped: true);
            assassin.ResetMovementAnimationFlags();

            if (assassin.Anim != null) assassin.Anim.SetBool(AnimationConstants.IsIdling, true);
            return;
        }

        if (Time.time >= assassin.nextPathUpdateTime)
        {
            assassin.nextPathUpdateTime = Time.time + pathUpdateInterval;
            assassin.SetDestinationToPreparationPoint();
        }

        assassin.SyncMovementAnimation();
    }

    private void UpdateVigilance(AssassinNPC assassin)
    {
        if (assassin.Target == null) return;

        Vector3 direction = assassin.Target.position - assassin.transform.position;
        direction.y = 0f;
        if (direction.sqrMagnitude <= 0.001f) return;

        float angle = Mathf.Sin(Time.time * lookAroundSpeed) * lookAroundAngle;
        Vector3 lookDirection = Quaternion.Euler(0f, angle, 0f) * direction.normalized;
        Quaternion targetRotation = Quaternion.LookRotation(lookDirection);
        assassin.transform.rotation = Quaternion.Slerp(
            assassin.transform.rotation,
            targetRotation,
            Time.deltaTime * 5f);
    }
}