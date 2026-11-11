using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewAssassinPersonality", menuName = "NPC/AssassinPersonality")]
public class AssassinPersonalityProfile : ScriptableObject
{
    [Serializable]
    private class WeaponPreference
    {
        public EquipmentCategory category;
        [Min(0f)] public float weight = 1f;
    }

    [SerializeField] private List<WeaponPreference> weaponPreferences = new List<WeaponPreference>();

    public float GetWeaponWeight(EquipmentCategory category)
    {
        foreach (WeaponPreference preference in weaponPreferences)
        {
            if (preference != null && preference.category == category)
            {
                return Mathf.Max(0f, preference.weight);
            }
        }

        return 1f;
    }
}
