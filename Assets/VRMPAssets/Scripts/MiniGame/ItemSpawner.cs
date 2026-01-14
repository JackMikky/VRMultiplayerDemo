using Unity.Netcode;
using UnityEngine;
using System.Collections.Generic;
using UnityEngine.AI;

namespace XRMultiplayer.MiniGames
{
    public enum ColliderType
    {
        Box,
        Sphere,
    }

    /// <summary>
    /// ネットワーク対応のアイテムスポナー
    /// </summary>
    public class ItemSpawner : NetworkBehaviour
    {
        [SerializeField]
        [Tooltip("生成するアイテムのプレハブ")]
        private List<GameObject> m_ItemPrefabs;

        [SerializeField]
        [Tooltip("コライダーの種類")]
        private ColliderType m_ColliderType = ColliderType.Box;

        [SerializeField]
        [Tooltip("スポーンエリアのサイズ")]
        private Vector3 m_SpawnAreaSize = new Vector3(5f, 2f, 5f);

        [SerializeField]
        [Tooltip("スポーン球体の半径")]
        private float m_SpawnSphereRadius = 2.5f;

        [SerializeField]
        [Tooltip("スポーンエリアの中心オフセット")]
        private Vector3 m_SpawnAreaCenter = Vector3.zero;

        [SerializeField]
        [Tooltip("アイテムの生成位置")]
        private Transform m_SpawnTransform;

        [SerializeField]
        [Tooltip("生成間隔（秒）")]
        private float m_SpawnInterval = 2f;

        [SerializeField]
        [Tooltip("最大生成数")]
        private int m_MaxSpawnCount = 10;

        [SerializeField]
        [Tooltip("ランダムな位置に生成")]
        private bool m_RandomSpawnPosition = false;

        [SerializeField]
        [Tooltip("NavMeshを使用してスポーン位置を検証")]
        private bool UseNavMesh = false;

        [SerializeField]
        [Tooltip("NavMesh検索の最大距離")]
        private float m_NavMeshSearchDistance = 10f;

        [SerializeField]
        [Tooltip("真下判定の水平許容範囲（メートル）")]
        private float m_HorizontalTolerance = 1f;

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

        public bool readyForSpawn = false;

        public ColliderType VisualColliderType
        {
            get => m_ColliderType;
            set => m_ColliderType = value;
        }

        /// <summary>
        /// 生成位置
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

        private void Awake()
        {
            if (m_ItemPrefabs == null || m_ItemPrefabs.Count == 0)
            {
                Debug.LogError("ItemSpawner: アイテムプレハブが設定されていません。", this);
                enabled = false;
                return;
            }

            foreach (var prefab in m_ItemPrefabs)
            {
                if (prefab == null)
                {
                    Debug.LogError("ItemSpawner: リスト内にnullのプレハブが含まれています。", this);
                    enabled = false;
                    return;
                }

                if (!prefab.TryGetComponent<NetworkObject>(out _))
                {
                    Debug.LogError("ItemSpawner: プレハブにNetworkObjectコンポーネントが必要です。", this);
                    enabled = false;
                    return;
                }
            }

#if UNITY_EDITOR
            // Editor上でのみ可視化用コライダーを初期化
            InitializeSpawnAreaCollider();
#endif
        }

#if UNITY_EDITOR

        /// <summary>
        /// スポーンエリアのコライダーを初期化（Editor専用）
        /// </summary>
        private void InitializeSpawnAreaCollider()
        {
            UpdateColliderType();
        }

        /// <summary>
        /// コライダータイプに応じてコライダーを更新（Editor専用）
        /// </summary>
        public void UpdateColliderType()
        {
            // 既存のコライダーを削除
            if (visualizeCollider != null)
            {
                if (!Application.isPlaying)
                    DestroyImmediate(visualizeCollider);
                else
                    Destroy(visualizeCollider);
            }

            // 新しいコライダーを作成
            switch (m_ColliderType)
            {
                case ColliderType.Box:
                    BoxCollider boxCollider = gameObject.AddComponent<BoxCollider>();
                    boxCollider.size = m_SpawnAreaSize;
                    boxCollider.center = m_SpawnAreaCenter;
                    boxCollider.isTrigger = true;
                    visualizeCollider = boxCollider;
                    break;

                case ColliderType.Sphere:
                    SphereCollider sphereCollider = gameObject.AddComponent<SphereCollider>();
                    sphereCollider.radius = m_SpawnSphereRadius;
                    sphereCollider.center = m_SpawnAreaCenter;
                    sphereCollider.isTrigger = true;
                    visualizeCollider = sphereCollider;
                    break;
            }
        }

#endif

