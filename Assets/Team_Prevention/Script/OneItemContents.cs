using Assets.Team_Prevention.Script;   // ItemData 用の namespace
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class OneItemContents : MonoBehaviour
{
    [Header("見た目用の参照")]
    [SerializeField] private Image iconImage;     // Icon オブジェクト
    [SerializeField] private TMP_Text labelText;  // Label オブジェクト

    // この UI が表している“内容”
    private ItemData itemData;

    public ItemData ItemData => itemData;

    /// <summary>
    /// 外部からデータをセットする（ItemData 対応）
    /// </summary>
    public void SetData(ItemData data)
    {
        itemData = data;

        // ラベル反映
        if (labelText != null && data != null)
            labelText.text = data.Name;

        // アイコン反映（Texture2D → Sprite 変換）
        if (iconImage != null && data != null && data.Icon != null)
        {
            iconImage.sprite = Sprite.Create(
                data.Icon,
                new Rect(0, 0, data.Icon.width, data.Icon.height),
                new Vector2(0.5f, 0.5f)
            );
        }

        // ★ クリック関連の処理は削除済み ★
    }
}