using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class ItemObject : MonoBehaviour
{
    [Header("Equipment Asset")]
    [SerializeField] private ScriptableEquipmentBase equipmentAsset;

    public ScriptableEquipmentBase Equipment => equipmentAsset;
    public AudioSource AudioSource { get; private set; }
    public bool IsPickedUp { get; private set; }

    public EquipmentCategory ItemCategory => equipmentAsset != null ? equipmentAsset.Category : EquipmentCategory.None;

    private Rigidbody itemRigidbody;
    private Collider[] itemColliders;
    private bool[] originalColliderStates;
    private Renderer[] itemRenderers;
    private bool[] originalRendererStates;
    private bool originalUseGravity;
    private bool originalIsKinematic;
    private bool originalDetectCollisions;

    private void Awake()
    {
        AudioSource = GetComponent<AudioSource>();
        itemRigidbody = GetComponent<Rigidbody>();
        itemColliders = GetComponentsInChildren<Collider>(true);
        itemRenderers = GetComponentsInChildren<Renderer>(true);
        originalColliderStates = new bool[itemColliders.Length];
        originalRendererStates = new bool[itemRenderers.Length];

        for (int i = 0; i < itemColliders.Length; i++)
        {
            originalColliderStates[i] = itemColliders[i].enabled;
        }

        for (int i = 0; i < itemRenderers.Length; i++)
        {
            originalRendererStates[i] = itemRenderers[i].enabled;
        }

        originalUseGravity = itemRigidbody.useGravity;
        originalIsKinematic = itemRigidbody.isKinematic;
        originalDetectCollisions = itemRigidbody.detectCollisions;

    }
    private void Start()
    {
        if (this.IsWeapon())
        {
            SceneItemManager.Instance?.RegisterWeapon(this);
        }
    }

    public void SetEnabled(bool enabled)
    {
        SetVisible(enabled);
        foreach (var collider in itemColliders)
        {
            collider.enabled = enabled;
        }
        itemRigidbody.isKinematic = !enabled;
        itemRigidbody.useGravity = enabled;
        itemRigidbody.detectCollisions = enabled;
        itemRigidbody.linearVelocity = Vector3.zero;
        itemRigidbody.angularVelocity = Vector3.zero;
    }

    public bool PickUp(Transform holdPoint)
    {
        if (holdPoint == null || IsPickedUp)
        {
            return false;
        }

        IsPickedUp = true;
        SetEnabled(false);

        foreach (Collider weaponCollider in itemColliders)
        {
            weaponCollider.enabled = false;
        }

        transform.SetParent(holdPoint, false);
        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;

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

        itemRigidbody.useGravity = originalUseGravity;
        itemRigidbody.isKinematic = originalIsKinematic;
        itemRigidbody.detectCollisions = originalDetectCollisions;

        for (int i = 0; i < itemColliders.Length; i++)
        {
            itemColliders[i].enabled = originalColliderStates[i];
        }

        SetVisible(true);

        if (!itemRigidbody.isKinematic)
        {
            itemRigidbody.linearVelocity = velocity;
        }

        return true;
    }

    public void SetVisible(bool visible)
    {
        for (int i = 0; i < itemRenderers.Length; i++)
        {
            itemRenderers[i].enabled = visible && originalRendererStates[i];
        }
    }

    public void UsItem(Transform user, Transform target)
    {
        if (equipmentAsset != null)
        {
            equipmentAsset.UseEquipment(user, target, this);
        }
        else
        {
            Debug.LogWarning($"[ItemObject]{gameObject.name} does not have any Equipment assets attached!");
        }
    }

    public bool IsWeapon()
    {
        return ItemCategory == EquipmentCategory.Melee || ItemCategory == EquipmentCategory.Gun || ItemCategory == EquipmentCategory.Bomb;
    }
}