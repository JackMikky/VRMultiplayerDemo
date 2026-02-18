using System;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace Manufacturing
{
    /// <summary>
    /// 組み立て部品用クラス
    /// </summary>
    public class AssemblyParts : NetworkBehaviour
    {
        /// <summary>
        /// 組み立て部品種別
        /// </summary>
        public PartsType Type;

		/// <summary>
		/// 組み立て部品サイズ
		/// </summary>
		public PartsSize Size;

		/// <summary>
		/// オブジェクトを離した時に発火されるイベント
		/// </summary>
		[NonSerialized]
		public UnityEvent<AssemblyParts> OnReleaseEvent = new UnityEvent<AssemblyParts>();

        /// <summary>
        /// オブジェクトを離した時に発火されるイベント
        /// </summary>
        [NonSerialized]
        public UnityEvent<AssemblyParts> OnGrabEvent = new UnityEvent<AssemblyParts>();

        /// <summary>
        /// XRGrabInteractableのインスタンス
        /// </summary>
        public XRGrabInteractable XRGrabInteractable;

        /// <summary>
        /// Colliderのインスタンス
        /// </summary>
        public Collider Collider;

        /// <summary>
        /// 接続先のガイド用オブジェクトのGuidePartsインデックス
        /// </summary>
        public int ConnectGuidePartsIndex = -1;

        /// <summary>
        /// 接続先のガイド用オブジェクトのGuidePartsインデックス（ネットワーク上）
        /// </summary>
        public NetworkVariable<int> ConnectGuidePartsIndexNetwork = new NetworkVariable<int>(-1, writePerm: NetworkVariableWritePermission.Server);

        /// <summary>
        /// isKinematicの状態（ネットワーク上）
        /// </summary>
        public NetworkVariable<bool> IsKinematicNetwork = new NetworkVariable<bool>(false, writePerm: NetworkVariableWritePermission.Server);

        /// <summary>
        /// 初期座標
        /// </summary>
        private NetworkVariable<Vector3> initialPosition = new NetworkVariable<Vector3>(Vector3.zero, writePerm: NetworkVariableWritePermission.Server);

        /// <summary>
        /// 初期回転
        /// </summary>
        private NetworkVariable<Vector3> initialRotation = new NetworkVariable<Vector3>(Vector3.zero, writePerm: NetworkVariableWritePermission.Server);

        /// <summary>
        /// Unity:Awake
        /// </summary>
        public override void OnNetworkSpawn()
        {
			// インスタンスを取得
            XRGrabInteractable = GetComponent<XRGrabInteractable>();
            Collider = GetComponent<Collider>();

            // イベントに登録
            XRGrabInteractable.selectExited.AddListener(OnRelease);
			XRGrabInteractable.selectEntered.AddListener(OnGrab);

            // 初期座標・回転を保存
            if (IsOwner)
            {
                initialPosition.Value = transform.position;
                initialRotation.Value = transform.eulerAngles;
            }
        }

        /// <summary>
        /// Unity:OnDestroy
        /// </summary>
        public override void OnNetworkDespawn()
		{
			// イベントの登録解除
			if (XRGrabInteractable != null)
			{
                XRGrabInteractable.selectExited.RemoveListener(OnRelease);
				XRGrabInteractable.selectEntered.RemoveListener(OnGrab);
            }
		}

        /// <summary>
        /// オブジェクトを初期位置・回転にリセットします
        /// </summary>
        public void ResetObject()
        {
            transform.position = initialPosition.Value;
            transform.eulerAngles = initialRotation.Value;
        }

        /// <summary>
        /// オブジェクトを離した時に呼ばれます
        /// </summary>
        /// <param name="args"></param>
        void OnRelease(SelectExitEventArgs args)
		{
			// 掴んでいるinteractorの数が0より多いならreturn
			if (XRGrabInteractable.interactorsSelecting.Count > 0)
			{
				return;
			}

            Collider.isTrigger = false;

            OnReleaseServerRpc();
        }

        /// <summary>
        /// オブジェクトを離した時にサーバーで呼ばれます
        /// </summary>
        [Rpc(SendTo.Server)]
        void OnReleaseServerRpc()
        {
            // イベント発火
            OnReleaseEvent.Invoke(this);
        }

        /// <summary>
        /// オブジェクトを掴んだ時に呼ばれます
        /// </summary>
        /// <param name="args"></param>
        void OnGrab(SelectEnterEventArgs args)
        {
            Collider.isTrigger = true;

            // イベント発火
            OnGrabEvent.Invoke(this);
        }
    }
}
