using UnityEngine;
public class ItemProperty: MonoBehaviour
{
    /// <summary>
    /// 道具の属性
    /// </summary>
    public enum ItemAttribute
    {
        None,
        Erase,// 消す
        RunAway,// 逃げる
        Entertainment// エンタメ
    }


    [Tooltip("アイテムの重さ")]
    public float weight;

    [Tooltip("有効時獲得ポイント")]
    public float effectiveGetPoint;

    [Tooltip("無効時失効ポイント")]
    public float noEffectiveLosePoint;

    [Tooltip("アイテム属性")]
    public ItemAttribute itemAttribute;

}