        /// <summary>
        /// スポーンエリア内のランダムな位置を取得
        /// </summary>
        /// <returns>ワールド座標でのランダムな位置</returns>
        private Vector3 GetRandomPositionInSpawnArea()
        {
            Vector3 localRandomPoint = Vector3.zero;

            if (m_ColliderType == ColliderType.Box)
            {
                // ボックス内のランダムな位置を計算
                localRandomPoint = m_SpawnAreaCenter + new Vector3(
                    Random.Range(-m_SpawnAreaSize.x * 0.5f, m_SpawnAreaSize.x * 0.5f),
                    Random.Range(-m_SpawnAreaSize.y * 0.5f, m_SpawnAreaSize.y * 0.5f),
                    Random.Range(-m_SpawnAreaSize.z * 0.5f, m_SpawnAreaSize.z * 0.5f)
                );
            }
            else if (m_ColliderType == ColliderType.Sphere)
            {
                // 球体内のランダムな位置を計算
                Vector3 randomInsideSphere = Random.insideUnitSphere * m_SpawnSphereRadius;
                localRandomPoint = m_SpawnAreaCenter + randomInsideSphere;
            }

            // ローカル座標からワールド座標に変換
            return transform.TransformPoint(localRandomPoint);
        }

        /// <summary>
        /// スポーンエリアのサイズを設定
        /// </summary>
        /// <param name="size">新しいサイズ</param>
        public void SetSpawnAreaSize(Vector3 size)
        {
            m_SpawnAreaSize = size;
#if UNITY_EDITOR
            // Editor上のコライダーも更新
            if (visualizeCollider is BoxCollider boxCollider)
            {
                boxCollider.size = size;
            }
#endif
        }

        /// <summary>
        /// スポーン球体の半径を設定
        /// </summary>
        /// <param name="radius">新しい半径</param>
        public void SetSpawnSphereRadius(float radius)
        {
            m_SpawnSphereRadius = radius;
#if UNITY_EDITOR
            // Editor上のコライダーも更新
            if (visualizeCollider is SphereCollider sphereCollider)
            {
                sphereCollider.radius = radius;
            }
#endif
        }

        /// <summary>
        /// スポーンエリアの中心オフセットを設定
        /// </summary>
        /// <param name="center">新しい中心オフセット</param>
        public void SetSpawnAreaCenter(Vector3 center)
        {
            m_SpawnAreaCenter = center;
#if UNITY_EDITOR
            // Editor上のコライダーも更新
            if (visualizeCollider != null)
            {
                if (visualizeCollider is BoxCollider boxCollider)
                    boxCollider.center = center;
                else if (visualizeCollider is SphereCollider sphereCollider)
                    sphereCollider.center = center;
            }
#endif
        }

        private void Update()
        {
            if (!readyForSpawn)
                return;
            // ネットワーク接続確認
            if (!NetworkManager.Singleton.IsConnectedClient)
                return;

            // オーナーのみが生成を管理
            if (!IsServer)
                return;

            // 最大数チェック
            if (m_CurrentSpawnCount >= m_MaxSpawnCount)
                return;

            // タイマー更新
            m_SpawnTimer += Time.deltaTime;
            if (m_SpawnTimer >= m_SpawnInterval)
            {
                SpawnRandomItem();
                m_SpawnTimer = 0f;
            }
        }

        /// <summary>
        /// ランダムに選択したアイテムを生成
        /// </summary>
        private void SpawnRandomItem()
        {
            if (m_ItemPrefabs == null || m_ItemPrefabs.Count == 0)
            {
                Debug.LogWarning("ItemSpawner: 生成可能なアイテムプレハブがありません。");
                return;
            }

            // リストからランダムに1つ選択
            int randomIndex = Random.Range(0, m_ItemPrefabs.Count);
            GameObject selectedPrefab = m_ItemPrefabs[randomIndex];

            SpawnItem(selectedPrefab);
        }

