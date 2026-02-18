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

        /// <summary>
        /// 組み立て対象ID（親オブジェクトのタグから取得）
        /// </summary>
        private int? _cachedAssemblyTargetId = null;

        /// <summary>
        /// 組み立て対象IDを取得します（親オブジェクトのタグから判定）
        /// </summary>
        /// <remarks>
        /// 親オブジェクトに "AssemblyTarget_0", "AssemblyTarget_1" などのタグを設定してください
        /// </remarks>
        public int AssemblyTargetId
        {
            get
            {
                // キャッシュがあればそれを返す
                if (_cachedAssemblyTargetId.HasValue)
                {
                    return _cachedAssemblyTargetId.Value;
                }

                // 親オブジェクトを探索してタグから組み立て対象IDを取得
                Transform parent = transform.parent;
                while (parent != null)
                {
                    if (parent.tag.StartsWith("AssemblyTarget_"))
                    {
                        string idString = parent.tag.Replace("AssemblyTarget_", "");
                        if (int.TryParse(idString, out int id))
                        {
                            _cachedAssemblyTargetId = id;
                            return id;
                        }
                    }
                    parent = parent.parent;
                }

                // タグが見つからない場合はデフォルト値0を返す
                Debug.LogWarning($"GuideParts '{gameObject.name}' の親オブジェクトに 'AssemblyTarget_X' タグが見つかりません。デフォルト値0を使用します。", gameObject);
                _cachedAssemblyTargetId = 0;
                return 0;
            }
        }
    }
}
