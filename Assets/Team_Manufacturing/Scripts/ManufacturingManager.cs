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
        /// Unity:Start
        /// </summary>
        public override void OnNetworkSpawn()
		{
			// 初期化処理
			AssemblyManager.Init();
		}
	}
}