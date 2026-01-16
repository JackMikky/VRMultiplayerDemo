using UnityEditor;
using UnityEngine;
using XRMultiplayer.MiniGames;
using UnityEngine.XR.Content.Interaction;

[CustomEditor(typeof(ItemSpawner))]
public class SpawnAreaInspector : Editor
{
    private SerializedProperty m_ItemPrefabsProperty;
    private SerializedProperty m_ColliderTypeProperty;
    private SerializedProperty m_VisualizeColliderProperty;
    private SerializedProperty m_SpawnAreaSizeProperty;
    private SerializedProperty m_SpawnSphereRadiusProperty;
    private SerializedProperty m_SpawnTransformProperty;
    private SerializedProperty m_SpawnIntervalProperty;
    private SerializedProperty m_MaxSpawnCountProperty;
    private SerializedProperty m_RandomSpawnPositionProperty;
    private SerializedProperty m_UseNavMeshProperty;
    private SerializedProperty m_NavMeshSearchDistanceProperty;
    private SerializedProperty m_HorizontalToleranceProperty;
    private SerializedProperty m_MaxRetryAttemptsProperty;
    private SerializedProperty m_UseObjectPoolProperty;
    private SerializedProperty m_InitialPoolSizeProperty;
    private SerializedProperty m_AllowPoolGrowthProperty;
    private SerializedProperty m_GizmoSettingsProperty;

    // Runtime statistics
    private bool m_ShowRuntimeStats = true;

    private void OnEnable()
    {
        m_ItemPrefabsProperty = serializedObject.FindProperty("m_ItemPrefabs");
        m_ColliderTypeProperty = serializedObject.FindProperty("m_ColliderType");
        m_VisualizeColliderProperty = serializedObject.FindProperty("visualizeCollider");
        m_SpawnAreaSizeProperty = serializedObject.FindProperty("m_SpawnAreaSize");
        m_SpawnSphereRadiusProperty = serializedObject.FindProperty("m_SpawnSphereRadius");
        m_SpawnTransformProperty = serializedObject.FindProperty("m_SpawnTransform");
        m_SpawnIntervalProperty = serializedObject.FindProperty("m_SpawnInterval");
        m_MaxSpawnCountProperty = serializedObject.FindProperty("m_MaxSpawnCount");
        m_RandomSpawnPositionProperty = serializedObject.FindProperty("m_RandomSpawnPosition");
        m_UseNavMeshProperty = serializedObject.FindProperty("UseNavMesh");
        m_NavMeshSearchDistanceProperty = serializedObject.FindProperty("m_NavMeshSearchDistance");
        m_HorizontalToleranceProperty = serializedObject.FindProperty("m_HorizontalTolerance");
        m_MaxRetryAttemptsProperty = serializedObject.FindProperty("m_MaxRetryAttempts");
        m_UseObjectPoolProperty = serializedObject.FindProperty("m_UseObjectPool");
        m_InitialPoolSizeProperty = serializedObject.FindProperty("m_InitialPoolSize");
        m_AllowPoolGrowthProperty = serializedObject.FindProperty("m_AllowPoolGrowth");
        m_GizmoSettingsProperty = serializedObject.FindProperty("m_GizmoSettings");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        ItemSpawner spawner = (ItemSpawner)target;

        // Item Prefabs Section
        DrawItemPrefabsSection(spawner);

        EditorGUILayout.Space();

        // Spawn Area Settings Section
        DrawSpawnAreaSection(spawner);

        EditorGUILayout.Space();

        // Spawn Settings Section
        DrawSpawnSettingsSection();

        EditorGUILayout.Space();

        // Object Pool Settings Section
        DrawObjectPoolSection();

        EditorGUILayout.Space();

        // NavMesh Settings Section
        DrawNavMeshSection();

        EditorGUILayout.Space();

        // Gizmo Settings Section
        EditorGUILayout.PropertyField(m_GizmoSettingsProperty, true);

        // Runtime Statistics Section (Play mode only)
        if (Application.isPlaying)
        {
            EditorGUILayout.Space();
            DrawRuntimeStatistics(spawner);
        }

        serializedObject.ApplyModifiedProperties();
    }

