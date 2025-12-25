using CFaz.OffAxisCamera;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation;
using Unity.XR.CoreUtils;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace PXR.Construction.Runtime
{
    public class PortalController : MonoBehaviour
    {
        /// <summary>
        /// XROrigin
        /// </summary>
        private XROrigin xrOrigin;

        /// <summary>
        /// ポータルを見せるターゲット
        /// </summary>
        private Transform targetTransform;

        /// <summary>
        /// TeleportationProvider
        /// </summary>
        private TeleportationProvider teleportationProvider;

        /// <summary>
        /// ペアとなるポータル
        /// </summary>
        private PortalController pairPortal;

        /// <summary>
        /// 自身のUnity標準カメラ
        /// </summary>
        [SerializeField] private Camera unityCamera;

        /// <summary>
        /// 自身のOffAxisCamera
        /// </summary>
        [SerializeField] private OffAxisCamera offAxisCamera;

        /// <summary>
        /// 自身のmeshRenderer
        /// </summary>
        [SerializeField] private MeshRenderer meshRenderer;

        /// <summary>
        /// 複製元material
        /// </summary>
        [SerializeField] private Material materialOrigin;

        /// <summary>
        /// メインMeshのTransform
        /// </summary>
        [SerializeField] private Transform meshTransform;

        /// <summary>
        /// 飾りMeshのTransform
        /// </summary>
        [SerializeField] private Transform decorationTransform;

        /// <summary>
        /// ペアのカメラ描画先renderTexture
        /// </summary>
        private RenderTexture renderTexture;

        /// <summary>
        /// ポータル当たり判定の大きさ
        /// </summary>
        private Vector2 portalSize;

        /// <summary>
        /// 通り抜け可能状態かどうか
        /// </summary>
        bool isReadyThrough = false;

        /// <summary>
        /// 初期化
        /// </summary>
        public void Init(XROrigin xrOrigin,
                         Transform target,
                         TeleportationProvider teleportationProvider,
                         PortalController pair,
                         Vector2 size)
        {
            this.xrOrigin = xrOrigin;
            this.targetTransform = target;
            this.teleportationProvider = teleportationProvider;
            this.pairPortal = pair;
            UpdatePortalSize(size);

            // 自身に適用するmaterial
            var material = Instantiate(materialOrigin);

            // materialに適用するrenderTexture
            renderTexture = new RenderTexture((int)(512 * portalSize.x), (int)(512 * portalSize.y), 1, UnityEngine.Experimental.Rendering.GraphicsFormat.R8G8B8A8_UNorm); // TODO : サイズに応じた解像度設定
            renderTexture.enableRandomWrite = true;
            renderTexture.Create();
            material.SetTexture("_BaseMap", renderTexture);

            // 自身のマテリアルは自身に設定
            this.meshRenderer.material = material;

            // renderTextureはペアのカメラ画像を使用
            pairPortal.unityCamera.targetTexture = renderTexture;
        }

        /// <summary>
        /// Update
        /// </summary>
        private void Update()
        {
            UpdatePairPortal();
            CheckTeleport();
        }

        /// <summary>
        /// ペアのポータル用Cameraを更新します
        /// </summary>
        private void UpdatePairPortal()
        {
            var offset = targetTransform.position - this.transform.position;
            pairPortal.UpdatePovLocal(Quaternion.Inverse(this.transform.localRotation) * offset);
        }

        /// <summary>
        /// テレポート判定処理
        /// </summary>
        private void CheckTeleport()
        {
            if (!IsPointWithinPortalFrame(targetTransform.position, out float dot))
            {
                isReadyThrough = false;
                return;
            }

            if (isReadyThrough && // 前回フレームで通り抜けてもよいと判断されている
                dot > 0f)        // 通り抜けた位置（裏側）に存在
            {
                Teleport();
            }

            // ポータルの正面側にいるなら次フレーム通り抜け可能
            isReadyThrough = dot < 0;
        }

        /// <summary>
        /// ポータルの枠を延長した先に点があるかを判定します
        /// </summary>
        /// <param name="pt"></param>
        /// <param name="dot"></param>
        /// <returns></returns>
        private bool IsPointWithinPortalFrame(Vector3 pt, out float dot)
        {
            var tf = this.transform;
            dot = 0;

            (var prjPos, float dist) = MathUtil.CalcProjectPoint(pt, tf.position, tf.right);

            // X方向チェック：ポータルのZY平面との距離が離れすぎていないか
            if (dist > portalSize.x / 2f)
            {
                return false;
            }

            // Y方向チェック：ポータルのZY平面との距離が離れすぎていないか
            if (prjPos.y < 0 || prjPos.y > portalSize.y)
            {
                return false;
            }

            // 符号付きポータルZ方向距離
            dot = MathUtil.CalcProjectPointRatio(pt, tf.position, tf.forward);

            return true;
        }

        /// <summary>
        /// OffAxisCameraのPOVを更新
        /// </summary>
        /// <param name="offset"></param>
        public void UpdatePovLocal(Vector3 offset)
        {
            this.offAxisCamera.PointOfViewLocal = offset;
        }

        /// <summary>
        /// テレポート
        /// </summary>
        public void Teleport()
        {
            // Position
            Vector3 newPos;
            {
                var pos = targetTransform.position;
                pos.y = this.transform.position.y - this.portalSize.y * 0.5f;
                Vector3 localPos = this.transform.InverseTransformPoint(pos); // TODO : ここの計算はPortalの回転がY軸回転のみであることを前提としている
                Vector3 flippedLocalPos = new Vector3(-localPos.x, localPos.y, -localPos.z);
                newPos = pairPortal.transform.TransformPoint(flippedLocalPos);
            }

            // Rotation
            Quaternion newRot;
            {
                // 入口ポータルのローカル空間でのオブジェクトの向き（Y軸回転だけ）
                Quaternion localRot = Quaternion.Inverse(this.transform.rotation) * Quaternion.Euler(0, targetTransform.rotation.eulerAngles.y, 0);
                Quaternion flippedLocalRot = Quaternion.Euler(0, 180, 0) * localRot;
                newRot = pairPortal.transform.rotation * flippedLocalRot;
            }

            // テレポート要求
            TeleportRequest request = new TeleportRequest
            {
                destinationPosition = newPos,
                destinationRotation = newRot,
                matchOrientation = MatchOrientation.TargetUpAndForward
            };

            teleportationProvider.QueueTeleportRequest(request);
        }

        /// <summary>
        /// ポータルサイズを更新します
        /// </summary>
        /// <param name="size"></param>
        public void UpdatePortalSize(Vector2 size)
        {
            this.portalSize = size;
            meshTransform.localScale = new Vector3(size.x, size.y, 1);
            decorationTransform.localScale = new Vector3(size.x + 0.1f, size.y + 0.1f, 0.1f);
            offAxisCamera.PlaneSize = size;
        }

        /// <summary>
        /// リソースの破棄
        /// </summary>
        private void OnDestroy()
        {
            renderTexture.Release();
        }
    }


#if UNITY_EDITOR
    /// <summary>
    /// デバッグ用Editor拡張
    /// </summary>
    [CustomEditor(typeof(PortalController))]
    public class PortalControllerGUI : Editor
    {
        // TODO : スクリプト分割
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            EditorGUILayout.Space();

            var targetComponent = target as PortalController;

            if (GUILayout.Button("Teleport"))
            {
                targetComponent.Teleport();
            }
        }
    }
#endif
}