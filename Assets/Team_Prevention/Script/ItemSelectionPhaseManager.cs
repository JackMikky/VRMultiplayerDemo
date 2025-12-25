using UnityEngine;
using System.Collections.Generic;
using Assets.Team_Prevention.Script;

/// <summary>
/// アイテム選択フェーズを管理するマネージャー。
/// コントローラー/手との衝突を検知してアイテムを取得します。
/// </summary>
public class ItemSelectionPhaseManager : MonoBehaviour
{
    #region 関連object
    [Tooltip("左コントローラー")]
    public GameObject leftController;

    [Tooltip("右コントローラー")]
    public GameObject rightController;

    [Tooltip("左手")]
    public GameObject leftHand;

    [Tooltip("右手")]
    public GameObject rightHand;

    [Tooltip("アイテムを含むコンテナ")]
    public GameObject itemsContainer;

    [Tooltip("プレイヤー情報（アイテムを追加する対象）")]
    public PlayerInfo targetPlayer;

    [Header("フォールバック用")]
    [Tooltip("アイテムデータカタログ（ItemDataHolderがない場合の名前検索用）")]
    public List<ItemData> itemCatalog;
    #endregion 関連object

    private List<GameObject> hiddenItems = new List<GameObject>();

    void Update()
    {
        CheckCollisionAndRemoveItems();
    }

    /// <summary>
    /// コントローラー/手との衝突をチェックしてアイテムを隠す
    /// </summary>
    private void CheckCollisionAndRemoveItems()
    {
        foreach (Transform item in itemsContainer.transform)
        {
            if (item.gameObject.activeSelf && IsCollidingWithControllerOrHand(item.gameObject))
            {
                // アイテムを隠す
                hiddenItems.Add(item.gameObject);
                item.gameObject.SetActive(false);

                // PlayerInfoのItemsListに追加
                AddItemToPlayer(item.gameObject);
            }
        }
    }

    /// <summary>
    /// 隠したアイテムをPlayerInfoのItemsListに追加
    /// </summary>
    /// <param name="item">隠したアイテムのGameObject</param>
    private void AddItemToPlayer(GameObject item)
    {
        if (targetPlayer == null)
        {
            Debug.LogWarning("[ItemSelectionPhaseManager] PlayerInfo (targetPlayer) is not assigned.");
            return;
        }

        ItemData itemData = null;

        // 優先: ItemDataHolderコンポーネントから取得
        var holder = item.GetComponent<ItemDataHolder>();
        if (holder != null && holder.ItemData != null)
        {
            itemData = holder.ItemData;
            Debug.Log($"[ItemSelectionPhaseManager] ItemData取得成功（ItemDataHolder）: {itemData.Name}");
        }
        else
        {
            // フォールバック: GameObjectの名前からカタログ検索
            itemData = FindItemDataByName(item.name);
            
            if (itemData != null)
            {
                Debug.LogWarning($"[ItemSelectionPhaseManager] ItemData取得（カタログ検索）: {itemData.Name} - ItemDataHolderの設定を推奨します");
            }
        }

        // ItemDataが取得できなかった場合
        if (itemData == null)
        {
            Debug.LogError($"[ItemSelectionPhaseManager] ItemDataが見つかりません: {item.name}");
            return;
        }

        // PlayerInfoのGetItemを呼び出してアイテムを追加
        bool success = targetPlayer.GetItem(itemData);
        
        if (success)
        {
            Debug.Log($"[ItemSelectionPhaseManager] アイテムをインベントリに追加: {itemData.Name}");
        }
        else
        {
            Debug.LogWarning($"[ItemSelectionPhaseManager] アイテムの追加に失敗: {itemData.Name}（インベントリが満杯または重複）");
        }
    }

    /// <summary>
    /// 名前一致でItemDataを取得（フォールバック用）
    /// </summary>
    /// <param name="name">アイテム名</param>
    /// <returns>一致するItemData、見つからない場合はnull</returns>
    private ItemData FindItemDataByName(string name)
    {
        if (itemCatalog == null || string.IsNullOrEmpty(name))
        {
            return null;
        }

        // GameObjectの名前には "(Clone)" などが付く場合があるので、部分一致も考慮
        string cleanName = name.Replace("(Clone)", "").Trim();

        for (int i = 0; i < itemCatalog.Count; i++)
        {
            var data = itemCatalog[i];
            if (data != null && (data.Name == name || data.Name == cleanName))
            {
                return data;
            }
        }

        return null;
    }

    private bool IsCollidingWithControllerOrHand(GameObject item)
    {
        // アイテムがコントローラーや手と重なっているかをチェックする
        return IsColliding(item, leftController) || IsColliding(item, rightController) ||
               IsColliding(item, leftHand) || IsColliding(item, rightHand);
    }

    private bool IsColliding(GameObject item, GameObject target)
    {
        if (target == null)
        {
            return false;
        }

        // 距離が近いと重なっているとみなす
        float distanceThreshold = 0.1f; // しきい値を調整可能
        return Vector3.Distance(item.transform.position, target.transform.position) < distanceThreshold;
    }
}
