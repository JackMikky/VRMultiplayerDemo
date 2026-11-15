using UnityEngine;

public abstract class ScriptableBombPlantBehavior : ScriptableBehaviorBase
{
    [SerializeField, Min(0f)] protected float plantingDuration = 14.5f;

    public float PlantingDuration => plantingDuration;

    public abstract void PlantBomb(NPCBase npc, SearchableObject targetSearchableObject);
}