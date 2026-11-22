using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Reusable area definition (circle or box on the XZ plane). Reference it from other scripts to get random points inside the area.
/// </summary>
public class AreaZone : MonoBehaviour
{
    public enum ZoneShape
    {
        Circle,
        Box
    }

    [SerializeField] private ZoneShape shape = ZoneShape.Circle;
    [SerializeField, Min(0f)] private float radius = 3f;
    [SerializeField] private Vector2 boxSize = new Vector2(6f, 6f);

    [Header("NavMesh")]
    [SerializeField, Min(0f)] private float maxSampleDistance = 1.5f;
    [SerializeField, Min(1)] private int sampleAttempts = 8;

    public Vector3 Center => transform.position;

    public Vector3 GetRandomPoint()
    {
        if (shape == ZoneShape.Circle)
        {
            Vector2 offset = Random.insideUnitCircle * radius;
            return transform.position + new Vector3(offset.x, 0f, offset.y);
        }

        Vector3 local = new Vector3(
            Random.Range(-boxSize.x * 0.5f, boxSize.x * 0.5f),
            0f,
            Random.Range(-boxSize.y * 0.5f, boxSize.y * 0.5f));
        return transform.TransformPoint(local);
    }

    public bool TryGetRandomNavMeshPoint(out Vector3 point)
    {
        for (int i = 0; i < sampleAttempts; i++)
        {
            if (NavMesh.SamplePosition(GetRandomPoint(), out NavMeshHit hit, maxSampleDistance, NavMesh.AllAreas))
            {
                point = hit.position;
                return true;
            }
        }

        point = Center;
        return false;
    }

    public bool Contains(Vector3 worldPosition)
    {
        if (shape == ZoneShape.Circle)
        {
            Vector3 delta = worldPosition - transform.position;
            delta.y = 0f;
            return delta.sqrMagnitude <= radius * radius;
        }

        Vector3 local = transform.InverseTransformPoint(worldPosition);
        return Mathf.Abs(local.x) <= boxSize.x * 0.5f && Mathf.Abs(local.z) <= boxSize.y * 0.5f;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        if (shape == ZoneShape.Circle)
        {
            Gizmos.DrawWireSphere(transform.position, radius);
            return;
        }

        Matrix4x4 previous = Gizmos.matrix;
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.DrawWireCube(Vector3.zero, new Vector3(boxSize.x, 0.1f, boxSize.y));
        Gizmos.matrix = previous;
    }
}
