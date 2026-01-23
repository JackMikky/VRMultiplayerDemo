using Unity.Netcode;
using UnityEngine;
using System.Collections.Generic;
using UnityEngine.AI;
using UnityEngine.XR.Content.Interaction;

namespace XRMultiplayer.MiniGames
{
    public enum ColliderType
    {
        Box = 0,
        Sphere = 1
    }

    /// <summary>
    /// Network-enabled item spawner with object pooling
    /// </summary>
    public class ItemSpawner : NetworkBehaviour
    {
        [SerializeField]
        [Tooltip("Item prefabs to spawn")]
        private List<GameObject> m_ItemPrefabs;

        [SerializeField]
        [Tooltip("Collider type")]
        private ColliderType m_ColliderType = ColliderType.Box;

        [SerializeField]
        [Tooltip("Spawn area size")]
        private Vector3 m_SpawnAreaSize = new Vector3(5f, 2f, 5f);

        [SerializeField]
        [Tooltip("Spawn sphere radius")]
        private float m_SpawnSphereRadius = 2.5f;

        [SerializeField]
        [Tooltip("Item spawn position")]
        private Transform m_SpawnTransform;

        [SerializeField]
        [Tooltip("Spawn interval (seconds)")]
        private float m_SpawnInterval = 2f;

        [SerializeField]
        [Tooltip("Maximum spawn count")]
        private int m_MaxSpawnCount = 10;

        [SerializeField]
        [Tooltip("Spawn at random positions")]
        private bool m_RandomSpawnPosition = false;

        [SerializeField]
        [Tooltip("Validate spawn position using NavMesh")]
        private bool UseNavMesh = false;

        [SerializeField]
        [Tooltip("Maximum distance for NavMesh search")]
        private float m_NavMeshSearchDistance = 10f;

        [SerializeField]
        [Tooltip("Horizontal tolerance for directly below check (meters)")]
        private float m_HorizontalTolerance = 1f;

        [SerializeField]
        [Tooltip("Maximum retry attempts when NavMesh validation fails")]
        private int m_MaxRetryAttempts = 5;

        [Header("Object Pool Settings")]
        [SerializeField]
        [Tooltip("Enable object pooling")]
        private bool m_UseObjectPool = true;

        [SerializeField]
        [Tooltip("Initial pool size per prefab")]
        private int m_InitialPoolSize = 5;

        [SerializeField]
        [Tooltip("Allow pool to grow beyond initial size")]
        private bool m_AllowPoolGrowth = true;

        [Header("Gizmo Settings")]
        [SerializeField]
        private GizmoSettings m_GizmoSettings = new GizmoSettings
        {
            showSpawnArea = true,
            spawnAreaColor = new Color(0f, 1f, 1f, 0.3f),
            showSpawnPoint = true,
            spawnPointColor = Color.yellow,
            showAxis = true,
            axisLength = 0.5f
        };

        [SerializeField]
        [HideInInspector]
        public Collider visualizeCollider;

        private float m_SpawnTimer = 0f;
        private int m_CurrentSpawnCount = 0;

        // Object pool management
        private Dictionary<GameObject, Queue<GameObject>> m_ObjectPools = new Dictionary<GameObject, Queue<GameObject>>();

        private Dictionary<GameObject, GameObject> m_ActiveObjects = new Dictionary<GameObject, GameObject>();
        private Transform m_PoolContainer;

        // List to track spawned instances
        private List<GameObject> m_SpawnedInstances = new List<GameObject>();

        public bool readyForSpawn = false;

        public ColliderType VisualColliderType
        {
            get => m_ColliderType;
            set => m_ColliderType = value;
        }

        /// <summary>
        /// Spawn position
        /// </summary>
        public Transform spawnTransform
        {
            get
            {
                if (m_SpawnTransform == null)
                    return transform;
                return m_SpawnTransform;
            }
        }

        /// <summary>
        /// Get the list of spawned instances (read-only)
        /// </summary>
        public IReadOnlyList<GameObject> SpawnedInstances => m_SpawnedInstances.AsReadOnly();

