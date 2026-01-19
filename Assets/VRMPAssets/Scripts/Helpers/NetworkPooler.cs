using NUnit.Framework;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.Services.Matchmaker.Models;
using UnityEngine;

namespace XRMultiplayer
{
    /// <summary>
    /// Network-synchronized object pooler using Unity Netcode.
    /// Objects are spawned as NetworkObjects and synchronized across all clients.
    /// </summary>
    public class NetworkPooler : NetworkBehaviour
    {
        [SerializeField, Tooltip("The prefab to spawn and use for pooling (must have NetworkObject component)")]
        private GameObject m_SpawnPrefab;

        [SerializeField, Tooltip("Initial pool size")]
        private int m_InitialPoolSize = 10;

        [SerializeField, Tooltip("Maximum pool size")]
        private int m_MaxPoolSize = 100;

        [SerializeField, Tooltip("Allow pool to grow beyond initial size")]
        private bool m_AllowPoolGrowth = true;

        [SerializeField]
        private List<GameObject> m_Pool = new List<GameObject>();

        private List<GameObject> m_ActiveObjects = new List<GameObject>();

        [SerializeField] private Transform spawnPosition;

        private bool m_IsInitialized = false;

        private void Awake()
        {
            ValidatePrefab();
        }

        private void ValidatePrefab()
        {
            if (m_SpawnPrefab == null)
            {
                Debug.LogError("NetworkPooler: Spawn prefab is not set.", this);
                enabled = false;
                return;
            }

            if (!m_SpawnPrefab.TryGetComponent<NetworkObject>(out _))
            {
                Debug.LogError("NetworkPooler: Spawn prefab must have a NetworkObject component.", this);
                enabled = false;
            }
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            if (IsServer)
            {
                InitializePool();
            }
            else
            {
                CollectExistingPooledObjects();
            }
        }

        /// <summary>
        /// 客户端收集已由服务器生成的对象
        /// </summary>
        private void CollectExistingPooledObjects()
        {
            m_Pool.Clear();
            m_ActiveObjects.Clear();

            foreach (Transform child in transform)
            {
                if (child.TryGetComponent<NetworkProjectile>(out var projectile))
                {
                    if (projectile.Visible)
                    {
                        m_ActiveObjects.Add(child.gameObject);
                    }
                    else
                    {
                        m_Pool.Add(child.gameObject);
                    }
                }
            }

            m_IsInitialized = true;
            Debug.Log($"[NetworkPooler] Client collected {m_Pool.Count} pooled + {m_ActiveObjects.Count} active objects.");
        }

        public override void OnNetworkDespawn()
        {
            base.OnNetworkDespawn();

            if (IsServer)
            {
                ClearPool();
            }
        }

        /// <summary>
        /// Initialize the object pool on the server.
        /// </summary>
        private void InitializePool()
        {
            if (m_IsInitialized) return;

            for (int i = 0; i < m_InitialPoolSize; i++)
            {
                GameObject obj = CreatePooledObject();
                if (obj != null)
                {
                    if (obj.TryGetComponent<NetworkProjectile>(out var networkProjectile))
                    {
                        networkProjectile.Visible = false;
                        m_Pool.Add(obj);
                    }
                }
            }

            m_IsInitialized = true;
            Debug.Log($"[NetworkPooler] Initialized pool with {m_Pool.Count} objects.");
        }

        /// <summary>
        /// Create a new pooled object and spawn it as a NetworkObject.
        /// </summary>
        private GameObject CreatePooledObject()
        {
            GameObject obj = Instantiate(m_SpawnPrefab, this.transform.position, Quaternion.identity);

            if (obj.TryGetComponent(out NetworkObject networkObject))
            {
                networkObject.Spawn();

                obj.transform.SetParent(this.transform);
                obj.transform.position = this.transform.position;

                // Deactivate directly (spawned objects are synced)
                obj.GetComponent<NetworkProjectile>().Visible = false;

                return obj;
            }

            Debug.LogError("NetworkPooler: Failed to spawn NetworkObject.", this);
            Destroy(obj);
            return null;
        }

