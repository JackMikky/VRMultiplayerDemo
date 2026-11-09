using UnityEngine;

[CreateAssetMenu(fileName = "NewAssassinRushing", menuName = BehaviorConstants.MoveBehaviorMenuName + "AssassinRushing")]
public class AssassinRushingBehavior : ScriptableMoveBehavior
{
    [Header("Pursuit Settings")]
    [Tooltip("Pathfinding path refresh interval (seconds)")]
    [SerializeField] private float pathUpdateInterval = 0.2f;

    public override void Enter(NPCBase npc)
    {
        if (npc is AssassinNPC assassin)
        {
            assassin.SetAgentVelocity(assassin.runSpeed, isStopped: false);
            assassin.ResetMovementAnimationFlags();

            if (assassin.Anim != null)
            {
                assassin.Anim.SetBool(AnimationConstants.IsRunning, true);
            }

            assassin.SetDestinationToTarget();

            assassin.nextPathUpdateTime = Time.time + pathUpdateInterval;
        }
    }

    public override void UpdateBehavior(NPCBase npc)
    {
        if (npc.Target == null) return;

        if (npc is AssassinNPC assassin)
        {
            Vector3 targetOffset = assassin.Target.position - assassin.transform.position;
            targetOffset.y = 0f;
            float distance = targetOffset.magnitude;

            if (assassin.Agent != null && assassin.Agent.enabled && assassin.Agent.isOnNavMesh)
            {
                ScriptableEquipmentBase equipment = assassin.CurrentEquipment != null
                    ? assassin.CurrentEquipment.Equipment
                    : null;

                bool reachedAttackRange = equipment != null
                    ? distance <= Mathf.Max(0f, equipment.attackDistance)
                    : !assassin.Agent.pathPending
                        && assassin.Agent.remainingDistance <= assassin.Agent.stoppingDistance;

                if (reachedAttackRange)
                {
                    assassin.ChangeToState(assassin.AttackState, AssassinState.Attacking);
                    return;
                }
            }

            if (distance > assassin.startRunningDistance)
            {
                assassin.ChangeToState(assassin.ApproachingState, AssassinState.Approaching);
                return;
            }

            if (Time.time >= assassin.nextPathUpdateTime)
            {
                assassin.nextPathUpdateTime = Time.time + pathUpdateInterval;
                assassin.SetDestinationToTarget();
            }

            assassin.SyncMovementAnimation();
        }
    }

    public override void Exit(NPCBase npc)
    {
    }
}