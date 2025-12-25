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
    public class GuideParts : NetworkBehaviour
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
        /// 組み立て部品が接続されているかどうか
        /// </summary>
        public bool IsConnected = false;
    }
}
