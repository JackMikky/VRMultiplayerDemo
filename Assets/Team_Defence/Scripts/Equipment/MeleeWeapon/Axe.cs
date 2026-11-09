using UnityEngine;

[CreateAssetMenu(fileName = "NewAxe", menuName = EquipmentConstants.MeleeWeaponMenuName + "Axe")]
public class Axe : MeleeEquipmentBase
{
    [SerializeField] private float knockbackForce = 5f;

    protected override void OnHit(Transform user, Transform target, WeaponObject weaponContext)
    {
    }
}