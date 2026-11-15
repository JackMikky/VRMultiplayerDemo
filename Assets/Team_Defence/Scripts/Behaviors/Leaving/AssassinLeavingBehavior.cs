using UnityEngine;

[CreateAssetMenu(fileName = "NewAssassinLeaving", menuName = BehaviorConstants.LeavingBehaviorMenuName + "AssassinLeaving")]
public class AssassinLeavingBehavior : ScriptableLeavingBehavior
{
    public override void Enter(NPCBase npc)
    {
    }

    public override void UpdateBehavior(NPCBase npc)
    {
    }

    public override void Exit(NPCBase npc)
    {
    }

    public override void Despawn(NPCBase npc)
    {
        if (NPCManager.Instance != null)
        {
            NPCManager.Instance.UnregisterAssassin(npc as AssassinNPC, wasCaptured: false);
        }

        Object.Destroy(npc.gameObject);
    }
}
