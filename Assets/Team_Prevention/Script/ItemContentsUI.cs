using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Assets.Team_Prevention.Script;   // ItemData 用の namespace

public class ItemContentsUI : MonoBehaviour
{
    [Header("見た目用の参照")]
    [SerializeField] private Image iconImage;   // Icon オブジェクト
    [SerializeField] private TMP_Text labelText;   // Label オブジェクト

    // このボタンが表している“内容”
    private ItemData itemData;

    // クリックされたときに外から呼ばれるコールバック
    private System.Action<ItemData> onClickCallback;

    public ItemData ItemData => itemData;

    /// <summary>
    /// 外部からデータをセットする（ItemData 対応）
    /// </summary>
    public void SetData(ItemData data, System.Action<ItemData> onClick)
    {
        itemData = data;
        onClickCallback = onClick;

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

        // Button コンポーネントを取得してクリックイベント登録
        var button = GetComponent<Button>();
        if (button != null)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(OnClicked);
        }
        else
        {
            Debug.LogWarning($"{name} に Button コンポーネントが付いていません。");
        }
    }

    private void OnClicked()
    {
        // 自分が持っている ItemData をそのまま返す
        onClickCallback?.Invoke(itemData);
    }
}