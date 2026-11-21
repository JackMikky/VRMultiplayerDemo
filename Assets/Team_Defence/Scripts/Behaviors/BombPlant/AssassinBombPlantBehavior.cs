using UnityEngine;

[CreateAssetMenu(fileName = "NewAssassinBombPlant", menuName = BehaviorConstants.BombPlantBehaviorMenuName + "AssassinBombPlant")]
public class AssassinBombPlantBehavior : ScriptableBombPlantBehavior
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

    public override void PlantBomb(NPCBase npc, SearchableObject targetSearchableObject)
    {
        if (targetSearchableObject == null || npc.CurrentEquipment == null)
        {
            return;
        }

        AssassinNPC assassin = npc as AssassinNPC;

        if (!(npc.CurrentEquipment is BombItemObject bombItem))
        {
            Debug.LogWarning($"[{npc.name}] Equipped bomb is not a BombItemObject; cannot plant.", npc);
            return;
        }

        // Drop the bomb from the NPC
        if (bombItem.Drop())
        {
            // AddItem handles all the positioning and hierarchy setup
            targetSearchableObject.AddItem(bombItem);

            // Clear the NPC's equipment reference
            assassin?.ClearEquipment();

            Debug.Log($"[{npc.name}] Transferred bomb to {targetSearchableObject.name}");
        }

        // Plant the bomb (the bomb item runs its own explosion timer)
        bombItem.Plant(targetSearchableObject.transform, npc.transform);
        Debug.Log($"[{npc.name}] Planted bomb at {targetSearchableObject.name}");
    }
}