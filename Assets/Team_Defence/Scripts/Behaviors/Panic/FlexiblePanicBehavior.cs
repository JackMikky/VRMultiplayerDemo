using UnityEngine;

[CreateAssetMenu(fileName = "NewFlexiblePanic", menuName = BehaviorConstants.PanicBehaviorMenuName + "FlexiblePanic")]
public class FlexiblePanicBehavior : ScriptablePanicBehavior
{
    [Header("Animation Trigger List")]
    [SerializeField] private string[] availableAnimationTriggers;

    public override void Enter(NPCBase npc)
    {
    }

    public override void Exit(NPCBase npc)
    {
    }

    public override void UpdateBehavior(NPCBase npc)
    {
    }
}