        /// <summary>
        /// Get an object from the pool. Only call from server.
        /// </summary>
        public GameObject GetItem()
        {
            Debug.Log($"[NetworkPooler] GetItem called - IsServer: {IsServer}, IsSpawned: {IsSpawned}, IsInitialized: {m_IsInitialized}, PoolCount: {m_Pool.Count}");

            if (!m_IsInitialized && IsServer)
            {
                Debug.LogWarning("[NetworkPooler] Pool not initialized. Initializing now...");
                InitializePool();
            }

            //if (!IsServer)
            //{
            //    Debug.LogWarning($"[NetworkPooler] GetItem can only be called on the server. Current state - IsServer: {IsServer}, IsHost: {IsHost}");
            //    return null;
            //}

            GameObject obj = null;

            while (m_Pool.Count > 0)
            {
                obj = m_Pool[0];
                m_Pool.RemoveAt(0);
                if (obj != null)
                {
                    break;
                }
            }

            if (obj == null)
            {
                if (m_AllowPoolGrowth && m_ActiveObjects.Count < m_MaxPoolSize)
                {
                    obj = CreatePooledObject();
                    Debug.Log("[NetworkPooler] Pool expanded.");
                }
                else
                {
                    Debug.LogWarning("[NetworkPooler] Pool exhausted and growth is disabled or max size reached.");
                    return null;
                }
            }

            if (obj != null)
            {
                if (obj.TryGetComponent<NetworkProjectile>(out var networkProjectile))
                {
                    m_ActiveObjects.Add(obj);

                    return obj;
                }
            }

            return obj;
        }

        /// <summary>
        /// Return an object to the pool. Only call from server.
        /// </summary>
        public void ReturnItem(GameObject item)
        {
            if (!IsServer)
            {
                Debug.LogWarning("NetworkPooler: ReturnItem can only be called on the server.");
                return;
            }

            if (item == null) return;

            if (m_ActiveObjects.Contains(item))
            {
                m_ActiveObjects.Remove(item);
                m_Pool.Add(item);

                item.SetActive(false);
                item.transform.position = this.spawnPosition.position;
                item.transform.rotation = Quaternion.identity;

                if (item.TryGetComponent(out Rigidbody rb))
                {
                    rb.linearVelocity = Vector3.zero;
                    rb.angularVelocity = Vector3.zero;
                }

                if (item.TryGetComponent(out NetworkObject networkObject))
                {
                    SetObjectActiveClientRpc(networkObject.NetworkObjectId, false);
                }
            }
        }

        /// <summary>
        /// Sync object active state to all clients.
        /// </summary>
        [ClientRpc]
        private void SetObjectActiveClientRpc(ulong networkObjectId, bool isActive)
        {
            if (IsServer) return;

            if (NetworkManager.Singleton != null &&
                NetworkManager.Singleton.SpawnManager != null &&
                NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(networkObjectId, out NetworkObject networkObject))
            {
                networkObject.gameObject.SetActive(isActive);
            }
        }

        /// <summary>
        /// Clear all pooled objects.
        /// </summary>
        private void ClearPool()
        {
            foreach (var obj in m_ActiveObjects)
            {
                if (obj != null && obj.TryGetComponent(out NetworkObject networkObject))
                {
                    if (networkObject.IsSpawned)
                    {
                        networkObject.Despawn();
                    }
                    Destroy(obj);
                }
            }
            m_ActiveObjects.Clear();

            foreach (var obj in m_Pool)
            {
                if (obj.TryGetComponent(out NetworkObject networkObject))
                {
                    if (networkObject.IsSpawned)
                    {
                        networkObject.Despawn();
                    }
                    Destroy(obj);
                }
            }
            m_Pool.Clear();
            m_IsInitialized = false;
        }
    }
}