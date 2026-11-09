using UnityEngine;

[RequireComponent(typeof(AudioSource))]
[RequireComponent(typeof(Rigidbody))]
public class WeaponObject : MonoBehaviour
{
    [Header("Equipment Asset")]
    [SerializeField] private ScriptableEquipmentBase equipmentAsset;

    public ScriptableEquipmentBase Equipment => equipmentAsset;
    public AudioSource AudioSource { get; private set; }
    public bool IsPickedUp { get; private set; }

    private Rigidbody weaponRigidbody;
    private Collider[] weaponColliders;
    private bool[] originalColliderStates;
    private Renderer[] weaponRenderers;
    private bool[] originalRendererStates;
    private bool originalUseGravity;
    private bool originalIsKinematic;
    private bool originalDetectCollisions;

    private void Awake()
    {
        AudioSource = GetComponent<AudioSource>();
        weaponRigidbody = GetComponent<Rigidbody>();
        weaponColliders = GetComponentsInChildren<Collider>(true);
        weaponRenderers = GetComponentsInChildren<Renderer>(true);
        originalColliderStates = new bool[weaponColliders.Length];
        originalRendererStates = new bool[weaponRenderers.Length];

        for (int i = 0; i < weaponColliders.Length; i++)
        {
            originalColliderStates[i] = weaponColliders[i].enabled;
        }

        for (int i = 0; i < weaponRenderers.Length; i++)
        {
            originalRendererStates[i] = weaponRenderers[i].enabled;
        }

        originalUseGravity = weaponRigidbody.useGravity;
        originalIsKinematic = weaponRigidbody.isKinematic;
        originalDetectCollisions = weaponRigidbody.detectCollisions;
    }

    public bool PickUp(Transform holdPoint)
    {
        if (holdPoint == null || IsPickedUp)
        {
            return false;
        }

        IsPickedUp = true;
        weaponRigidbody.linearVelocity = Vector3.zero;
        weaponRigidbody.angularVelocity = Vector3.zero;
        weaponRigidbody.isKinematic = true;
        weaponRigidbody.useGravity = false;
        weaponRigidbody.detectCollisions = false;

        foreach (Collider weaponCollider in weaponColliders)
        {
            weaponCollider.enabled = false;
        }

        transform.SetParent(holdPoint, false);
        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;
        SetVisible(false);
        return true;
    }

    public bool Drop(Vector3 velocity = default)
    {
        if (!IsPickedUp)
        {
            return false;
        }

        IsPickedUp = false;
        transform.SetParent(null, true);

        weaponRigidbody.useGravity = originalUseGravity;
        weaponRigidbody.isKinematic = originalIsKinematic;
        weaponRigidbody.detectCollisions = originalDetectCollisions;

        for (int i = 0; i < weaponColliders.Length; i++)
        {
            weaponColliders[i].enabled = originalColliderStates[i];
        }

        SetVisible(true);

        if (!weaponRigidbody.isKinematic)
        {
            weaponRigidbody.linearVelocity = velocity;
        }

        return true;
    }

    public void SetVisible(bool visible)
    {
        for (int i = 0; i < weaponRenderers.Length; i++)
        {
            weaponRenderers[i].enabled = visible && originalRendererStates[i];
        }
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