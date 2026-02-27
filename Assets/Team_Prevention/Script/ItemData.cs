using System;
using UnityEngine;
using Assets.Team_Prevention.Script;

namespace Assets.Team_Prevention.Script
{
    [CreateAssetMenu(fileName = "NewItemData", menuName = "Team_Prevention/ItemData", order = 1)]
    public class ItemData : ScriptableObject
    {
        /// <summary>SpawnMode ごとの生成姿勢オフセット</summary>
        [Serializable]
        public class SpawnPoseOffset
        {
            [Tooltip("生成位置のローカルオフセット（spawnBase 基準）")]
            public Vector3 localPositionOffset = Vector3.zero;

            [Tooltip("生成回転のローカルオフセット（Euler / spawnBase 基準）")]
            public Vector3 localRotationOffsetEuler = Vector3.zero;
        }

        /// <summary>アイテム名</summary>
        [Tooltip("アイテムの名前（UI表示やログに使用）")]
        [SerializeField] private string _name;

        /// <summary>アイテム画像（UI表示用）</summary>
        [Tooltip("UIに表示するアイコン画像")]
        [SerializeField] private Texture2D _icon;

        /// <summary>このアイテム使用が正解とされる地点ID</summary>
        [Tooltip("このアイテムを使用すべき正しいスポットのID")]
        [SerializeField] private int _correctUseSpotId;

        /// <summary>加算または減算されるポイント</summary>
        [Tooltip("正解時に加算されるポイント（誤答時は減算）")]
        [SerializeField] private int _point;

        [Header("エフェクト設定")]

        /// <summary>このアイテム使用時に発動するエフェクトの種類</summary>
        [Tooltip("このアイテムを正しいスポットで使用したときに発動するエフェクト")]
        [SerializeField] private ItemEffectType _effectType = ItemEffectType.None;

        /// <summary>エフェクトのターゲット名</summary>
        [Tooltip("エフェクト対象のGameObject名（例: 'Flame01', 'Sail'）")]
        [SerializeField] private string _effectTargetName = "";

        /// <summary>エフェクトの強度（0.0 ～ 1.0）</summary>
        [Tooltip("エフェクトの強度（例: 縮小率、煙の量など）")]
        [Range(0f, 1f)]
        [SerializeField] private float _effectIntensity = 1.0f;

        [Header("生成方式")]

        /// <summary>
        /// このアイテムをどの方式で生成するか。
        /// FollowHand=手元追従 / FollowBody=身体固定追従 / WorldFixed=ワールド固定
        /// </summary>
        [Tooltip("生成方式を選択します。\nFollowHand: 手元追従\nFollowBody: 身体固定追従（腰など）\nWorldFixed: ワールド固定")]
        [SerializeField] private ItemUseSpawner.SpawnMode _spawnMode = ItemUseSpawner.SpawnMode.FollowHand;

        [Header("FollowBody アタッチポイント設定")]

        /// <summary>
        /// FollowBody 時に親として使用する身体 Transform の GameObject 名。
        /// タグが未設定または見つからない場合に使用します。
        /// XR Rig のルートから子孫を名前で再帰検索します。
        /// </summary>
        [Tooltip("FollowBody 時のアタッチ先を GameObject 名で指定します（例: 'WaistAttachPoint'）。\nタグ未設定 or タグ検索失敗時に XR Rig ルートから再帰検索します。")]
        [SerializeField] private string _bodyAttachPointName = "";

        [Header("生成姿勢オフセット（SpawnMode ごと）")]

        /// <summary>FollowHand 時の生成姿勢オフセット</summary>
        [Tooltip("FollowHand（手元追従）時の位置・回転オフセット")]
        [SerializeField] private SpawnPoseOffset _followHandOffset = new SpawnPoseOffset();

        /// <summary>FollowBody 時の生成姿勢オフセット</summary>
        [Tooltip("FollowBody（身体追従）時の位置・回転オフセット")]
        [SerializeField] private SpawnPoseOffset _followBodyOffset = new SpawnPoseOffset();

        /// <summary>WorldFixed 時の生成姿勢オフセット</summary>
        [Tooltip("WorldFixed（ワールド固定）時の位置・回転オフセット")]
        [SerializeField] private SpawnPoseOffset _worldFixedOffset = new SpawnPoseOffset();

        // ===== プロパティ =====

        public string Name => _name;
        public Texture2D Icon => _icon;
        public int CorrectUseSpotId => _correctUseSpotId;
        public int Point => _point;
        public ItemEffectType EffectType => _effectType;
        public string EffectTargetName => _effectTargetName;
        public float EffectIntensity => _effectIntensity;
        public ItemUseSpawner.SpawnMode SpawnMode => _spawnMode;

        /// <summary>
        /// FollowBody 時のアタッチ先名。タグが未設定または見つからない場合に使用。
        /// </summary>
        public string BodyAttachPointName => _bodyAttachPointName;

        /// <summary>
        /// 指定した SpawnMode に対応するオフセットを返します。
        /// </summary>
        public SpawnPoseOffset GetSpawnPoseOffset(ItemUseSpawner.SpawnMode mode)
        {
            switch (mode)
            {
                case ItemUseSpawner.SpawnMode.FollowBody:
                    return _followBodyOffset;
                case ItemUseSpawner.SpawnMode.WorldFixed:
                    return _worldFixedOffset;
                case ItemUseSpawner.SpawnMode.FollowHand:
                default:
                    return _followHandOffset;
            }
        }

        // ===== 後方互換プロパティ（既存の呼び出しが残っている場合に備えて残す） =====

        /// @deprecated GetSpawnPoseOffset(SpawnMode.FollowHand) を使用してください
        public Vector3 SpawnLocalPositionOffset => _followHandOffset.localPositionOffset;
        /// @deprecated GetSpawnPoseOffset(SpawnMode.FollowHand) を使用してください
        public Vector3 SpawnLocalRotationOffsetEuler => _followHandOffset.localRotationOffsetEuler;
        /// @deprecated GetSpawnPoseOffset(SpawnMode.FollowBody) を使用してください
        public Vector3 BodySpawnLocalPositionOffset => _followBodyOffset.localPositionOffset;
        /// @deprecated GetSpawnPoseOffset(SpawnMode.FollowBody) を使用してください
        public Vector3 BodySpawnLocalRotationOffsetEuler => _followBodyOffset.localRotationOffsetEuler;
    }
}

/// <summary>プレイヤーが所持しているアイテムの情報</summary>
[Serializable]
public class ItemInfo
{
    public string Name;
    public bool IsUsed;
}