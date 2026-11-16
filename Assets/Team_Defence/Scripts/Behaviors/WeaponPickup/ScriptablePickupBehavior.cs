using System.Collections.Generic;
using UnityEngine;

public abstract class ScriptablePickupBehavior : ScriptableBehaviorBase
{
    [SerializeField, Min(0f)] protected float pickupDuration = 1.5f;

    public float PickupDuration => pickupDuration;

    public abstract ItemObject SelectBestWeapon(NPCBase npc, IReadOnlyList<ItemObject> availableWeapons);
}
