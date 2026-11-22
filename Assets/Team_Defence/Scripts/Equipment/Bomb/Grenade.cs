using UnityEngine;

[CreateAssetMenu(fileName = "NewGrenade", menuName = EquipmentConstants.BombMenuName + "Grenade")]
public class Grenade : BombEquipmentBase
{
    protected override void OnExploded(Transform user, Transform target, ItemObject weaponContext, Vector3 explosionCenter)
    {
        string userName = user != null ? user.name : "Unknown";
        string targetName = target != null ? target.name : "Unknown";
        Debug.Log($"[Grenade]{userName} exploded {Name} near {targetName}, radius={explosionRadius}, damage={damage}");
    }
}