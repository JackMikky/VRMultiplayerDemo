using UnityEngine;

[CreateAssetMenu(fileName = "NewAssassinFlexibleAttack", menuName = BehaviorConstants.AttackBehaviorMenuName + "AssassinFlexibleAttack")]
public class AssassinFlexibleAttackBehavior : ScriptableAttackBehavior
{
    [Header("Attack Settings")]
    [SerializeField] private string attackTriggerName = "Attack";

    public override void Enter(NPCBase npc)
    {
        npc.SetAgentVelocity(0f, isStopped: true);
        npc.ResetMovementAnimationFlags();

        if (npc.Target != null)
        {
            Vector3 lookTarget = new Vector3(npc.Target.position.x, npc.transform.position.y, npc.Target.position.z);
            npc.transform.LookAt(lookTarget);
        }

        if (npc.Anim != null)
        {
            npc.Anim.SetTrigger(Animator.StringToHash(attackTriggerName));
        }

        if (npc is AssassinNPC assassin && assassin.CurrentEquipment != null)
        {
            assassin.CurrentEquipment.UseWeapon(assassin.transform, assassin.Target);
        }

        Debug.Log("[Behavior] The assassin successfully launches an attack!");
    }

    public override void UpdateBehavior(NPCBase npc)
    { }

    public override void Exit(NPCBase npc)
    { }
}