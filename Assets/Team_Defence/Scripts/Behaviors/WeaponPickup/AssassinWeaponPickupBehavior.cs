using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[CreateAssetMenu(fileName = "NewAssassinWeaponPickup", menuName = BehaviorConstants.PickupBehaviorMenuName + "AssassinWeaponPickup")]
public class AssassinWeaponPickupBehavior : ScriptablePickupBehavior
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

    public override ItemObject SelectBestWeapon(NPCBase npc, IReadOnlyList<ItemObject> availableWeapons)
    {
        if (availableWeapons == null || availableWeapons.Count == 0)
        {
            return null;
        }

        AssassinNPC assassin = npc as AssassinNPC;
        AssassinPersonalityProfile personality = assassin != null ? assassin.PersonalityProfile : null;

        float totalWeight = 0f;
        for (int i = 0; i < availableWeapons.Count; i++)
        {
            ItemObject candidate = availableWeapons[i];
            if (candidate == null)
            {
                continue;
            }

            totalWeight += personality != null ? personality.GetWeaponWeight(candidate.ItemCategory) : 1f;
        }

        ItemObject selectedWeapon = null;

        if (totalWeight <= 0f)
        {
            selectedWeapon = availableWeapons.FirstOrDefault(item => item != null);
        }
        else
        {
            float roll = Random.Range(0f, totalWeight);
            float cumulative = 0f;

            for (int i = 0; i < availableWeapons.Count; i++)
            {
                ItemObject candidate = availableWeapons[i];
                if (candidate == null)
                {
                    continue;
                }

                cumulative += personality != null ? personality.GetWeaponWeight(candidate.ItemCategory) : 1f;
                if (roll <= cumulative)
                {
                    selectedWeapon = candidate;
                    break;
                }
            }

            if (selectedWeapon == null)
            {
                selectedWeapon = availableWeapons.LastOrDefault(item => item != null);
            }
        }

        return selectedWeapon;
    }
}