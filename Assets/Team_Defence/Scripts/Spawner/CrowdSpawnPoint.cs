using UnityEngine;

public class CrowdSpawnPoint : MonoBehaviour
{
    [SerializeField, Min(0f)] private float radius = 8f;

    public Vector3 GetRandomPosition()
    {
        Vector2 offset = Random.insideUnitCircle * radius;
        return transform.position + new Vector3(offset.x, 0f, offset.y);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, radius);
    }
}