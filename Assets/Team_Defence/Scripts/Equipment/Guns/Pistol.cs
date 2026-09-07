using UnityEngine;

[CreateAssetMenu(fileName = "NewPistol", menuName = EquipmentConstants.GunMenuName + "Pistol")]
public class Pistol : GunEquipmentBase
{
    private void OnValidate()
    {
        Category = EquipmentCategory.Gun;
    }

    protected override void OnFired(Transform user, Transform target, WeaponObject weaponContext)
    {
        Debug.Log($"[Pistol]{user.name} fires at {target.name} using {Name}!");
    }
}