    private void DrawItemPrefabsSection(ItemSpawner spawner)
    {
        EditorGUILayout.PropertyField(m_ItemPrefabsProperty, new GUIContent("Item Prefabs"), true);

        // Validate prefabs
        if (m_ItemPrefabsProperty.arraySize > 0)
        {
            bool hasPoolableObjects = false;
            bool hasNonPoolableObjects = false;

            for (int i = 0; i < m_ItemPrefabsProperty.arraySize; i++)
            {
                SerializedProperty prefabProperty = m_ItemPrefabsProperty.GetArrayElementAtIndex(i);
                GameObject prefab = prefabProperty.objectReferenceValue as GameObject;

                if (prefab != null)
                {
                    if (prefab.TryGetComponent<BreakableTarget>(out _))
                    {
                        hasPoolableObjects = true;
                    }
                    else
                    {
                        hasNonPoolableObjects = true;
                    }
                }
            }

            if (hasPoolableObjects && hasNonPoolableObjects)
            {
                EditorGUILayout.HelpBox("Mix of poolable (BreakableTarget) and non-poolable objects detected. Pool settings will only affect BreakableTarget objects.", MessageType.Info);
            }
            else if (hasPoolableObjects)
            {
                EditorGUILayout.HelpBox($"All prefabs are poolable. Object pooling is recommended for better performance.", MessageType.Info);
            }
            else if (hasNonPoolableObjects)
            {
                EditorGUILayout.HelpBox("None of the prefabs have BreakableTarget component. Object pooling will not be used.", MessageType.Warning);
            }
        }
    }

    private void DrawSpawnAreaSection(ItemSpawner spawner)
    {
        EditorGUILayout.LabelField("Spawn Area Settings", EditorStyles.boldLabel);

        EditorGUI.BeginChangeCheck();
        EditorGUILayout.PropertyField(m_ColliderTypeProperty, new GUIContent("Collider Type"));
        if (EditorGUI.EndChangeCheck())
        {
            serializedObject.ApplyModifiedProperties();
            spawner.UpdateColliderType();
            serializedObject.Update();
        }

        ColliderType colliderType = (ColliderType)m_ColliderTypeProperty.enumValueIndex;

        if (colliderType == ColliderType.Box)
        {
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(m_SpawnAreaSizeProperty, new GUIContent("Spawn Area Size"));
            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
                if (spawner.visualizeCollider is BoxCollider boxCollider)
                {
                    boxCollider.size = m_SpawnAreaSizeProperty.vector3Value;
                }
            }
        }
        else if (colliderType == ColliderType.Sphere)
        {
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(m_SpawnSphereRadiusProperty, new GUIContent("Spawn Sphere Radius"));
            if (EditorGUI.EndChangeCheck() && spawner.visualizeCollider is SphereCollider sphereCollider)
            {
                serializedObject.ApplyModifiedProperties();
                sphereCollider.radius = m_SpawnSphereRadiusProperty.floatValue;
            }
        }

