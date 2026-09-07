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
            float distance = Vector3.Distance(assassin.transform.position, assassin.Target.position);
            float attackDist = assassin.CurrentEquipment.Equipment != null ? assassin.CurrentEquipment.Equipment.attackDistance : 1.5f;

            if (distance <= attackDist)
            {
                assassin.ChangeToState(assassin.AttackState, AssassinState.Attacking);
                return;
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