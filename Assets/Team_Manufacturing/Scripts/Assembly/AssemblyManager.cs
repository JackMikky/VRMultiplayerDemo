using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

namespace Manufacturing
{
	/// <summary>
	/// 組み立て処理管理用クラス
	/// </summary>
	public class AssemblyManager : NetworkBehaviour
	{
		/// <summary>
		/// 組み立て部品情報群
		/// </summary>
		private List<AssemblyParts> AssemblyPartsList = new List<AssemblyParts>();

        /// <summary>
        /// ガイド用部品情報群
        /// </summary>
        private List<GuideParts> GuidePartsList = new List<GuideParts>();

        /// <summary>
        /// オブジェクトどうしをくっつける距離の閾値
        /// </summary>
        private readonly float _connectDistance = 0.2f;

		/// <summary>
		/// 初期化処理
		/// </summary>
		public void Init(
			GameObject actionableObject,
			List<GameObject> playerDesks,
            List<GameObject> operatorDesks)
		{
			// ユーザーが掴むことのできるオブジェクト群を取得
			{
				AssemblyParts[] assemblyPartsArray = actionableObject.GetComponentsInChildren<AssemblyParts>();
				if (assemblyPartsArray.Length > 0)
				{
					foreach (AssemblyParts parts in assemblyPartsArray)
					{
						// イベントに登録
						parts.OnReleaseEvent.AddListener(OnRelease);
						parts.OnGrabEvent.AddListener(OnGrab);

						// リストに追加
						AssemblyPartsList.Add(parts);
					}
				}
			}

			// ガイド用オブジェクト群を取得
			{
				GuideParts[] guidePartsArray = actionableObject.GetComponentsInChildren<GuideParts>();
				if (guidePartsArray.Length > 0)
				{
					foreach(GuideParts parts in guidePartsArray)
					{
						GuidePartsList.Add(parts);
					}
				}
			}
		}                        

		/// <summary>
		/// オブジェクトを離した時に呼び出されます
		/// </summary>
		/// <param name="assemblyParts"></param>
		private void OnRelease(AssemblyParts assemblyParts)
		{
			Debug.Log($"離された組み立て部品種別：{assemblyParts.Type}");
			int index = AssemblyPartsList.FindIndex(x => x == assemblyParts);
			if (AssemblyPartsList[index] == null)
			{
				Debug.LogError("離されたオブジェクトが見つかりません。");
				return;
            }

            // サーバー側で処理を実行
            OnReleaseServerRpc(index);
        }

