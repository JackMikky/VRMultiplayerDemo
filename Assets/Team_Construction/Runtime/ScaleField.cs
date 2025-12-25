using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace PXR.Construction.Runtime
{
    public class ScaleField : NetworkBehaviour
    {
        [Serializable]
        public struct FieldSet
        {
            public Vector3 Position;
            public Vector3 Scale;

            public FieldSet(Vector3 position, Vector3 scale)
            {
                Position = position;
                Scale = scale;
            }
        }

        /// <summary>
        /// Editor空間用フィールド設定
        /// </summary>
        [SerializeField]
        private FieldSet editorField;

        /// <summary>
        /// Simulator空間用フィールド設定
        /// </summary>
        [SerializeField]
        private FieldSet simulatorField;

        /// <summary>
        /// Editor空間用のアイテム群
        /// </summary>
        [SerializeField]
        private GameObject editorRoomItems;

        /// <summary>
        /// 
        /// </summary>
        private List<Transform> children = new List<Transform>();

        /// <summary>
        /// デバッグ用：UnityEditor上で実行した際Entranceから実行されるように変更
        /// </summary>
        private void Start()
        {
//#if UNITY_EDITOR
//            // UnityEditor上で実行した際Entranceから実行されるように変更
//            string[] guids = AssetDatabase.FindAssets("t:SceneAsset Entrance");
//            foreach (string guid in guids)
//            {
//                string path = AssetDatabase.GUIDToAssetPath(guids[0]);
//                SceneAsset scene = AssetDatabase.LoadAssetAtPath<SceneAsset>(path);
//                if(scene.name.EndsWith("Entrance"))
//                {
//                    EditorSceneManager.playModeStartScene = scene;
//                }
//            }
//#endif
            foreach (Transform child in this.transform)
            {
                children.Add(child);
            }
        }

        /// <summary>
        /// シミュレート切り替え
        /// </summary>
        /// <param name="isOn"></param>
        public void ToggleSimulate(bool isOn)
        {
            if(!IsOwner) { return; }

            ToggleSimulateServerRpc(isOn);
        }

        [ServerRpc(RequireOwnership = false)]
        private void ToggleSimulateServerRpc(bool isOn)
        {
            ToggleSimulateClientRpc(isOn);
        }

        [ClientRpc]
        private void ToggleSimulateClientRpc(bool isOn)
        {
            foreach (var child in children)
            {
                // Grab操作等で親子関係が変わっている可能性があるため、再度親子関係を設定し直す
                child.transform.parent = this.transform;

                // Editorモードのみ操作可能
                if (child.TryGetComponent<XRGrabInteractable>(out var xRGrab))
                {
                    xRGrab.enabled = !isOn;
                }
            }

            SetFieldSet(isOn ? simulatorField : editorField);

            editorRoomItems.SetActive(!isOn);
        }

        /// <summary>
        /// スケール変更
        /// </summary>
        /// <param name="set"></param>
        private void SetFieldSet(FieldSet set)
        {
            this.transform.localPosition = set.Position;
            this.transform.localScale = set.Scale;
        }
    }
}
