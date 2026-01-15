using UnityEditor;
using UnityEngine;
using XRMultiplayer.MiniGames;

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
    private SerializedProperty m_GizmoSettingsProperty;

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
        m_GizmoSettingsProperty = serializedObject.FindProperty("m_GizmoSettings");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        ItemSpawner spawner = (ItemSpawner)target;

        EditorGUILayout.PropertyField(m_ItemPrefabsProperty, new GUIContent("Item Prefabs"), true);

        EditorGUILayout.Space();
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

        EditorGUI.BeginChangeCheck();
        if (EditorGUI.EndChangeCheck())
        {
            serializedObject.ApplyModifiedProperties();
            if (spawner.visualizeCollider != null)
            {
                if (spawner.visualizeCollider is BoxCollider boxCollider)
                    boxCollider.center = m_SpawnTransformProperty.vector3Value;
                else if (spawner.visualizeCollider is SphereCollider sphereCollider)
                    sphereCollider.center = m_SpawnTransformProperty.vector3Value;
            }
        }

        EditorGUI.BeginDisabledGroup(true);
        EditorGUILayout.PropertyField(m_VisualizeColliderProperty, new GUIContent("Visualize Collider (Auto)"));
        EditorGUI.EndDisabledGroup();

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Spawn Settings", EditorStyles.boldLabel);

        EditorGUILayout.PropertyField(m_SpawnTransformProperty);
        EditorGUILayout.PropertyField(m_SpawnIntervalProperty);
        EditorGUILayout.PropertyField(m_MaxSpawnCountProperty);
        EditorGUILayout.PropertyField(m_RandomSpawnPositionProperty);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("NavMesh Settings", EditorStyles.boldLabel);

        EditorGUILayout.PropertyField(m_UseNavMeshProperty, new GUIContent("Use NavMesh"));

        if (m_UseNavMeshProperty != null && m_UseNavMeshProperty.boolValue)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.HelpBox("When NavMesh validation is enabled, objects will only spawn on valid navigation mesh. If validation fails, the system will automatically retry.", MessageType.Info);
            EditorGUILayout.PropertyField(m_NavMeshSearchDistanceProperty, new GUIContent("Search Distance"));
            EditorGUILayout.PropertyField(m_HorizontalToleranceProperty, new GUIContent("Horizontal Tolerance"));
            EditorGUILayout.PropertyField(m_MaxRetryAttemptsProperty, new GUIContent("Max Retry Attempts"));
            EditorGUI.indentLevel--;
        }

        EditorGUILayout.Space();
        EditorGUILayout.PropertyField(m_GizmoSettingsProperty, true);

        serializedObject.ApplyModifiedProperties();
    }
}

public enum ColliderType
{
    Box = 0,
    Sphere = 1
}