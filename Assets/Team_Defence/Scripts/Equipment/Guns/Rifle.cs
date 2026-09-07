using UnityEngine;

[CreateAssetMenu(fileName = "NewRifle", menuName = EquipmentConstants.GunMenuName + "Rifle")]
public class Rifle : GunEquipmentBase
{
    [SerializeField] private int bulletsPerShot = 3;

    protected override void OnFired(Transform user, Transform target, WeaponObject weaponContext)
    {
        // 步枪特有的连发逻辑
    }
}