        private void Awake()
        {
            if (m_ItemPrefabs == null || m_ItemPrefabs.Count == 0)
            {
                Debug.LogError("ItemSpawner: No item prefabs are set.", this);
                enabled = false;
                return;
            }

            foreach (var prefab in m_ItemPrefabs)
            {
                if (prefab == null)
                {
                    Debug.LogError("ItemSpawner: The list contains null prefabs.", this);
                    enabled = false;
                    return;
                }

                if (!prefab.TryGetComponent<NetworkObject>(out _))
                {
                    Debug.LogError("ItemSpawner: Prefab requires a NetworkObject component.", this);
                    enabled = false;
                    return;
                }
            }

            // Create pool container
            if (m_UseObjectPool)
            {
                m_PoolContainer = new GameObject("ObjectPool").transform;
                m_PoolContainer.SetParent(transform);
                m_PoolContainer.localPosition = Vector3.zero;
            }

#if UNITY_EDITOR
            // Initialize visualization collider only in Editor
            InitializeSpawnAreaCollider();
#endif
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            // Initialize object pools on server
            if (IsServer && m_UseObjectPool)
            {
                InitializeObjectPools();
            }
        }

        public override void OnNetworkDespawn()
        {
            base.OnNetworkDespawn();

            // Clean up pools
            if (IsServer && m_UseObjectPool)
            {
                ClearObjectPools();
            }
        }

        /// <summary>
        /// Initialize object pools for all prefabs
        /// </summary>
        private void InitializeObjectPools()
        {
            foreach (var prefab in m_ItemPrefabs)
            {
                if (!m_ObjectPools.ContainsKey(prefab))
                {
                    Queue<GameObject> pool = new Queue<GameObject>();

                    for (int i = 0; i < m_InitialPoolSize; i++)
                    {
                        GameObject obj = CreatePooledObject(prefab);
                        pool.Enqueue(obj);
                    }

                    m_ObjectPools[prefab] = pool;
                    Debug.Log($"ItemSpawner: Initialized pool for {prefab.name} with {m_InitialPoolSize} objects.");
                }
            }
        }

        /// <summary>
        /// Create a pooled object
        /// </summary>
        private GameObject CreatePooledObject(GameObject prefab)
        {
            // Don't set parent during instantiation
            GameObject obj = Instantiate(prefab);
            obj.transform.position = m_PoolContainer.position;
            obj.transform.rotation = Quaternion.identity;

            // Spawn NetworkObject immediately FIRST
            if (obj.TryGetComponent(out NetworkObject networkObject))
            {
                networkObject.Spawn();
            }

            // Configure BreakableTarget AFTER spawning (so IsServer is true)
            if (obj.TryGetComponent<BreakableTarget>(out var breakableTarget))
            {
                breakableTarget.IsPoolObject = true;

                // Register ReturnToPool event to onBreak
                breakableTarget.onBreak.AddListener(() =>
                {
                    ReturnToPool(obj);
                });

                // Hide via NetworkVariable (will be synced to all clients)
                breakableTarget.IsVisible = false;
            }

            return obj;
        }

        /// <summary>
        /// Get object from pool or create new one
        /// </summary>
        private GameObject GetFromPool(GameObject prefab)
        {
            if (!m_ObjectPools.ContainsKey(prefab))
            {
                m_ObjectPools[prefab] = new Queue<GameObject>();
            }

            Queue<GameObject> pool = m_ObjectPools[prefab];

            GameObject obj = null;

            // Try to get from pool
            while (pool.Count > 0)
            {
                obj = pool.Dequeue();
                if (obj != null)
                {
                    break;
                }
            }

            // Create new object if pool is empty and growth is allowed
            if (obj == null)
            {
                if (m_AllowPoolGrowth)
                {
                    obj = CreatePooledObject(prefab);
                    Debug.Log($"ItemSpawner: Pool for {prefab.name} expanded.");
                }
                else
                {
                    Debug.LogWarning($"ItemSpawner: Pool for {prefab.name} is exhausted and growth is disabled.");
                    return null;
                }
            }

            return obj;
        }

