using Unity.Netcode;
using XRMultiplayer;

namespace UnityEngine.XR.Content.Interaction
{
    /// <summary>
    /// Detects a collision with a tagged collider, replacing this object with a 'broken' version
    /// </summary>
    public class BreakableTarget : NetworkBehaviour
    {
        public int pointValue = 1;

        [SerializeField]
        [Tooltip("The 'broken' version of this object.")]
        private GameObject m_BrokenVersion;

        [SerializeField]
        [Tooltip("The tag a collider must have to cause this object to break.")]
        private string m_ColliderTag = "Destroyer";

        private bool m_Destroyed = false;
        public CustomEvent onBreak;

        [SerializeField] private bool isDisplayObject = false;

        [SerializeField] private float dispearAfter = 5f;

        private void Start()
        {
            if (!isDisplayObject)
                Invoke(nameof(RequestDestroyServerRpc), dispearAfter + Random.Range(0f, 10f));
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (m_Destroyed)
                return;

            if (collision.gameObject.CompareTag(m_ColliderTag))
            {
                Break(collision);
            }
        }

        public void Break(Collision collision)
        {
            if (m_Destroyed) return;
            m_Destroyed = true;

            collision.gameObject.TryGetComponent<Projectile>(out Projectile projectile);
            if (projectile != null && projectile.isLocalPlayerProjectile)
            {
                projectile.HitTarget(pointValue, true);
            }

            var brokenObject = Instantiate(m_BrokenVersion, transform.position, transform.rotation);
            brokenObject.transform.localScale = transform.localScale;

            onBreak?.Invoke();

            if (!isDisplayObject)
            {
                RequestDestroyServerRpc();
            }
            else
            {
                m_Destroyed = false;
                gameObject.SetActive(false);
            }
        }

        [Rpc(SendTo.Server)]
        private void RequestDestroyServerRpc()
        {
            if (TryGetComponent<NetworkObject>(out var networkObject))
            {
                networkObject.Despawn();
            }
            else
            {
                Destroy(gameObject);
            }
        }
    }
}