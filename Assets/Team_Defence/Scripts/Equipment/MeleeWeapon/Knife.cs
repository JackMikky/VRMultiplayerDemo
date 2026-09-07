using UnityEngine;

[CreateAssetMenu(fileName = "NewKnife", menuName = EquipmentConstants.MeleeWeaponMenuName + "Knife")]
public class Knife : MeleeEquipmentBase
{
    protected override void OnHit(Transform user, Transform target, WeaponObject weaponContext)
    {
        Debug.Log($"[Knife]{user.name} slashes at {target.name} using {Name}, dealing {damage} damage!");
    }
}