using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class WeaponObject : MonoBehaviour
{
    [Header("Equipment Asset")]
    [SerializeField] private ScriptableEquipmentBase equipmentAsset;

    public ScriptableEquipmentBase Equipment => equipmentAsset;
    public AudioSource AudioSource { get; private set; }

    private void Awake()
    {
        AudioSource = GetComponent<AudioSource>();
    }

    public void UseWeapon(Transform user, Transform target)
    {
        if (equipmentAsset != null)
        {
            equipmentAsset.UseEquipment(user, target, this);
        }
        else
        {
            Debug.LogWarning($"[WeaponObject]{gameObject.name} does not have any Equipment assets attached!");
        }
    }
}