        /// <summary>
        /// Return object to pool
        /// </summary>
        public void ReturnToPool(GameObject obj)
        {
            if (!m_UseObjectPool)
            {
                // If not using pooling, just destroy
                if (obj.TryGetComponent(out NetworkObject networkObject))
                {
                    if (networkObject.IsSpawned)
                    {
                        networkObject.Despawn();
                    }
                }
                Destroy(obj);
                return;
            }

            if (!IsServer)
            {
                Debug.LogWarning("ItemSpawner: ReturnToPool can only be called on the server.");
                return;
            }

            // Remove from active objects tracking
            if (m_ActiveObjects.ContainsKey(obj))
            {
                GameObject prefab = m_ActiveObjects[obj];
                m_ActiveObjects.Remove(obj);

                // Hide via NetworkVariable
                if (obj.TryGetComponent<BreakableTarget>(out var breakableTarget))
                {
                    breakableTarget.IsVisible = false;
                }

                // Move to pool position
                obj.transform.position = m_PoolContainer.position;
                obj.transform.rotation = Quaternion.identity;

                if (m_ObjectPools.ContainsKey(prefab))
                {
                    m_ObjectPools[prefab].Enqueue(obj);
                }

                // Decrease spawn count
                m_CurrentSpawnCount--;

                Debug.Log($"ItemSpawner: Returned {obj.name} to pool. Current spawn count: {m_CurrentSpawnCount}");
            }
        }

        /// <summary>
        /// Deactivate object on all clients
        /// </summary>
        [Rpc(SendTo.Everyone)]
        private void DeactivateObjectClientRpc(ulong networkObjectId)
        {
            if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(networkObjectId, out NetworkObject networkObject))
            {
                networkObject.gameObject.SetActive(false);
            }
        }

        /// <summary>
        /// Activate object on all clients
        /// </summary>
        [Rpc(SendTo.Everyone)]
        private void ActivateObjectClientRpc(ulong networkObjectId, Vector3 position, Quaternion rotation)
        {
            if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(networkObjectId, out NetworkObject networkObject))
            {
                networkObject.transform.position = position;
                networkObject.transform.rotation = rotation;
                networkObject.gameObject.SetActive(true);

                // Reset BreakableTarget if present
                if (networkObject.TryGetComponent<BreakableTarget>(out var breakableTarget))
                {
                    breakableTarget.ResetTarget();
                }
            }
        }

        /// <summary>
        /// Clear all object pools
        /// </summary>
        private void ClearObjectPools()
        {
            // Return all active objects to pool first
            List<GameObject> activeObjects = new List<GameObject>(m_ActiveObjects.Keys);
            foreach (var obj in activeObjects)
            {
                ReturnToPool(obj);
                var breakableTarget = obj.GetComponent<BreakableTarget>();
                breakableTarget?.ResetTarget();
                breakableTarget?.onBreak.RemoveAllListeners();
            }

            // Destroy pooled objects
            foreach (var pool in m_ObjectPools.Values)
            {
                while (pool.Count > 0)
                {
                    GameObject obj = pool.Dequeue();
                    if (obj != null)
                    {
                        if (obj.TryGetComponent<NetworkObject>(out var networkObject))
                        {
                            if (networkObject.IsSpawned)
                            {
                                networkObject.Despawn();
                            }
                        }
                        Destroy(obj);
                    }
                }
            }

            m_ObjectPools.Clear();
            m_ActiveObjects.Clear();
        }

#if UNITY_EDITOR

        /// <summary>
        /// Initialize spawn area collider (Editor only)
        /// </summary>
        private void InitializeSpawnAreaCollider()
        {
            UpdateColliderType();
        }

        /// <summary>
        /// Update collider according to collider type (Editor only)
        /// </summary>
        public void UpdateColliderType()
        {
            // Remove existing collider
            if (visualizeCollider != null)
            {
                if (!Application.isPlaying)
                    DestroyImmediate(visualizeCollider);
                else
                    Destroy(visualizeCollider);
            }

            // Create new collider
            switch (m_ColliderType)
            {
                case ColliderType.Box:
                    BoxCollider boxCollider = gameObject.AddComponent<BoxCollider>();
                    boxCollider.size = m_SpawnAreaSize;
                    boxCollider.center = this.transform.localPosition;
                    boxCollider.isTrigger = true;
                    visualizeCollider = boxCollider;
                    break;

                case ColliderType.Sphere:
                    SphereCollider sphereCollider = gameObject.AddComponent<SphereCollider>();
                    sphereCollider.radius = m_SpawnSphereRadius;
                    sphereCollider.center = this.transform.localPosition;
                    sphereCollider.isTrigger = true;
                    visualizeCollider = sphereCollider;
                    break;
            }
        }

