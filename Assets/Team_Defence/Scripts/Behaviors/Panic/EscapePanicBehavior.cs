using UnityEngine;

[CreateAssetMenu(fileName = "NewEscapePanic", menuName = BehaviorConstants.PanicBehaviorMenuName + "EscapePanic")]
public class EscapePanicBehavior : ScriptablePanicBehavior
{
    [SerializeField] private float runSpeed = 3.5f;

    public override void Enter(NPCBase npc)
    {
        npc.SetNavigationMode(true);
        npc.Agent.speed = runSpeed;
        // Set the destination to a point away from the target
        // npc.Agent.SetDestination();
    }

    public override void Exit(NPCBase npc)
    {
        npc.SetNavigationMode(false);
    }

    public override void UpdateBehavior(NPCBase npc)
    {
    }
}