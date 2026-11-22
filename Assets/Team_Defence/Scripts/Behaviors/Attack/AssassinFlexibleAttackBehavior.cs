using UnityEngine;

[CreateAssetMenu(fileName = "NewAssassinFlexibleAttack", menuName = BehaviorConstants.AttackBehaviorMenuName + "AssassinFlexibleAttack")]
public class AssassinFlexibleAttackBehavior : ScriptableAttackBehavior
{
    [Header("Attack Settings")]
    [SerializeField] private string attackTriggerName = "Attack";

    [Tooltip("Total shots per attack when using a gun")]
    [SerializeField, Min(1)] private int gunShotCount = 3;

    [Tooltip("Seconds between gun shots")]
    [SerializeField, Min(0.05f)] private float gunShotInterval = 0.8f;

    public int GunShotCount => gunShotCount;
    public float GunShotInterval => gunShotInterval;

    public override void Enter(NPCBase npc)
    {
        npc.SetAgentVelocity(0f, isStopped: true);
        npc.ResetMovementAnimationFlags();

        if (npc is AssassinNPC equippedAssassin && equippedAssassin.CurrentEquipment != null)
        {
            equippedAssassin.CurrentEquipment.SetVisible(true);
        }

        Fire(npc);
    }

    public void Fire(NPCBase npc)
    {
        if (npc.Target != null)
        {
            Vector3 lookTarget = new Vector3(npc.Target.position.x, npc.transform.position.y, npc.Target.position.z);
            npc.transform.LookAt(lookTarget);
        }

        if (npc.Anim != null)
        {
            npc.Anim.SetTrigger(Animator.StringToHash(attackTriggerName));
        }

        if (npc is AssassinNPC attackingAssassin && attackingAssassin.CurrentEquipment != null)
        {
            attackingAssassin.CurrentEquipment.UsItem(attackingAssassin.transform, attackingAssassin.Target);
        }

        Debug.Log("[Behavior] The assassin successfully launches an attack!");
    }

    public override void UpdateBehavior(NPCBase npc)
    { }

    public override void Exit(NPCBase npc)
    {
        if (npc is AssassinNPC assassin && assassin.CurrentEquipment != null)
        {
            assassin.CurrentEquipment.SetVisible(false);
        }
    }
}