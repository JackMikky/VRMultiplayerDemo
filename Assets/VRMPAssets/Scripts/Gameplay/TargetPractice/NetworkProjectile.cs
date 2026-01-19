using System;
using System.Collections;
using Unity.Netcode;
using UnityEngine;

namespace XRMultiplayer
{
    /// <summary>
    /// Network-synchronized projectile that syncs position and hit events across clients.
    /// </summary>
    public class NetworkProjectile : NetworkBehaviour
    {
        [SerializeField] protected TrailRenderer m_TrailRenderer;
        [SerializeField] protected float m_Lifetime = 10.0f;
        [SerializeField] private string m_ColliderTag = "Piggy";

        private Vector3 m_PrevPos = Vector3.zero;
        private RaycastHit m_Hit;
        private bool m_HasHitTarget = false;

        private NetworkVariable<Color> m_ProjectileColor = new NetworkVariable<Color>(
            Color.white,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Owner
        );

        private NetworkVariable<bool> m_Visible = new NetworkVariable<bool>(
            true,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

        public bool Visible { get => m_Visible.Value; set => m_Visible.Value = value; }

        private Action<NetworkProjectile> m_OnReturnToPool;
        private Action<int, bool> m_HitAction;
        private Rigidbody m_Rigidbody;

        /// <summary>
        /// Gets whether this projectile belongs to the local player (owner).
        /// </summary>
        public bool IsLocalPlayerProjectile => IsOwner;

        private void Awake()
        {
            TryGetComponent(out m_Rigidbody);
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            m_ProjectileColor.OnValueChanged += OnColorChanged;
            m_Visible.OnValueChanged += OnActiveStateChanged;

            // Apply initial color
            ApplyColor(m_ProjectileColor.Value);
        }

        public override void OnNetworkDespawn()
        {
            base.OnNetworkDespawn();
            m_ProjectileColor.OnValueChanged -= OnColorChanged;
            m_Visible.OnValueChanged -= OnActiveStateChanged;
        }

        private void OnColorChanged(Color previousValue, Color newValue)
        {
            ApplyColor(newValue);
        }

        private void ApplyColor(Color color)
        {
            if (m_TrailRenderer != null)
            {
                m_TrailRenderer.startColor = color;
                m_TrailRenderer.endColor = color;
            }
        }

        private void OnActiveStateChanged(bool previousValue, bool newValue)
        {
            gameObject.SetActive(newValue);
            if (!newValue && m_TrailRenderer != null)
            {
                m_TrailRenderer.Clear();
            }
        }

        /// <summary>
        /// Setup the projectile with parameters. Call on server only.
        /// </summary>
        public void Setup(bool localPlayer, Color playerColor, Action<NetworkProjectile> returnToPoolAction = null, Action<int, bool> hitTargetAction = null)
        {
            if (!IsServer)
            {
                Debug.LogWarning("NetworkProjectile: Setup should only be called on the server.");
                return;
            }

            m_HasHitTarget = false;
            m_OnReturnToPool = returnToPoolAction;
            m_HitAction = hitTargetAction;

            // Sync color to all clients
            SetColorClientRpc(playerColor);

            m_PrevPos = transform.position;
            m_TrailRenderer?.Clear();

            // Activate the projectile
            m_Visible.Value = true;

            // Start lifetime coroutine
            StartCoroutine(ResetProjectileAfterTime());
        }

        [Rpc(SendTo.Everyone)]
        private void SetColorClientRpc(Color color)
        {
            ApplyColor(color);
        }

        private IEnumerator ResetProjectileAfterTime()
        {
            yield return new WaitForSeconds(m_Lifetime);
            ResetProjectile();
        }

        private void FixedUpdate()
        {
            // Only server handles hit detection
            if (!IsServer || m_HasHitTarget) return;

            if (Physics.Linecast(m_PrevPos, transform.position, out m_Hit))
            {
                if (m_Hit.transform.CompareTag("Target"))
                {
                    HitTarget(m_Hit.transform.GetComponentInParent<Target>());
                }

                CheckForInteractableHit(m_Hit.transform);
            }

            m_PrevPos = transform.position;
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (!IsServer) return;
            CheckForInteractableHit(collision.transform);
        }

        private void CheckForInteractableHit(Transform t)
        {
            NetworkPhysicsInteractable networkPhysicsInteractable = t.GetComponentInParent<NetworkPhysicsInteractable>();
            if (networkPhysicsInteractable != null)
            {
                networkPhysicsInteractable.RequestOwnership();
            }
        }

        /// <summary>
        /// Called when the projectile hits a target.
        /// </summary>
        protected virtual void HitTarget(Target target)
        {
            if (target != null)
            {
                target.TargetHitLocal();
            }
            m_HasHitTarget = true;
        }

        /// <summary>
        /// Called when a target is hit with score.
        /// </summary>
        public void HitTarget(int score, bool isLocalPlayer)
        {
            if (IsServer)
            {
                HitTargetClientRpc(score, isLocalPlayer);
            }
        }

        [Rpc(SendTo.Everyone)]
        private void HitTargetClientRpc(int score, bool isLocalPlayer)
        {
            m_HitAction?.Invoke(score, isLocalPlayer);
            if (IsServer)
            {
                ResetProjectile();
            }
        }

        [ClientRpc(RequireOwnership = false)]
        public void ResetProjectileClientRpc()
        {
            ResetProjectile();
        }

        /// <summary>
        /// Reset and return the projectile to the pool.
        /// </summary>
        public void ResetProjectile()
        {
            if (!IsServer) return;

            StopAllCoroutines();
            m_HasHitTarget = false;

            // Reset rigidbody
            if (m_Rigidbody != null)
            {
                m_Rigidbody.isKinematic = true;
                m_Rigidbody.isKinematic = false;
                m_Rigidbody.linearVelocity = Vector3.zero;
                m_Rigidbody.angularVelocity = Vector3.zero;
            }

            // Deactivate via NetworkVariable
            m_Visible.Value = false;

            m_OnReturnToPool?.Invoke(this);
        }
    }
}