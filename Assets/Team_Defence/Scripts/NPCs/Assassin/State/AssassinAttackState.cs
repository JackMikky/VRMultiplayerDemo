public class AssassinAttackState : IState
{
    private AssassinNPC npc;

    public AssassinAttackState(AssassinNPC npc)
    {
        this.npc = npc;
    }

    private int shotsFired;
    private float nextShotTime;

    public void Enter()
    {
        shotsFired = 1;
        nextShotTime = UnityEngine.Time.time;
        if (npc.AttackBehavior is AssassinFlexibleAttackBehavior flexible)
        {
            nextShotTime += flexible.GunShotInterval;
        }

        npc.AttackBehavior?.Enter(npc);
    }

    public void Exit()
    {
        npc.AttackBehavior?.Exit(npc);
    }

    public void Update()
    {
        if (npc.AttackBehavior is AssassinFlexibleAttackBehavior flexible
            && npc.CurrentEquipment != null
            && npc.CurrentEquipment.ItemCategory == EquipmentCategory.Gun
            && npc.Target != null
            && !IsTargetDead()
            && shotsFired < flexible.GunShotCount
            && UnityEngine.Time.time >= nextShotTime)
        {
            shotsFired++;
            nextShotTime = UnityEngine.Time.time + flexible.GunShotInterval;
            flexible.Fire(npc);
        }

        npc.AttackBehavior?.UpdateBehavior(npc);
    }

    public void Dispose()
    {
    }

    private bool IsTargetDead()
    {
        return npc.Target.TryGetComponent(out NPCBase targetNpc) && targetNpc.IsDead;
    }
}