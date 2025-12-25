using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace PXR.Construction.Runtime
{
    public class PortalManager : MonoBehaviour
    {
        /// <summary>
        /// XROrigin
        /// </summary>
        [SerializeField] private XROrigin xrOrigin;

        /// <summary>
        /// ポータルを見せるターゲット
        /// </summary>
        [SerializeField] private Transform targetTransform;

        /// <summary>
        /// TeleportationProvider
        /// </summary>
        [SerializeField] private TeleportationProvider teleportationProvider;

        /// <summary>
        /// ポータルAlpha
        /// </summary>
        [SerializeField] private PortalController portalAlpha;

        /// <summary>
        /// ポータルBeta
        /// </summary>
        [SerializeField] private PortalController portalBeta;

        /// <summary>
        /// ポータル当たり判定の大きさ
        /// </summary>
        [SerializeField] private Vector2 portalSize = new Vector2(1, 3);

        private void Awake()
        {
            portalAlpha.Init
                (
                    xrOrigin,
                    targetTransform,
                    teleportationProvider,
                    portalBeta,
                    portalSize
                );

            portalBeta.Init
                (
                    xrOrigin,
                    targetTransform,
                    teleportationProvider,
                    portalAlpha,
                    portalSize
                );
        }

#if UNITY_EDITOR
        public void UpdatePortalsSize()
        {
            portalAlpha.UpdatePortalSize(portalSize);
            portalBeta.UpdatePortalSize(portalSize);
        }
#endif
    }

#if UNITY_EDITOR
    /// <summary>
    /// デバッグ用Editor拡張
    /// </summary>
    [CustomEditor(typeof(PortalManager))]
    public class PortalManagerGUI : Editor
    {
        // TODO : スクリプト分割
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            EditorGUILayout.Space();

            var targetComponent = target as PortalManager;

            if (GUILayout.Button("UpdatePortalsSize"))
            {
                targetComponent.UpdatePortalsSize();
            }
        }
    }
#endif
}