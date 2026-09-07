using UnityEngine;

[CreateAssetMenu(fileName = "NewHand", menuName = EquipmentConstants.MeleeWeaponMenuName + "Hand")]
public class Hand : MeleeEquipmentBase
{
    protected override void OnHit(Transform user, Transform target, WeaponObject weaponContext)
    {
        Debug.Log($"[Hand]{user.name} slashes at {target.name} using {Name}, dealing {damage} damage!");
    }
}