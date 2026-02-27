using UnityEngine;

namespace Assets.Team_Prevention.Script.Effects
{
    /// <summary>
    /// 汎用エフェクト再生インターフェース。
    /// Play() 実行後、エフェクトが完了するまでの想定秒数を返す。
    /// </summary>
    public interface IEffectPlayer
    {
        /// <summary>
        /// エフェクトを再生する。完了までの想定秒数を返す（再生しない場合は0）。
        /// </summary>
        float Play();
    }
}