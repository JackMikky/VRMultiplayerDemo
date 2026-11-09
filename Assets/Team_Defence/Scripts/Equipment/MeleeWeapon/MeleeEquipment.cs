using UnityEngine;

public abstract class MeleeEquipmentBase : ScriptableEquipmentBase
{
    [Header("Melee Base Settings")]
    [Tooltip("The layer used for hit detection contains only obstacles; it does not include the target or the object itself.")]
    [SerializeField] protected LayerMask obstacleLayerMask;

    [Tooltip("Starting height offset for gaze detection")]
    [SerializeField] protected float eyeHeight = 1.2f;

    private void OnValidate()
    {
        Category = EquipmentCategory.Melee;
    }

    public override void UseEquipment(Transform user, Transform target, WeaponObject weaponContext)
    {
        if (weaponContext == null || target == null) return;

        float distance = Vector3.Distance(user.position, target.position);
        if (distance > attackDistance) return;

        Vector3 origin = user.position + Vector3.up * eyeHeight;
        Vector3 targetPoint = target.position + Vector3.up * eyeHeight;

        if (Physics.Linecast(origin, targetPoint, obstacleLayerMask))
        {
            return;
        }

        if (target.TryGetComponent<IDamageable>(out var damageable))
        {
            damageable.TakeDamage(damage, user.gameObject);
        }

        PlayAttackSound(weaponContext.AudioSource);

        OnHit(user, target, weaponContext);
    }

    /// <summary>
    /// Subclasses can override this to add their own specific behavioral logic (such as bleeding effects, combo mechanics, logging, etc.).
    /// </summary>
    protected virtual void OnHit(Transform user, Transform target, WeaponObject weaponContext)
    { }
}