        /// <summary>
        /// アイテムを生成してクライアントに共有
        /// </summary>
        private void SpawnItem(GameObject itemPrefab)
        {
            Vector3 spawnPosition = m_RandomSpawnPosition
                ? GetRandomPositionInSpawnArea()
                : transform.position;

            // UseNavMeshが有効な場合、真下にNavMeshがあるか確認
            if (UseNavMesh)
            {
                if (!IsNavMeshBelowPosition(spawnPosition))
                {
                    Debug.LogWarning($"ItemSpawner: スポーン位置 {spawnPosition} の真下にNavMeshが見つかりませんでした。オブジェクトの生成をスキップします。", this);
                    return;
                }
            }

            GameObject spawnedObject = Instantiate(
                itemPrefab,
                spawnPosition,
                spawnTransform.rotation
            );

            // NetworkObjectを取得してSpawn（全クライアントに共有）
            if (spawnedObject.TryGetComponent(out NetworkObject networkObject))
            {
                networkObject.Spawn();
                m_CurrentSpawnCount++;
            }
            else
            {
                Debug.LogError("ItemSpawner: 生成されたオブジェクトにNetworkObjectが見つかりません。", this);
                Destroy(spawnedObject);
            }
        }

        /// <summary>
        /// 指定位置の真下にNavMeshが存在するかを確認
        /// </summary>
        /// <param name="position">確認する位置</param>
        /// <returns>NavMeshが存在する場合はtrue、存在しない場合はfalse</returns>
        private bool IsNavMeshBelowPosition(Vector3 position)
        {
            NavMeshHit hit;

            // 指定位置から真下にNavMeshを検索
            if (NavMesh.SamplePosition(position, out hit, m_NavMeshSearchDistance, NavMesh.AllAreas))
            {
                // ヒット位置が指定位置の真下かどうかを確認
                // Y座標が指定位置以下であることを確認
                if (hit.position.y <= position.y)
                {
                    // 水平距離の許容範囲をチェック（真下かどうか）
                    float horizontalDistance = Vector3.Distance(
                        new Vector3(position.x, 0, position.z),
                        new Vector3(hit.position.x, 0, hit.position.z)
                    );

                    // 許容範囲内であればNavMeshが真下に存在すると判断
                    return horizontalDistance <= m_HorizontalTolerance;
                }
            }

            return false;
        }

        /// <summary>
        /// 手動で1つアイテムを生成
        /// </summary>
        public void SpawnItemManually()
        {
            if (!IsOwner)
                return;

            if (m_CurrentSpawnCount >= m_MaxSpawnCount)
            {
                Debug.LogWarning("ItemSpawner: 最大生成数に達しています。");
                return;
            }

            SpawnRandomItem();
        }

        /// <summary>
        /// 生成カウントをリセット
        /// </summary>
        public void ResetSpawnCount()
        {
            if (!IsOwner)
                return;

            m_CurrentSpawnCount = 0;
        }

        // 常に表示されるGizmos（オブジェクトが選択されていない時も表示）
        private void OnDrawGizmos()
        {
            Vector3 position = (m_SpawnTransform != null) ? m_SpawnTransform.position : transform.position;
            if (m_GizmoSettings.showSpawnPoint)
            {
                Gizmos.color = m_GizmoSettings.spawnPointColor;
                Gizmos.DrawWireSphere(position, 0.1f);
            }

            // スポーンエリアの表示
            if (m_GizmoSettings.showSpawnArea)
            {
                // ワールド座標での中心位置を計算
                Vector3 worldCenter = transform.position;

                Gizmos.color = m_GizmoSettings.spawnAreaColor;

                if (m_ColliderType == ColliderType.Box)
                {
                    var size = this.visualizeCollider.bounds.size;
                    var center = this.visualizeCollider.bounds.center + transform.position;
                    // ボックスの描画（回転を考慮）
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

            // 軸の表示
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
            // Inspector での値変更時にコライダーを更新
            // OnValidateではDestroyImmediateが使えないため、EditorApplication.delayCallで遅延実行
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
            [Tooltip("スポーンエリアを表示")]
            public bool showSpawnArea = true;

            [Tooltip("スポーンエリアの色")]
            public Color spawnAreaColor = new Color(0f, 1f, 1f, 0.3f);

            [Tooltip("スポーンポイントを表示")]
            public bool showSpawnPoint = true;

            [Tooltip("スポーンポイントの色")]
            public Color spawnPointColor = Color.yellow;

            [Tooltip("軸を表示")]
            public bool showAxis = true;

            [Tooltip("軸の長さ")]
            public float axisLength = 0.5f;
        }
    }
}