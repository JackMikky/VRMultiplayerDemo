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

        [SerializeField]
        [Tooltip("Lifetime of the broken version before it's destroyed")]
        private float m_BrokenVersionLifetime = 3f;

        private bool m_Destroyed = false;
        public CustomEvent onBreak;

        [SerializeField] private bool isDisplayObject = false;

        [SerializeField] private float dispearAfter = 5f;

        [SerializeField]
        private NetworkVariable<bool> isPoolObject = new NetworkVariable<bool>(
            false,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server);

        [SerializeField]
        private NetworkVariable<bool> isVisible = new NetworkVariable<bool>(
            false,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server);

        public bool IsPoolObject
        {
            get => isPoolObject.Value;
            set
            {
                if (IsServer)
                {
                    isPoolObject.Value = value;
                }
            }
        }

        public bool IsVisible
        {
            get => isVisible.Value;
            set
            {
                if (IsServer)
                {
                    isVisible.Value = value;
                }
            }
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            isVisible.OnValueChanged += OnVisibilityChanged;
            if (IsServer)
            {
                if (isDisplayObject)
                {
                    isVisible.Value = true;
                }
                OnVisibilityChanged(false, isVisible.Value);
            }
            gameObject.SetActive(isVisible.Value);
        }

        public override void OnNetworkDespawn()
        {
            base.OnNetworkDespawn();

            isVisible.OnValueChanged -= OnVisibilityChanged;
        }

        private void OnVisibilityChanged(bool previousValue, bool newValue)
        {
            gameObject.SetActive(newValue);

            if (newValue)
            {
                m_Destroyed = false;
            }
        }

        private void Start()
        {
            if (!isDisplayObject && !isPoolObject.Value && IsServer)
                Invoke(nameof(RequestDestroyServerRpc), dispearAfter + Random.Range(0f, 10f));
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (m_Destroyed)
                return;

            if (collision.gameObject.CompareTag(m_ColliderTag))
            {
                // Call server RPC to break the target
                BreakServerRpc();
                collision.gameObject.TryGetComponent<Projectile>(out Projectile projectile);
                if (projectile != null && projectile.isLocalPlayerProjectile)
                {
                    projectile.HitTarget(pointValue, true);
                }
            }
        }

        /// <summary>
        /// Server RPC to handle breaking the target
        /// </summary>
        [Rpc(SendTo.Server)]
        private void BreakServerRpc()
        {
            if (m_Destroyed) return;

            // Execute break on server and propagate to all clients
            BreakClientRpc();
        }

        /// <summary>
        /// Client RPC to break the target on all clients
        /// </summary>
        [Rpc(SendTo.Everyone)]
        private void BreakClientRpc()
        {
            if (m_Destroyed) return;

            ExecuteBreak();
        }

        /// <summary>
        /// Execute the break logic (called on all clients)
        /// </summary>
        private void ExecuteBreak()
        {
            m_Destroyed = true;

            // Create broken version (visual effect only)
            if (m_BrokenVersion != null)
            {
                var brokenObject = Instantiate(m_BrokenVersion, transform.position, transform.rotation);
                brokenObject.transform.localScale = transform.localScale;

                // Destroy broken version after a delay
                Destroy(brokenObject, m_BrokenVersionLifetime);
            }

            // Invoke onBreak event (this will trigger ReturnToPool on server)
            if (IsServer)
            {
                onBreak?.Invoke();
            }

            // Handle object lifecycle
            if (!isDisplayObject)
            {
                if (isPoolObject.Value)
                {
                    // Pool object: hide via NetworkVariable (synced to all clients)
                    if (IsServer)
                    {
                        isVisible.Value = false;
                    }
                    // onBreak event will trigger ReturnToPool on server
                }
                else
                {
                    // Non-pool object: destroy normally
                    if (IsServer)
                    {
                        RequestDestroyServerRpc();
                    }
                }
            }
            else
            {
                m_Destroyed = false;
                if (IsServer)
                {
                    isVisible.Value = false;
                }
            }
        }

        /// <summary>
        /// Legacy Break method (for backward compatibility)
        /// </summary>
        public void Break(Collision collision)
        {
            if (m_Destroyed) return;

            // Get projectile for score calculation (server only)
            if (IsServer)
            {
                collision.gameObject.TryGetComponent<Projectile>(out Projectile projectile);
                if (projectile != null && projectile.isLocalPlayerProjectile)
                {
                    projectile.HitTarget(pointValue, true);
                }
            }

            // Call server RPC to synchronize break across network
            BreakServerRpc();
        }

        [Rpc(SendTo.Server)]
        private void RequestDestroyServerRpc()
        {
            if (TryGetComponent<NetworkObject>(out var networkObject))
            {
                if (networkObject.IsSpawned)
                {
                    networkObject.Despawn();
                }
            }
            else
            {
                Destroy(gameObject);
            }
        }

        /// <summary>
        /// Reset target state (called when retrieved from pool)
        /// </summary>
        public void ResetTarget()
        {
            m_Destroyed = false;

            // Show via NetworkVariable (server only)
            if (IsServer)
            {
                isVisible.Value = true;
            }

            // Cancel any pending invokes
            CancelInvoke();
        }

        private void OnDestroy()
        {
            // Clean up any pending invokes
            CancelInvoke();
            onBreak?.RemoveAllListeners();
        }
    }
}