using UnityEngine;

public abstract class ScriptableBehaviorBase : ScriptableObject
{
    [SerializeField] protected string defaultAnimationTrigger;

    public abstract void Enter(NPCBase npc);

    public abstract void UpdateBehavior(NPCBase npc);

    public abstract void Exit(NPCBase npc);
}