        EditorGUI.BeginDisabledGroup(true);
        EditorGUILayout.PropertyField(m_VisualizeColliderProperty, new GUIContent("Visualize Collider (Auto)"));
        EditorGUI.EndDisabledGroup();
    }

    private void DrawSpawnSettingsSection()
    {
        EditorGUILayout.LabelField("Spawn Settings", EditorStyles.boldLabel);

        EditorGUILayout.PropertyField(m_SpawnTransformProperty);
        EditorGUILayout.PropertyField(m_SpawnIntervalProperty, new GUIContent("Spawn Interval (seconds)"));
        EditorGUILayout.PropertyField(m_MaxSpawnCountProperty, new GUIContent("Maximum Active Objects"));
        EditorGUILayout.PropertyField(m_RandomSpawnPositionProperty, new GUIContent("Random Spawn Position"));

        if (!m_RandomSpawnPositionProperty.boolValue)
        {
            EditorGUILayout.HelpBox("Objects will spawn at the exact position of the Spawn Transform.", MessageType.Info);
        }
    }

    private void DrawObjectPoolSection()
    {
        EditorGUILayout.LabelField("Object Pool Settings", EditorStyles.boldLabel);

        EditorGUILayout.PropertyField(m_UseObjectPoolProperty, new GUIContent("Use Object Pool"));

        if (m_UseObjectPoolProperty != null && m_UseObjectPoolProperty.boolValue)
        {
            EditorGUI.indentLevel++;

            EditorGUILayout.HelpBox(
                "Object pooling optimizes performance by:\n" +
                "• Pre-spawning NetworkObjects once\n" +
                "• Reusing objects via activation/deactivation\n" +
                "• Avoiding repeated Spawn/Despawn network calls\n" +
                "• Reducing garbage collection\n\n" +
                "Objects return to pool automatically via onBreak event.",
                MessageType.Info);

            EditorGUILayout.PropertyField(m_InitialPoolSizeProperty, new GUIContent("Initial Pool Size"));

            if (m_InitialPoolSizeProperty.intValue < 1)
            {
                EditorGUILayout.HelpBox("Initial pool size should be at least 1.", MessageType.Warning);
            }
            else if (m_InitialPoolSizeProperty.intValue > m_MaxSpawnCountProperty.intValue)
            {
                EditorGUILayout.HelpBox($"Initial pool size ({m_InitialPoolSizeProperty.intValue}) is larger than maximum spawn count ({m_MaxSpawnCountProperty.intValue}). Consider reducing pool size.", MessageType.Warning);
            }

            EditorGUILayout.PropertyField(m_AllowPoolGrowthProperty, new GUIContent("Allow Pool Growth"));

            if (m_AllowPoolGrowthProperty.boolValue)
            {
                EditorGUILayout.HelpBox(
                    "Pool will automatically expand when empty.\n" +
                    "Note: Expansion creates new NetworkObjects at runtime, which may cause brief performance drops.",
                    MessageType.Warning);
            }
            else
            {
                EditorGUILayout.HelpBox(
                    "Pool will not expand beyond initial size.\n" +
                    "Ensure Initial Pool Size ≥ Maximum Active Objects to avoid spawn failures.",
                    MessageType.Warning);
            }

            EditorGUI.indentLevel--;

            // Pool Architecture Explanation
            EditorGUILayout.Space(5);
            if (GUILayout.Button("How Object Pooling Works", EditorStyles.linkLabel))
            {
                EditorUtility.DisplayDialog(
                    "Object Pooling Architecture",
                    "1. INITIALIZATION (OnNetworkSpawn):\n" +
                    "   - Instantiate objects as children of PoolContainer\n" +
                    "   - Spawn NetworkObjects immediately\n" +
                    "   - Register onBreak event → ReturnToPool\n" +
                    "   - Deactivate objects (SetActive(false))\n\n" +
                    "2. SPAWNING (GetFromPool):\n" +
                    "   - Dequeue object from pool\n" +
                    "   - Move to spawn position\n" +
                    "   - Activate object (SetActive(true))\n" +
                    "   - Network sync happens automatically\n\n" +
                    "3. RETURN (ReturnToPool via onBreak):\n" +
                    "   - onBreak event triggers ReturnToPool\n" +
                    "   - Deactivate object (SetActive(false))\n" +
                    "   - Move back to PoolContainer (position 0,0,0)\n" +
                    "   - Enqueue back to pool\n" +
                    "   - No Despawn needed!\n\n" +
                    "Benefits:\n" +
                    "✓ Single Spawn per object (reduced network traffic)\n" +
                    "✓ Fast reuse via activation\n" +
                    "✓ Minimal garbage collection\n" +
                    "✓ Event-driven architecture (no direct references)",
                    "OK");
            }
        }
        else
        {
            EditorGUILayout.HelpBox(
                "Object pooling is disabled. Objects will be created and destroyed normally using Instantiate/Despawn.\n" +
                "This may cause performance issues with frequent spawning.",
                MessageType.Warning);
        }
    }

    private void DrawNavMeshSection()
    {
        EditorGUILayout.LabelField("NavMesh Settings", EditorStyles.boldLabel);

        EditorGUILayout.PropertyField(m_UseNavMeshProperty, new GUIContent("Use NavMesh Validation"));

        if (m_UseNavMeshProperty != null && m_UseNavMeshProperty.boolValue)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.HelpBox(
                "NavMesh validation ensures objects spawn only on valid navigation mesh.\n" +
                "If validation fails, the system will automatically retry up to Max Retry Attempts.",
                MessageType.Info);

            EditorGUILayout.PropertyField(m_NavMeshSearchDistanceProperty, new GUIContent("Search Distance"));
            EditorGUILayout.PropertyField(m_HorizontalToleranceProperty, new GUIContent("Horizontal Tolerance"));
            EditorGUILayout.PropertyField(m_MaxRetryAttemptsProperty, new GUIContent("Max Retry Attempts"));

            if (m_MaxRetryAttemptsProperty.intValue < 1)
            {
                EditorGUILayout.HelpBox("Max retry attempts should be at least 1.", MessageType.Warning);
            }

            EditorGUI.indentLevel--;
        }
    }

    private void DrawRuntimeStatistics(ItemSpawner spawner)
    {
        m_ShowRuntimeStats = EditorGUILayout.Foldout(m_ShowRuntimeStats, "Runtime Statistics", true, EditorStyles.foldoutHeader);

        if (m_ShowRuntimeStats)
        {
            EditorGUI.indentLevel++;

            EditorGUI.BeginDisabledGroup(true);

            // Basic stats
            EditorGUILayout.LabelField("Spawner Status", spawner.readyForSpawn ? "Active" : "Inactive");
            EditorGUILayout.LabelField("Active Spawned Objects", $"{spawner.SpawnedInstances.Count} / {m_MaxSpawnCountProperty.intValue}");

            // Draw progress bar for spawn count
            float spawnProgress = m_MaxSpawnCountProperty.intValue > 0
                ? (float)spawner.SpawnedInstances.Count / m_MaxSpawnCountProperty.intValue
                : 0f;
            Rect progressRect = EditorGUILayout.GetControlRect(false, 20);
            EditorGUI.ProgressBar(progressRect, spawnProgress, $"{spawner.SpawnedInstances.Count} / {m_MaxSpawnCountProperty.intValue}");

            EditorGUI.EndDisabledGroup();

            EditorGUILayout.Space(5);

            // Debug buttons
            EditorGUILayout.LabelField("Debug Controls", EditorStyles.boldLabel);

            EditorGUI.BeginDisabledGroup(!Application.isPlaying || !spawner.isActiveAndEnabled);

            if (GUILayout.Button("Clear All Spawned Instances"))
            {
                if (EditorUtility.DisplayDialog(
                    "Clear All Spawned Instances",
                    "This will return all active objects to the pool (or destroy them if pooling is disabled).\n\nContinue?",
                    "Yes",
                    "Cancel"))
                {
                    spawner.ClearAllSpawnedInstances();
                }
            }

            EditorGUI.EndDisabledGroup();

            EditorGUI.indentLevel--;

            // Auto-refresh in play mode
            if (Application.isPlaying)
            {
                Repaint();
            }
        }
    }
}

public enum ColliderType
{
    Box = 0,
    Sphere = 1
}