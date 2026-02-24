namespace Assets.Team_Prevention.Script
{
    /// <summary>
    /// アイテム使用時に発動するエフェクトの種類
    /// </summary>
    public enum ItemEffectType
    {
        /// <summary>
        /// エフェクトなし
        /// </summary>
        None = 0,

        /// <summary>
        /// 炎を小さくする（スケールを縮小）
        /// </summary>
        ReduceFlame = 1,

        /// <summary>
        /// 煙を発生させる（パーティクル再生）
        /// </summary>
        GenerateSmoke = 2,

        /// <summary>
        /// 帆を小さくする（スケール縮小）
        /// </summary>
        ReduceSail = 3,

        /// <summary>
        /// 水を放出する（パーティクル再生）
        /// </summary>
        SprayWater = 4,

        /// <summary>
        /// オブジェクトを消す（非表示化）
        /// </summary>
        HideObject = 5,

        /// <summary>
        /// オブジェクトを表示する
        /// </summary>
        ShowObject = 6
    }
}