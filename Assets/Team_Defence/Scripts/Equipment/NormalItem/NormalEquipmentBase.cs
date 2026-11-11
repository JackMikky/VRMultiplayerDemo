using UnityEngine;

[CreateAssetMenu(fileName = "NewNormalItem", menuName = EquipmentConstants.NormalMenuName + "NormalItem")]
public class NormalEquipmentBase : ScriptableEquipmentBase
{
    private void OnValidate()
    {
        Category = EquipmentCategory.Normal;
    }

    public override void UseEquipment(Transform user, Transform target, ItemObject weaponContext)
    {
    }
}