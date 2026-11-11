using System.Collections;
using UnityEngine;

[CreateAssetMenu(fileName = "NewTimedBomb", menuName = EquipmentConstants.BombMenuName + "TimedBomb")]
public class TimedBomb : BombEquipmentBase
{
    [Header("Timed Bomb Settings")]
    [SerializeField, Min(0.1f)] private float fuseSeconds = 3f;

    public override void UseEquipment(Transform user, Transform target, ItemObject weaponContext)
    {
        if (weaponContext == null || user == null || target == null) return;
        if (Vector3.Distance(user.position, target.position) > attackDistance) return;

        Vector3 armedPosition = target.position;
        weaponContext.StartCoroutine(ExplodeAfterDelay(user, target, weaponContext, armedPosition));
    }

    public void Plant(SearchableObject container, Transform user, ItemObject bombItem)
    {
        if (container == null) return;

        container.StartCoroutine(ExplodePlantedAfterDelay(container, user, bombItem));
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

    private IEnumerator ExplodePlantedAfterDelay(SearchableObject container, Transform user, ItemObject bombItem)
    {
        yield return new WaitForSeconds(fuseSeconds);

        Transform center = container.transform;
        ExplodeAt(user, center, bombItem, center.position);

        GameManager.Instance?.EndGame();
    }

    protected override void OnExploded(Transform user, Transform target, ItemObject weaponContext, Vector3 explosionCenter)
    {
        Debug.Log($"[TimedBomb]{user.name} exploded {Name} after {fuseSeconds:0.00}s at {explosionCenter}");
    }
}