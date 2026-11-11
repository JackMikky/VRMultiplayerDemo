using UnityEngine;

[CreateAssetMenu(fileName = "NewGrenade", menuName = EquipmentConstants.BombMenuName + "Grenade")]
public class Grenade : BombEquipmentBase
{
    protected override void OnExploded(Transform user, Transform target, ItemObject weaponContext, Vector3 explosionCenter)
    {
        Debug.Log($"[Grenade]{user.name} exploded {Name} near {target.name}, radius={explosionRadius}, damage={damage}");
    }
}