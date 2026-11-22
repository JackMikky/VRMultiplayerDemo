using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class BombEquipmentBase : ScriptableEquipmentBase
{
    [Header("Bomb Base Settings")]
    [SerializeField, Min(0.1f)] protected float explosionRadius = 2.5f;

    [SerializeField] protected LayerMask damageMask = ~0;
    [SerializeField] protected GameObject explosionEffectPrefab;
    [SerializeField, Min(0f)] protected float explosionEffectLifetime = 3f;
    [SerializeField] protected bool canDamageUser;

    [Header("Panic")]
    [Tooltip("Citizens within this distance of the explosion panic. Independent from the damage radius.")]
    [SerializeField, Min(0f)] protected float panicRadius = 20f;

    public float ExplosionRadius => explosionRadius;
    public float PanicRadius => panicRadius;

    private void OnValidate()
    {
        Category = EquipmentCategory.Bomb;
    }

    public override void UseEquipment(Transform user, Transform target, ItemObject weaponContext)
    {
        if (weaponContext == null || user == null || target == null) return;
        if (Vector3.Distance(user.position, target.position) > attackDistance) return;

        ExplodeAt(user, target, weaponContext, target.position);
    }

    protected void ExplodeAt(Transform user, Transform target, ItemObject weaponContext, Vector3 explosionCenter)
    {
        if (panicRadius > 0f && NPCManager.Instance != null)
        {
            NPCManager.Instance.BroadcastPanic(explosionCenter, panicRadius);
        }

        if (explosionEffectPrefab != null)
        {
            GameObject effect = Object.Instantiate(explosionEffectPrefab, explosionCenter, Quaternion.identity);
            Object.Destroy(effect, explosionEffectLifetime);
        }
        else
        {
            Debug.Log($"[Bomb VFX]{Name} explosion VFX at {explosionCenter} (radius={explosionRadius})");
        }

        Collider[] hitColliders = Physics.OverlapSphere(explosionCenter, explosionRadius, damageMask);
        var damagedTargets = new HashSet<IDamageable>();

        for (int i = 0; i < hitColliders.Length; i++)
        {
            var hitCollider = hitColliders[i];
            if (hitCollider == null) continue;
            if (!canDamageUser && user != null && hitCollider.transform.IsChildOf(user)) continue;

            if (hitCollider.TryGetComponent<IDamageable>(out var damageable) && damagedTargets.Add(damageable))
            {
                damageable.TakeDamage(damage, user != null ? user.gameObject : null);
            }
        }

        PlayAttackSound(weaponContext != null ? weaponContext.AudioSource : null);
        if (attackSounds == null || attackSounds.Count == 0)
        {
            Debug.Log($"[Bomb SFX]{Name} explosion SFX at {explosionCenter}");
        }

        OnExploded(user, target, weaponContext, explosionCenter);
    }

    protected virtual void OnExploded(Transform user, Transform target, ItemObject weaponContext, Vector3 explosionCenter)
    {
    }
}