#endif

        /// <summary>
        /// Get a random position within the spawn area
        /// </summary>
        /// <returns>Random position in world coordinates</returns>
        private Vector3 GetRandomPositionInSpawnArea()
        {
            Vector3 localRandomPoint = Vector3.zero;

            if (m_ColliderType == ColliderType.Box)
            {
                // Calculate random position inside box
                localRandomPoint = this.transform.localPosition + new Vector3(
                    Random.Range(-m_SpawnAreaSize.x * 0.5f, m_SpawnAreaSize.x * 0.5f),
                    Random.Range(-m_SpawnAreaSize.y * 0.5f, m_SpawnAreaSize.y * 0.5f),
                    Random.Range(-m_SpawnAreaSize.z * 0.5f, m_SpawnAreaSize.z * 0.5f)
                );
            }
            else if (m_ColliderType == ColliderType.Sphere)
            {
                // Calculate random position inside sphere
                Vector3 randomInsideSphere = Random.insideUnitSphere * m_SpawnSphereRadius;
                localRandomPoint = this.transform.localPosition + randomInsideSphere;
            }

            // Convert from local to world coordinates
            return transform.TransformPoint(localRandomPoint);
        }

        /// <summary>
        /// Set spawn area size
        /// </summary>
        /// <param name="size">New size</param>
        public void SetSpawnAreaSize(Vector3 size)
        {
            m_SpawnAreaSize = size;
#if UNITY_EDITOR
            // Update collider in Editor
            if (visualizeCollider is BoxCollider boxCollider)
            {
                boxCollider.size = size;
            }
#endif
        }

        /// <summary>
        /// Set spawn sphere radius
        /// </summary>
        /// <param name="radius">New radius</param>
        public void SetSpawnSphereRadius(float radius)
        {
            m_SpawnSphereRadius = radius;
#if UNITY_EDITOR
            // Update collider in Editor
            if (visualizeCollider is SphereCollider sphereCollider)
            {
                sphereCollider.radius = radius;
            }
#endif
        }

        /// <summary>
        /// Set spawn area center offset
        /// </summary>
        /// <param name="center">New center offset</param>
        public void SetSpawnAreaCenter(Vector3 center)
        {
#if UNITY_EDITOR
            // Update collider in Editor
            if (visualizeCollider != null)
            {
                if (visualizeCollider is BoxCollider boxCollider)
                    boxCollider.center = center;
                else if (visualizeCollider is SphereCollider sphereCollider)
                    sphereCollider.center = center;
            }
#endif
        }

        private void FixedUpdate()
        {
            if (!readyForSpawn)
                return;

            // Check network connection
            if (!NetworkManager.Singleton.IsConnectedClient)
                return;

            // Only owner manages spawning
            if (!IsServer)
                return;

            // Check maximum count
            if (m_CurrentSpawnCount >= m_MaxSpawnCount)
                return;

            // Update timer
            m_SpawnTimer += Time.fixedDeltaTime;
            if (m_SpawnTimer >= m_SpawnInterval)
            {
                SpawnRandomItem();
                m_SpawnTimer = 0f;
            }
        }

        /// <summary>
        /// Spawn a randomly selected item
        /// </summary>
        private void SpawnRandomItem()
        {
            if (m_ItemPrefabs == null || m_ItemPrefabs.Count == 0)
            {
                Debug.LogWarning("ItemSpawner: No item prefabs available to spawn.");
                return;
            }

            // Select one randomly from the list
            int randomIndex = Random.Range(0, m_ItemPrefabs.Count);
            GameObject selectedPrefab = m_ItemPrefabs[randomIndex];

            SpawnItem(selectedPrefab);
        }

        /// <summary>
        /// Spawn item and share with clients
        /// </summary>
        private void SpawnItem(GameObject itemPrefab)
        {
            int retryCount = 0;
            bool spawnSuccessful = false;

            while (!spawnSuccessful && retryCount < m_MaxRetryAttempts)
            {
                Vector3 spawnPosition = m_RandomSpawnPosition
                    ? GetRandomPositionInSpawnArea()
                    : transform.position;

                // If UseNavMesh is enabled, check if there's a NavMesh directly below
                if (UseNavMesh)
                {
                    if (!IsNavMeshBelowPosition(spawnPosition))
                    {
                        retryCount++;
                        Debug.LogWarning($"ItemSpawner: No NavMesh found directly below spawn position {spawnPosition}. Retry attempt {retryCount}/{m_MaxRetryAttempts}.", this);
                        continue;
                    }
                }

                GameObject spawnedObject;

                if (m_UseObjectPool)
                {
                    // Get from pool
                    spawnedObject = GetFromPool(itemPrefab);
                    if (spawnedObject == null)
                    {
                        Debug.LogWarning("ItemSpawner: Failed to get object from pool.");
                        return;
                    }

                    // Get NetworkObject for RPC
                    if (!spawnedObject.TryGetComponent(out NetworkObject networkObject))
                    {
                        Debug.LogError("ItemSpawner: NetworkObject not found on pooled object.", this);
                        return;
                    }

                    // Move to spawn position (server only)
                    spawnedObject.transform.position = spawnPosition;
                    spawnedObject.transform.rotation = spawnTransform.rotation;

                    // Sync position and visibility to all clients via RPC
                    SyncPooledObjectClientRpc(networkObject.NetworkObjectId, spawnPosition, spawnTransform.rotation);

                    // Track which prefab this object came from
                    m_ActiveObjects[spawnedObject] = itemPrefab;

                    m_CurrentSpawnCount++;
                    spawnSuccessful = true;

                    Debug.Log($"ItemSpawner: Spawned {spawnedObject.name} from pool at {spawnPosition}. Current spawn count: {m_CurrentSpawnCount}");
                }
                else
                {
                    // Traditional instantiation
                    spawnedObject = Instantiate(
                        itemPrefab,
                        spawnPosition,
                        spawnTransform.rotation
                    );

                    if (spawnedObject.TryGetComponent<BreakableTarget>(out var breakableTarget))
                    {
                        breakableTarget.IsPoolObject = false;
                    }

                    // Get NetworkObject and Spawn (share with all clients)
                    if (spawnedObject.TryGetComponent(out NetworkObject networkObject))
                    {
                        networkObject.Spawn();
                        m_CurrentSpawnCount++;
                        m_SpawnedInstances.Add(spawnedObject);
                        spawnSuccessful = true;
                    }
                    else
                    {
                        Debug.LogError("ItemSpawner: NetworkObject not found on spawned object.", this);
                        Destroy(spawnedObject);
                        break;
                    }
                }
            }

            if (!spawnSuccessful && retryCount >= m_MaxRetryAttempts)
            {
                Debug.LogError($"ItemSpawner: Failed to spawn item after {m_MaxRetryAttempts} attempts. No valid NavMesh positions found.", this);
            }
        }

        /// <summary>
        /// Sync pooled object position, rotation, and visibility to all clients
        /// </summary>
        [Rpc(SendTo.Everyone)]
        private void SyncPooledObjectClientRpc(ulong networkObjectId, Vector3 position, Quaternion rotation)
        {
            if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(networkObjectId, out NetworkObject networkObject))
            {
                // Set position and rotation
                networkObject.transform.position = position;
                networkObject.transform.rotation = rotation;

                // Reset and show the target
                if (networkObject.TryGetComponent<BreakableTarget>(out var breakableTarget))
                {
                    breakableTarget.ResetTarget();  // This will set IsVisible = true
                }

                Debug.Log($"ItemSpawner (Client): Synced {networkObject.name} to position {position}");
            }
        }

        /// <summary>
        /// Check if NavMesh exists directly below the specified position
        /// </summary>
        /// <param name="position">Position to check</param>
        /// <returns>True if NavMesh exists, false otherwise</returns>
        private bool IsNavMeshBelowPosition(Vector3 position)
        {
            NavMeshHit hit;

            // Search for NavMesh directly below the specified position
            if (NavMesh.SamplePosition(position, out hit, m_NavMeshSearchDistance, NavMesh.AllAreas))
            {
                // Check if hit position is directly below the specified position
                // Confirm Y coordinate is at or below the specified position
                if (hit.position.y <= position.y)
                {
                    // Check horizontal distance tolerance (whether it's directly below)
                    float horizontalDistance = Vector3.Distance(
                        new Vector3(position.x, 0, position.z),
                        new Vector3(hit.position.x, 0, hit.position.z)
                    );

                    // If within tolerance, determine NavMesh exists directly below
                    return horizontalDistance <= m_HorizontalTolerance;
                }
            }

            return false;
        }

        /// <summary>
        /// Clear all spawned instances (only call on server)
        /// </summary>
        public void ClearAllSpawnedInstances()
        {
            if (!IsServer)
            {
                Debug.LogWarning("ItemSpawner: ClearAllSpawnedInstances can only be called on the server.");
                return;
            }

            if (m_UseObjectPool)
            {
                // For object pooling, return all active objects to pool
                List<GameObject> activeObjects = new List<GameObject>(m_ActiveObjects.Keys);
                foreach (var obj in activeObjects)
                {
                    ReturnToPool(obj);
                }
            }
            else
            {
                // Clean up null references first
                m_SpawnedInstances.RemoveAll(item => item == null);

                // Traditional destroy
                foreach (var instance in m_SpawnedInstances)
                {
                    if (instance != null)
                    {
                        if (instance.TryGetComponent(out NetworkObject networkObject))
                        {
                            if (networkObject.IsSpawned)
                            {
                                networkObject.Despawn();
                            }
                        }
                        Destroy(instance);
                    }
                }

                m_SpawnedInstances.Clear();
            }

            m_CurrentSpawnCount = 0;
            m_SpawnTimer = 0f;

            Debug.Log("ItemSpawner: All spawned instances cleared.");
        }

        // Gizmos always displayed (even when object is not selected)
        private void OnDrawGizmos()
        {
            Vector3 position = (m_SpawnTransform != null) ? m_SpawnTransform.position : transform.position;
            if (m_GizmoSettings.showSpawnPoint)
            {
                Gizmos.color = m_GizmoSettings.spawnPointColor;
                Gizmos.DrawWireSphere(position, 0.1f);
            }

            // Display spawn area
            if (m_GizmoSettings.showSpawnArea)
            {
                // Calculate center position in world coordinates
                Vector3 worldCenter = transform.position;

                Gizmos.color = m_GizmoSettings.spawnAreaColor;

                if (m_ColliderType == ColliderType.Box)
                {
                    var size = this.visualizeCollider.bounds.size;
                    var center = transform.position;
                    // Draw box (considering rotation)
                    Gizmos.matrix = Matrix4x4.TRS(worldCenter, transform.rotation, Vector3.one);
                    Gizmos.DrawCube(center, size);

                    Gizmos.color = new Color(m_GizmoSettings.spawnAreaColor.r, m_GizmoSettings.spawnAreaColor.g, m_GizmoSettings.spawnAreaColor.b, 1f);
                    Gizmos.DrawWireCube(center, size);
                    Gizmos.matrix = Matrix4x4.identity;
                }
                else if (m_ColliderType == ColliderType.Sphere)
                {
                    Gizmos.DrawSphere(worldCenter, m_SpawnSphereRadius);

                    Gizmos.color = new Color(m_GizmoSettings.spawnAreaColor.r, m_GizmoSettings.spawnAreaColor.g, m_GizmoSettings.spawnAreaColor.b, 1f);
                    Gizmos.DrawWireSphere(worldCenter, m_SpawnSphereRadius);
                }
            }

            // Display axes
            if (m_GizmoSettings.showAxis)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawLine(position, position + transform.right * m_GizmoSettings.axisLength);

                Gizmos.color = Color.green;
                Gizmos.DrawLine(position, position + transform.up * m_GizmoSettings.axisLength);

                Gizmos.color = Color.blue;
                Gizmos.DrawLine(position, position + transform.forward * m_GizmoSettings.axisLength);
            }
        }

#if UNITY_EDITOR

        private void OnValidate()
        {
            // Update collider when values change in Inspector
            // DestroyImmediate cannot be used in OnValidate, so use EditorApplication.delayCall for delayed execution
            if (!Application.isPlaying)
            {
                UnityEditor.EditorApplication.delayCall += () =>
                {
                    if (this != null)
                    {
                        UpdateColliderType();
                    }
                };
            }
        }

#endif

        [System.Serializable]
        public class GizmoSettings
        {
            [Tooltip("Display spawn area")]
            public bool showSpawnArea = true;

            [Tooltip("Spawn area color")]
            public Color spawnAreaColor = new Color(0f, 1f, 1f, 0.3f);

            [Tooltip("Display spawn point")]
            public bool showSpawnPoint = true;

            [Tooltip("Spawn point color")]
            public Color spawnPointColor = Color.yellow;

            [Tooltip("Display axes")]
            public bool showAxis = true;

            [Tooltip("Axis length")]
            public float axisLength = 0.5f;
        }
    }
}