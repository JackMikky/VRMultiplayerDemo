using System;
using UnityEngine;

namespace Assets.Team_Prevention.Script
{
    /// <summary>
    /// アイテムのデータを保持するScriptableObject。
    /// Unity Editor で「Assets > Create > Team_Prevention > ItemData」から作成できます。
    /// </summary>
    [CreateAssetMenu(fileName = "NewItemData", menuName = "Team_Prevention/ItemData", order = 1)]
    public class ItemData : ScriptableObject
    {
        /// <summary>
        /// アイテム名
        /// </summary>
        [Tooltip("アイテムの名前（UI表示やログに使用）")]
        [SerializeField] private string _name;

        /// <summary>
        /// アイテム画像（UI表示用）
        /// </summary>
        [Tooltip("UIに表示するアイコン画像")]
        [SerializeField] private Texture2D _icon;

        /// <summary>
        /// このアイテム使用が正解とされる地点ID
        /// </summary>
        [Tooltip("このアイテムを使用すべき正しいスポットのID")]
        [SerializeField] private int _correctUseSpotId;

        /// <summary>
        /// 加算または減算されるポイント
        /// </summary>
        [Tooltip("正解時に加算されるポイント（誤答時は減算）")]
        [SerializeField] private int _point;

        // プロパティでアクセス
        public string Name
        {
            get { return _name; }
        }

        public Texture2D Icon
        {
            get { return _icon; }
        }

        public int CorrectUseSpotId
        {
            get { return _correctUseSpotId; }
        }

        public int Point
        {
            get { return _point; }
        }
    }
}

/// <summary>
/// プレイヤーが所持しているアイテムの情報
/// </summary>
[Serializable]
public class ItemInfo
{
    public string Name;
    public bool IsUsed;
}