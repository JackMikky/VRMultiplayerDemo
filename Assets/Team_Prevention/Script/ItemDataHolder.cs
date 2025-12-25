using Assets.Team_Prevention.Script;
using UnityEngine;



/// <summary>
/// GameObjectとItemData（ScriptableObject）を紐付けるコンポーネント。
/// シーン内のアイテムGameObjectにアタッチして使用します。
/// </summary>
public class ItemDataHolder : MonoBehaviour
{
    /// <summary>
    /// このGameObjectが表すアイテムのデータ（ScriptableObject）
    /// </summary>
    [Tooltip("このアイテムのデータを設定してください")]
    [SerializeField] private ItemData _itemData;

    /// <summary>
    /// アイテムデータへの参照を取得
    /// </summary>
    public ItemData ItemData
    {
        get { return _itemData; }
    }

    private void OnValidate()
    {
        // Editorでの設定ミスを防ぐ警告
        if (_itemData == null)
        {
            Debug.LogWarning($"[ItemDataHolder] ItemDataが設定されていません: {gameObject.name}", this);
        }
    }
}