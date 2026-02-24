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

        [Header("エフェクト設定")]
        /// <summary>
        /// このアイテム使用時に発動するエフェクトの種類
        /// </summary>
        [Tooltip("このアイテムを正しいスポットで使用したときに発動するエフェクト")]
        [SerializeField] private ItemEffectType _effectType = ItemEffectType.None;

        /// <summary>
        /// エフェクトのターゲット名（複数のオブジェクトから特定する場合に使用）
        /// </summary>
        [Tooltip("エフェクト対象のGameObject名（例: 'Flame01', 'Sail'）")]
        [SerializeField] private string _effectTargetName = "";

        /// <summary>
        /// エフェクトの強度（0.0 ～ 1.0）
        /// </summary>
        [Tooltip("エフェクトの強度（例: 縮小率、煙の量など）")]
        [Range(0f, 1f)]
        [SerializeField] private float _effectIntensity = 1.0f;

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

        public ItemEffectType EffectType
        {
            get { return _effectType; }
        }

        public string EffectTargetName
        {
            get { return _effectTargetName; }
        }

        public float EffectIntensity
        {
            get { return _effectIntensity; }
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