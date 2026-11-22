using UnityEngine;

public abstract class GunEquipmentBase : ScriptableEquipmentBase
{
    [Header("Gun Base Settings")]
    [Tooltip("The layers used for hit detection should include obstacles and damageable targets.")]
    [SerializeField] protected LayerMask hitMask;

    [Tooltip("Initial vertical offset of the shot from the muzzle")]
    [SerializeField] protected float muzzleHeight = 1.4f;

    [Tooltip("On-hit effect, optional")]
    [SerializeField] protected GameObject hitEffectPrefab;

    [Tooltip("How long the hit effect stays in the scene before being destroyed")]
    [SerializeField, Min(0f)] protected float hitEffectLifetime = 2f;

    [Tooltip("Muzzle flash effect spawned at the weapon's fire point, optional")]
    [SerializeField] protected GameObject muzzleEffectPrefab;

    [Tooltip("How long the muzzle effect stays in the scene before being destroyed")]
    [SerializeField, Min(0f)] protected float muzzleEffectLifetime = 1f;

    [Header("Panic")]
    [Tooltip("Citizens within this radius panic when the gun is fired. 0 disables.")]
    [SerializeField, Min(0f)] protected float panicRadius = 10f;

    public float PanicRadius => panicRadius;

    #region Debugging Settings

    [Header("Debug")]
    [Tooltip("Draw a visible ray in the Scene view whenever this weapon fires")]
    [SerializeField] protected bool showDebugRay = true;

    [Tooltip("How long the debug ray stays visible (seconds)")]
    [SerializeField] protected float debugRayDuration = 1f;

    [Tooltip("Ray color when the shot hits something")]
    [SerializeField] protected Color debugRayHitColor = Color.red;

    [Tooltip("Ray color when the shot doesn't hit anything within range")]
    [SerializeField] protected Color debugRayMissColor = Color.yellow;

    #endregion Debugging Settings

    private void OnValidate()
    {
        Category = EquipmentCategory.Gun;
    }

    public override void UseEquipment(Transform user, Transform target, ItemObject weaponContext)
    {
        if (weaponContext == null || target == null) return;

        Transform firePoint = weaponContext.FirePoint;
        Vector3 origin = firePoint != null ? firePoint.position : user.position + Vector3.up * muzzleHeight;
        Vector3 targetPoint = target.position + Vector3.up * muzzleHeight;
        Vector3 direction = (targetPoint - origin).normalized;

        if (panicRadius > 0f && NPCManager.Instance != null)
        {
            NPCManager.Instance.BroadcastPanic(origin, panicRadius);
        }

        bool didHit = Physics.Raycast(origin, direction, out RaycastHit hit, attackDistance, hitMask);

        if (muzzleEffectPrefab != null)
        {
            GameObject muzzleEffect = Object.Instantiate(muzzleEffectPrefab, origin, Quaternion.LookRotation(direction), firePoint);
            Object.Destroy(muzzleEffect, muzzleEffectLifetime);
        }

        if (didHit)
        {
            if (hitEffectPrefab != null)
            {
                GameObject hitEffect = Object.Instantiate(hitEffectPrefab, hit.point, Quaternion.LookRotation(hit.normal));
                Object.Destroy(hitEffect, hitEffectLifetime);
            }

            if (hit.collider.TryGetComponent<IDamageable>(out var damageable))
            {
                damageable.TakeDamage(damage, user.gameObject);
            }
        }
#if UNITY_EDITOR
        DrawDebugRay(origin, direction, didHit ? hit.point : origin + direction * attackDistance, didHit);
#endif

        PlayAttackSound(weaponContext.AudioSource);

        OnFired(user, target, weaponContext);
    }

    /// <summary>
    /// Subclasses can override this to add their own specific behavioral logic (such as rapid fire, spread shots, logging, etc.).
    /// </summary>
    protected virtual void OnFired(Transform user, Transform target, ItemObject weaponContext)
    { }

    #region Debugging

    private void DrawDebugRay(Vector3 origin, Vector3 direction, Vector3 endPoint, bool didHit)
    {
        if (!showDebugRay) return;

        Color color = didHit ? debugRayHitColor : debugRayMissColor;
        Debug.DrawLine(origin, endPoint, color, debugRayDuration);

        if (didHit)
        {
            Debug.DrawRay(endPoint, Vector3.up * 0.2f, color, debugRayDuration);
            Debug.DrawRay(endPoint, Vector3.right * 0.2f, color, debugRayDuration);
        }
    }

    #endregion Debugging
}