        /// <summary>
        /// オブジェクトを離した時にサーバー側で呼び出されます
        /// </summary>
        [Rpc(SendTo.Server)]
        void OnReleaseServerRpc(int releasePartsIndex)
        {
            // 離されたオブジェクト情報を取得
            var releaseParts = AssemblyPartsList[releasePartsIndex];

            // 接続先のガイド用オブジェクトの接続情報をクリア
            if (releaseParts.ConnectGuidePartsIndex >= 0)
            {
                GuidePartsList[releaseParts.ConnectGuidePartsIndex].IsConnected = false;

                releaseParts.ConnectGuidePartsIndex = -1;
                releaseParts.ConnectGuidePartsIndexNetwork.Value = -1;
            }

            // 離されたオブジェクトのGameObjectとそのRigidbodyを取得
            GameObject releaseObject = releaseParts.gameObject;
            Rigidbody rigidbody = releaseObject.GetComponent<Rigidbody>();

            // 一定距離以内のオブジェクトを入れとくリスト
            List<NearObjectInfo> nearObjects = new List<NearObjectInfo>();

            Vector3 releaseObjectPosition = releaseObject.transform.position;

            // 離されたオブジェクトと一定以内の距離のガイド用オブジェクトを列挙
            foreach (var guideParts in GuidePartsList)
            {
                // 種別とサイズが一致しなければスキップ
                if (guideParts.Type != releaseParts.Type || guideParts.Size != releaseParts.Size)
                {
                    continue;
                }

                // 既に接続されているガイド用オブジェクトはスキップ
                if (guideParts.IsConnected)
                {
                    continue;
                }

                // 距離を計算して一定距離以内であればリストに追加
                float distance = Vector3.Distance(releaseObjectPosition, guideParts.transform.position);
                if (distance < _connectDistance)
                {
                    NearObjectInfo nearObject = new NearObjectInfo(guideParts, distance);
                    nearObjects.Add(nearObject);
                }
            }

            // 無ければ物理演算を適用
            if (nearObjects.Count <= 0)
            {
                rigidbody.isKinematic = false;
                rigidbody.useGravity = true;

                releaseParts.IsKinematicNetwork.Value = false;
                releaseParts.UseGravityNetwork.Value = true;

                // サーバー以外に通知
                OnReleaseRpc(releasePartsIndex, -1, false, true);

                return;
            }

            // 一番近いオブジェクト情報を取得
            NearObjectInfo objectInfo = nearObjects.OrderBy(obj => obj.Distance).First();

            // 一番近いガイド用オブジェクトにはめる
            ConnectObject(releaseParts, releaseObject, objectInfo.NearObject.gameObject);

            // 接続されているフラグをtrueに
            objectInfo.NearObject.IsConnected = true;

            // 接続されているガイドのインデックスを取得
            int nearPartsIndex = GuidePartsList.FindIndex(x => x == objectInfo.NearObject);

            // 接続先のガイド用オブジェクト情報を保存
            releaseParts.ConnectGuidePartsIndex = nearPartsIndex;
            releaseParts.ConnectGuidePartsIndexNetwork.Value = nearPartsIndex;

            Debug.Log($"サーバー側…isKinematic：{releaseObject.GetComponent<Rigidbody>().isKinematic}, useGravity：{releaseObject.GetComponent<Rigidbody>().useGravity}");

            // サーバー以外に通知
            OnReleaseRpc(releasePartsIndex, nearPartsIndex, true, false);
        }

        /// <summary>
        /// オブジェクトを離した時にサーバー以外で呼び出されます
        /// </summary>
        [Rpc(SendTo.Everyone)]
        void OnReleaseRpc(int releasePartsIndex, int nearPartsIndex, bool isKinematic, bool useGravity)
        {
            // 組み立て部品・ガイドの情報を取得
            var releaseParts = AssemblyPartsList[releasePartsIndex];
            var nearParts = nearPartsIndex >= 0 ? GuidePartsList[nearPartsIndex] : null;

            releaseParts.ConnectGuidePartsIndex = nearPartsIndex;
            Rigidbody rigidbody = releaseParts.GetComponent<Rigidbody>();
            rigidbody.isKinematic = isKinematic;
            rigidbody.useGravity = useGravity;

            if (nearParts != null)
            {
                nearParts.IsConnected = true;
            }

            Debug.Log($"クライアント側…isKinematic：{rigidbody.isKinematic}, useGravity：{rigidbody.useGravity}");
        }

        /// <summary>
        /// オブジェクトを掴んだ時に呼び出されます
        /// </summary>
        /// <param name="type"></param>
        private void OnGrab(AssemblyParts assemblyParts)
		{
            Debug.Log($"掴んだ組み立て部品種別：{assemblyParts.Type}");
        }

        /// <summary>
        /// 一定以下の距離のオブジェクトに指定のオブジェクトを接続
        /// </summary>
        private void ConnectObject(AssemblyParts releaseParts, GameObject releaseObject, GameObject guideObject)
		{
            releaseObject.transform.position = guideObject.transform.position;
            releaseObject.transform.rotation = guideObject.transform.rotation;

            Rigidbody rigidbody = releaseObject.GetComponent<Rigidbody>();
			rigidbody.useGravity = false;
			rigidbody.linearVelocity = Vector3.zero;
			rigidbody.angularVelocity = Vector3.zero;
			rigidbody.isKinematic = true;

            // ネットワーク変数も更新
            releaseParts.IsKinematicNetwork.Value = true;
            releaseParts.UseGravityNetwork.Value = false;
        }
    }


	/// <summary>
	/// 一定距離以内の物体を保存するインナークラス
	/// </summary>
	class NearObjectInfo
	{
		public GuideParts NearObject;

		public float Distance;

		public NearObjectInfo(GuideParts nearObject, float distance)
		{
            NearObject = nearObject;
			Distance = distance;
        }
	}
}
