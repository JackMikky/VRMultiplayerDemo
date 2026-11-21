using System.Collections;
using UnityEngine;

[CreateAssetMenu(fileName = "NewTimedBomb", menuName = EquipmentConstants.BombMenuName + "TimedBomb")]
public class TimedBomb : BombEquipmentBase
{
    [Header("Timed Bomb Settings")]
    [SerializeField, Min(0.1f)] private float fuseSeconds = 3f;

    [Tooltip("How long the planted bomb object stays in the scene after exploding before being destroyed.")]
    [SerializeField, Min(0f)] private float despawnDelay = 2.5f;

    public float FuseSeconds => fuseSeconds;
    public float DespawnDelay => despawnDelay;

    public override void UseEquipment(Transform user, Transform target, ItemObject weaponContext)
    {
        if (weaponContext == null || user == null || target == null) return;
        if (Vector3.Distance(user.position, target.position) > attackDistance) return;

        Vector3 armedPosition = target.position;
        weaponContext.StartCoroutine(ExplodeAfterDelay(user, target, weaponContext, armedPosition));
    }

    public void Detonate(Transform user, Transform target, ItemObject weaponContext, Vector3 explosionCenter)
    {
        ExplodeAt(user, target, weaponContext, explosionCenter);
    }

    private IEnumerator ExplodeAfterDelay(Transform user, Transform target, ItemObject weaponContext, Vector3 explosionCenter)
    {
        yield return new WaitForSeconds(fuseSeconds);

        if (weaponContext == null)
        {
            yield break;
        }

        ExplodeAt(user, target, weaponContext, explosionCenter);
    }

    protected override void OnExploded(Transform user, Transform target, ItemObject weaponContext, Vector3 explosionCenter)
    {
        Debug.Log($"[TimedBomb]{user.name} exploded {Name} after {fuseSeconds:0.00}s at {explosionCenter}");
    }
}