#if UNITY_EDITOR
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using Assets.Team_Prevention.Script;

/// <summary>
/// ヒエラルキーの "Items" 配下の子オブジェクトに対して以下を行うエディタユーティリティ:
/// - ItemProperty を追加（未存在時）
/// - XRGrabInteractable を追加（未存在時）
/// - ItemEffectTrigger を追加（未存在時）
/// - Rigidbody / Collider が無ければ追加（XRGrabInteractable の動作要件として）
/// </summary>
public static class AddInteractableAndProperty
{
    private const string ItemsRootName = "Items";

    /// <summary>
    /// 実行メニュー: __Tools > Team_Prevention > Add XRGrabInteractable & ItemProperty__
    /// </summary>
    [MenuItem("Tools/Team_Prevention/Add XRGrabInteractable & ItemProperty")]
    public static void AddToItemsChildren()
    {
        // プレイ中は変更しない
        if (EditorApplication.isPlayingOrWillChangePlaymode == true)
        {
            Debug.LogWarning("[AddInteractableAndProperty] プレイモード中は実行できません。Edit モードで実行してください。");
            return;
        }

        int addedItemProperty = 0;
        int addedInteractable = 0;
        int addedEffectTrigger = 0;
        int addedRigidbody = 0;
        int addedCollider = 0;

        // 全開いているシーンのルートから Items を検索
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
            Debug.LogWarning($"[AddInteractableAndProperty] ヒエラルキー内に '{ItemsRootName}' が見つかりませんでした。");
            return;
        }

        foreach (var itemsRoot in itemsRoots)
        {
            var scene = itemsRoot.scene;

            foreach (Transform child in itemsRoot.transform)
            {
                if (child == null)
                {
                    continue;
                }

                var go = child.gameObject;

                // ItemProperty を追加（未存在時）
                var itemProp = go.GetComponent<ItemProperty>();
                if (itemProp == null)
                {
                    Undo.AddComponent<ItemProperty>(go);
                    addedItemProperty++;
                }

                // XRGrabInteractable を追加（未存在時）
                var grab = go.GetComponent<XRGrabInteractable>();
                if (grab == null)
                {
                    // Rigidbody / Collider の要件を満たす
                    var rb = go.GetComponent<Rigidbody>();
                    if (rb == null)
                    {
                        rb = Undo.AddComponent<Rigidbody>(go);
                        // 既存挙動に影響を最小限にする設定
                        rb.mass = 1f;
                        rb.useGravity = false;
                        rb.isKinematic = false;
                        addedRigidbody++;
                    }

                    var col = go.GetComponent<Collider>();
                    if (col == null)
                    {
                        // シンプルに BoxCollider を追加
                        col = Undo.AddComponent<BoxCollider>(go);
                        addedCollider++;
                    }

                    // XRGrabInteractable を追加
                    Undo.AddComponent<XRGrabInteractable>(go);
                    addedInteractable++;
                }
                else
                {
                    // 既に XRGrabInteractable がある場合でも Rigidbody/Collider を補完する
                    var rb2 = go.GetComponent<Rigidbody>();
                    if (rb2 == null)
                    {
                        rb2 = Undo.AddComponent<Rigidbody>(go);
                        rb2.mass = 1f;
                        rb2.useGravity = false;
                        rb2.isKinematic = false;
                        addedRigidbody++;
                    }

                    var col2 = go.GetComponent<Collider>();
                    if (col2 == null)
                    {
                        Undo.AddComponent<BoxCollider>(go);
                        addedCollider++;
                    }
                }

                // ItemEffectTrigger を追加（未存在時）
                var effectTrigger = go.GetComponent<ItemEffectTrigger>();
                if (effectTrigger == null)
                {
                    Undo.AddComponent<ItemEffectTrigger>(go);
                    addedEffectTrigger++;
                }

                // シーンを Dirty にする（変更を保存できるように）
                EditorSceneManager.MarkSceneDirty(scene);
            }
        }

        // 保存してアセット/シーンを更新
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"[AddInteractableAndProperty] 完了: ItemProperty追加={addedItemProperty}, XRGrabInteractable追加={addedInteractable}, ItemEffectTrigger追加={addedEffectTrigger}, Rigidbody追加={addedRigidbody}, Collider追加={addedCollider}");
    }
}
#endif