#if UNITY_EDITOR
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Assets.Team_Prevention.Script;

namespace Assets.Team_Prevention.Editor
{
    /// <summary>
    /// ヒエラルキーの "Items" を探して処理するエディタユーティリティ。
    /// - 子オブジェクトと同名の ItemData (.asset) を作成（なければ）
    /// - ItemDataHolder がなければ追加（Edit モードでも永続）
    /// - ItemDataHolder があり ItemData が未設定なら同名アセットを割当
    /// </summary>
    public static class SyncHierarchyItems_ItemData
    {
        private const string ItemsRootName = "Items";
        private const string DataFolder = "Assets/Team_Prevention/Items/Data";
        private const int DefaultPoint = 20;

        [MenuItem("Tools/Team_Prevention/Sync Items -> Create/Assign ItemData (Point20)")]
        public static void SyncItems()
        {
            // プレイ中は変更しない
            if (EditorApplication.isPlayingOrWillChangePlaymode == true)
            {
                Debug.LogWarning("[SyncHierarchyItems_ItemData] プレイモード中は実行できません。Edit モードで実行してください。");
                return;
            }

            // Data フォルダ作成
            if (Directory.Exists(DataFolder) == false)
            {
                Directory.CreateDirectory(DataFolder);
                AssetDatabase.Refresh();
            }

            int createdAssets = 0;
            int assignedCount = 0;
            int holderAddedCount = 0;

            // 全シーンのルートから "Items" を探す
            var scenes = Enumerable.Range(0, EditorSceneManager.sceneCount)
                                   .Select(EditorSceneManager.GetSceneAt)
                                   .ToArray();

            var itemsRoots = scenes
                .SelectMany(s => s.GetRootGameObjects())
                .SelectMany(root => root.GetComponentsInChildren<Transform>(true))
                .Select(t => t.gameObject)
                .Where(go => go.name == ItemsRootName)
                .Distinct()
                .ToArray();

            if (itemsRoots == null || itemsRoots.Length == 0)
            {
                Debug.LogWarning($"[SyncHierarchyItems_ItemData] ヒエラルキー内に '{ItemsRootName}' が見つかりませんでした。");
                return;
            }

            foreach (var itemsRoot in itemsRoots)
            {
                foreach (Transform child in itemsRoot.transform)
                {
                    if (child == null)
                    {
                        continue;
                    }

                    string itemName = child.gameObject.name.Trim();
                    if (string.IsNullOrEmpty(itemName))
                    {
                        continue;
                    }

                    string assetPath = $"{DataFolder}/{itemName}.asset";

                    // ItemData アセットが存在しなければ作成
                    var existingData = AssetDatabase.LoadAssetAtPath<ItemData>(assetPath);
                    if (existingData == null)
                    {
                        var newData = ScriptableObject.CreateInstance<ItemData>();
                        AssetDatabase.CreateAsset(newData, assetPath);

                        // private フィールドに SerializedObject で値を設定
                        var so = new SerializedObject(newData);
                        var pName = so.FindProperty("_name");
                        if (pName != null)
                        {
                            pName.stringValue = itemName;
                        }

                        var pPoint = so.FindProperty("_point");
                        if (pPoint != null)
                        {
                            pPoint.intValue = DefaultPoint;
                        }

                        so.ApplyModifiedProperties();
                        EditorUtility.SetDirty(newData);
                        AssetDatabase.SaveAssets();

                        existingData = newData;
                        createdAssets++;
                        Debug.Log($"[SyncHierarchyItems_ItemData] 作成: {assetPath} (Point={DefaultPoint})");
                    }

                    // ItemDataHolder が無ければ追加（Undo できるように Undo を使用）
                    var holder = child.gameObject.GetComponent<ItemDataHolder>();
                    if (holder == null)
                    {
                        holder = Undo.AddComponent<ItemDataHolder>(child.gameObject);
                        holderAddedCount++;
                    }

                    // ItemDataHolder の ItemData が未設定なら割当
                    // private フィールドを SerializedObject 経由で設定する
                    if (holder != null)
                    {
                        var holderSO = new SerializedObject(holder);
                        var itemDataProp = holderSO.FindProperty("_itemData");
                        if (itemDataProp != null && itemDataProp.objectReferenceValue == null)
                        {
                            itemDataProp.objectReferenceValue = existingData;
                            holderSO.ApplyModifiedProperties();
                            EditorUtility.SetDirty(holder);
                            assignedCount++;
                            Debug.Log($"[SyncHierarchyItems_ItemData] 割当: {child.gameObject.name} -> {assetPath}");
                        }
                    }
                }
            }

            if (createdAssets > 0 || assignedCount > 0 || holderAddedCount > 0)
            {
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }

            Debug.Log($"[SyncHierarchyItems_ItemData] 完了: 作成={createdAssets}, Holder追加={holderAddedCount}, 割当={assignedCount}");
        }
    }
}
#endif