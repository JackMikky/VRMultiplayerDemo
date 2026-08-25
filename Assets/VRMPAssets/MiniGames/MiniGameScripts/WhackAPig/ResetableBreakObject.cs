using System.Collections;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
///Upon contact with a collider bearing the specified tag, the object is swapped for a broken version;
///after a set period, the fragments rewind to their original positions (as if time were flowing backward) to restore the object to its original state.
///A standalone component that operates independently, with no dependencies on the existing Breakable or NetworkedWhackAPig scripts.
/// </summary>
public class ResetableBreakObject : MonoBehaviour
{
    [Header("Break Settings")]
    [SerializeField]
    [Tooltip("The collider used for destruction detection. If not set, it is retrieved from the object itself.")]
    private Collider m_Collider;

    [SerializeField]
    [Tooltip("A broken version of the prefab (with the fragments as child objects).")]
    private GameObject m_BrokenVersion;

    [SerializeField]
    [Tooltip("It is destroyed when hit by a collider with this tag.")]
    private string m_ColliderTag = "Destroyer";

    [Header("Rewind Settings")]
    [SerializeField]
    [Tooltip("Duration (in seconds) during which fragments are scattering.")]
    private float m_ScatterTime = 2.0f;

    [SerializeField]
    [Tooltip("Duration (in seconds) during which fragments are rewinding.")]
    private float m_RewindTime = 1.0f;

    [SerializeField]
    [Tooltip("The waiting time (in seconds) from when rewinding completes and the object returns to its original appearance until it can be destroyed again.")]
    private float m_RespawnDelay = 0.25f;

    public UnityAction<Collider> onBreak;
    public UnityAction onRestored;

    private Renderer[] m_Renderers;
    private bool m_Destroyed;

    private void Awake()
    {
        if (m_Collider == null)
            TryGetComponent(out m_Collider);

        m_Renderers = GetComponentsInChildren<Renderer>(true);
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (m_Destroyed)
            return;

        if (collision.gameObject.CompareTag(m_ColliderTag))
            Break(collision.collider);
    }

    /// <summary>
    /// Initiate the destruction effect. The object is not discarded but hidden, and reappears after the rewind.
    /// </summary>
    public void Break(Collider breakCollider)
    {
        if (m_Destroyed)
            return;

        m_Destroyed = true;
        SetVisible(false);
        onBreak?.Invoke(breakCollider);
        StartCoroutine(BreakAndRestoreRoutine());
    }

    private IEnumerator BreakAndRestoreRoutine()
    {
        GameObject brokenObject = null;
        Transform[] shards = null;
        Vector3[] startPositions = null;
        Quaternion[] startRotations = null;
        Rigidbody[] bodies = null;

        if (m_BrokenVersion != null)
        {
            brokenObject = Instantiate(m_BrokenVersion, transform.position, transform.rotation);
            brokenObject.transform.localScale = transform.localScale;

            var brokenTransform = brokenObject.transform;
            var count = brokenTransform.childCount;
            shards = new Transform[count];
            startPositions = new Vector3[count];
            startRotations = new Quaternion[count];
            bodies = new Rigidbody[count];

            for (var i = 0; i < count; i++)
            {
                var shard = brokenTransform.GetChild(i);
                shards[i] = shard;
                startPositions[i] = shard.localPosition;
                startRotations[i] = shard.localRotation;
                shard.TryGetComponent(out bodies[i]);
            }
        }

        yield return new WaitForSeconds(m_ScatterTime);

        if (brokenObject != null)
        {
            for (var i = 0; i < shards.Length; i++)
            {
                if (bodies[i] != null)
                {
                    bodies[i].linearVelocity = Vector3.zero;
                    bodies[i].angularVelocity = Vector3.zero;
                    bodies[i].isKinematic = true;
                }

                if (shards[i].TryGetComponent(out Collider shardCollider))
                    shardCollider.enabled = false;
            }

            var fromPositions = new Vector3[shards.Length];
            var fromRotations = new Quaternion[shards.Length];
            for (var i = 0; i < shards.Length; i++)
            {
                fromPositions[i] = shards[i].localPosition;
                fromRotations[i] = shards[i].localRotation;
            }

            for (var elapsed = 0f; elapsed < m_RewindTime; elapsed += Time.deltaTime)
            {
                var percent = Mathf.SmoothStep(0f, 1f, elapsed / m_RewindTime);
                for (var i = 0; i < shards.Length; i++)
                {
                    shards[i].localPosition = Vector3.Lerp(fromPositions[i], startPositions[i], percent);
                    shards[i].localRotation = Quaternion.Slerp(fromRotations[i], startRotations[i], percent);
                }
                yield return null;
            }

            for (var i = 0; i < shards.Length; i++)
            {
                shards[i].localPosition = startPositions[i];
                shards[i].localRotation = startRotations[i];
            }

            Destroy(brokenObject);
        }

        SetVisible(true);
        onRestored?.Invoke();

        if (m_RespawnDelay > 0f)
            yield return new WaitForSeconds(m_RespawnDelay);

        m_Destroyed = false;
    }

    private void SetVisible(bool visible)
    {
        if (m_Collider != null)
            m_Collider.enabled = visible;

        if (m_Renderers == null)
            return;

        foreach (var meshRenderer in m_Renderers)
        {
            if (meshRenderer != null)
                meshRenderer.enabled = visible;
        }
    }
}