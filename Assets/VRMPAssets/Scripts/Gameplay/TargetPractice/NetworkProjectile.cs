using System;
using System.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem.Utilities;
using UnityEngine.XR.Content.Interaction;

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

        [ServerRpc]
        public void SetVisibleServerRpc(bool value)
        {
            this.Visible = value;
        }

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
            m_Visible.OnValueChanged += OnVisibleChanged;

            ApplyColor(m_ProjectileColor.Value);
            gameObject.SetActive(m_Visible.Value);
        }

        public override void OnNetworkDespawn()
        {
            base.OnNetworkDespawn();
            m_ProjectileColor.OnValueChanged -= OnColorChanged;
            m_Visible.OnValueChanged -= OnVisibleChanged;
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

        private void OnVisibleChanged(bool previousValue, bool newValue)
        {
            if (newValue)
            {
                gameObject.SetActive(true);
            }
            else
            {
                if (m_TrailRenderer != null)
                {
                    m_TrailRenderer.Clear();
                }

                gameObject.SetActive(false);
            }
        }

        [Rpc(SendTo.Server)]
        public void SetupByServerRpc(Color playerColor)
        {
            this.Setup(playerColor);
        }

        [Rpc(SendTo.Server)]
        public void SetTransformByServerRpc(Vector3 position, Quaternion rotation)
        {
            transform.position = position;
            transform.rotation = rotation;
        }

        public void SetupAction(Action<NetworkProjectile> returnToPoolAction = null, Action<int, bool> hitTargetAction = null)
        {
            m_OnReturnToPool = returnToPoolAction;
            m_HitAction = hitTargetAction;
        }

        /// <summary>
        /// Setup the projectile with parameters. Call on server only.
        /// </summary>
        public void Setup(Color playerColor, Action<NetworkProjectile> returnToPoolAction = null, Action<int, bool> hitTargetAction = null)
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

        private void OnCollisionEnter(Collision collision)
        {
            if (collision.gameObject.CompareTag(m_ColliderTag))
            {
                if (IsClient)
                    HitTarget(collision);
            }
        }

        public void HitTarget(Collision collision)
        {
            if (collision.gameObject.CompareTag(m_ColliderTag))
            {
                if (collision.gameObject.TryGetComponent<BreakableTarget>(out var breakableTarget))
                {
                    var score = breakableTarget.pointValue;
                    m_HitAction?.Invoke(score, IsClient);
                    HitTargetServerRpc();
                }
            }
        }

        [ServerRpc]
        private void HitTargetServerRpc()
        {
            ResetProjectile();
        }

        [ServerRpc(RequireOwnership = false)]
        public void FireServerRpc(Vector3 force)
        {
            this.m_Rigidbody.isKinematic = false;
            this.m_Rigidbody.useGravity = true;
            this.m_Rigidbody.linearVelocity = Vector3.zero;
            this.m_Rigidbody.angularVelocity = Vector3.zero;
            this.m_Rigidbody.AddForce(force);
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