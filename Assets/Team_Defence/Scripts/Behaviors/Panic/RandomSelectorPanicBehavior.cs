using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewRandomSelectorPanic", menuName = BehaviorConstants.PanicBehaviorMenuName + "RandomSelectorPanic")]
public class RandomSelectorPanicBehavior : ScriptablePanicBehavior
{
    [SerializeField] private List<ScriptableBehaviorBase> possibleBehaviors;

    public override void Enter(NPCBase npc)
    {
        if (possibleBehaviors == null || possibleBehaviors.Count == 0) return;
        if (npc is not CitizenNPC citizen) return;

        int randomIndex = Random.Range(0, possibleBehaviors.Count);
        citizen.activePanicBehavior = possibleBehaviors[randomIndex];

        citizen.activePanicBehavior?.Enter(npc);
    }

    public override void UpdateBehavior(NPCBase npc)
    {
        if (npc is CitizenNPC citizen)
        {
            citizen.activePanicBehavior?.UpdateBehavior(npc);
        }
    }

    public override void Exit(NPCBase npc)
    {
        if (npc is CitizenNPC citizen)
        {
            citizen.activePanicBehavior?.Exit(npc);
            citizen.activePanicBehavior = null;
        }
    }
}