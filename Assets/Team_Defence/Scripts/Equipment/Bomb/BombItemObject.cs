using System.Collections;
using UnityEngine;

public class BombItemObject : ItemObject
{
    private Coroutine fuseCoroutine;

    public bool IsPlanted { get; private set; }

    public TimedBomb TimedBombAsset => Equipment as TimedBomb;

    public bool Plant(Transform container, Transform user)
    {
        TimedBomb bomb = TimedBombAsset;
        if (IsPlanted || bomb == null || container == null)
        {
            return false;
        }

        IsPlanted = true;
        fuseCoroutine = StartCoroutine(ExplodeAfterFuse(bomb, container, user));
        return true;
    }

    public bool CancelFuse()
    {
        if (!IsPlanted)
        {
            return false;
        }

        if (fuseCoroutine != null)
        {
            StopCoroutine(fuseCoroutine);
            fuseCoroutine = null;
        }

        IsPlanted = false;
        return true;
    }

    private IEnumerator ExplodeAfterFuse(TimedBomb bomb, Transform container, Transform user)
    {
        yield return new WaitForSeconds(bomb.FuseSeconds);

        fuseCoroutine = null;
        IsPlanted = false;

        Vector3 center = container.position;
        bomb.Detonate(user, container, this, center);

        GameManager.Instance?.EndGame();

        Destroy(gameObject, bomb.DespawnDelay);
    }
}
