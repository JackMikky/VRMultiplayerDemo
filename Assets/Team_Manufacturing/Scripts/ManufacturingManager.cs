using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace Manufacturing
{
	/// <summary>
	/// Manufacturingシーン管理用クラス
	/// </summary>
	public class ManufacturingManager : NetworkBehaviour
	{
		/// <summary>
		/// 組み立て処理管理用クラス
		/// </summary>
		[SerializeField] private AssemblyManager AssemblyManager;

		/// <summary>
		/// ユーザーが操作できるオブジェクトの親Transform
		/// </summary>
		[SerializeField] private GameObject ActionableObject;

		/// <summary>
		/// プレイヤー用デスクの親GameObject
		/// </summary>
		[SerializeField] private List<GameObject> PlayerDesks;

        /// <summary>
        /// オペレーター用デスクの親GameObject
        /// </summary>
        [SerializeField] private List<GameObject> OperatorDesks;

        /// <summary>
        /// Unity:Start
        /// </summary>
        public override void OnNetworkSpawn()
		{
			// 初期化処理
			AssemblyManager.Init(ActionableObject, PlayerDesks, OperatorDesks);
		}
	}
}