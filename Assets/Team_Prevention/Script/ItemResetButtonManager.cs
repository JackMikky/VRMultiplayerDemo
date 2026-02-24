using UnityEngine;
using UnityEngine.UI;
using Assets.Team_Prevention.Script;
using System.Collections.Generic;

public class ItemResetButtonManager : MonoBehaviour
{
    [Tooltip("アイテム群を持つ GameObject")]
    public GameObject itemGroup;

    [Tooltip("PlayerInfoを指定")]
    public PlayerInfo playerInfo; // Inspectorで設定

    // 追加: ItemBoxComponent の参照
    [SerializeField] private Assets.Team_Prevention.Script.UI.ItemBoxComponent itemBoxComponent;

    // ★ ここから追加 ★
    [Header("インベントリ連携")]
    [SerializeField, Tooltip("アイテム選択フェーズのマネージャー（インベントリUIを管理しているクラス）")]
    private ItemSelectionPhaseManager itemSelectionPhaseManager;


    // アイテムの情報を記録するクラス
    [System.Serializable]
    public class ItemData
    {
        public Transform itemTransform;   // 元のアイテムのTransform
        public Vector3 position;         // 元の位置
        public Quaternion rotation;      // 元の回転
    }

    private List<ItemData> itemDataList = new List<ItemData>(); // アイテム情報を保存するリスト

    private void Start()
    {
        if (itemGroup != null)
        {
            // itemGroup内のすべての子オブジェクトを記録
            foreach (Transform item in itemGroup.transform)
            {
                ItemData data = new ItemData
                {
                    itemTransform = item,       // 元のTransformを保存
                    position = item.position,   // 元の位置を保存
                    rotation = item.rotation    // 元の回転を保存
                };
                itemDataList.Add(data);
            }
        }
        else
        {
            Debug.LogWarning("Item群のGameObjectが設定されていません。処理を行いません。");
        }

        // ボタンにクリックイベントを登録
        GetComponent<Button>().onClick.AddListener(OnButtonClicked);
    }

    private void OnButtonClicked()
    {
        Debug.Log("ボタンがクリックされました！");

        // アイテム群の子オブジェクトをリセット
        if (itemGroup != null)
        {
            foreach (ItemData data in itemDataList)
            {
                // アイテムをアクティブにする
                data.itemTransform.gameObject.SetActive(true);

                data.itemTransform.position = data.position;   // 元の位置を設定
                data.itemTransform.rotation = data.rotation;   // 元の回転を設定
                Debug.Log($"アイテムの位置と回転をリセットしました: {data.itemTransform.name}");
            }

            Debug.Log("アイテム群をリセットしました。");

            // HPをリセット（PlayerInfo）
            if (playerInfo != null)
            {
                playerInfo.ResetHPToMax();
                Debug.Log($"HPを最大値にリセットしました: {playerInfo.MaxHP}");
            }
            else
            {
                Debug.LogWarning("PlayerInfoが設定されていないため、HPをリセットできません。");
            }

            // ItemResetButtonManager に追加: PlayerInfo の全消去後の UI 更新
            if (playerInfo != null)
            {
                playerInfo.AllClear(); // PlayerInfo 側で ItemsList をクリアし、イベントを通知

                if (itemBoxComponent != null)
                {
                    itemBoxComponent.RefreshFromPlayer();
                    Debug.Log("ItemBoxComponent の所持品表示を全クリアしました。");
                }
            }

            // ★ ここから追加：ItemSelectionPhaseManager 側のインベントリも全クリア ★
            if (itemSelectionPhaseManager != null)
            {
                itemSelectionPhaseManager.ClearAllItems();
                Debug.Log("ItemSelectionPhaseManager 側のインベントリも全クリアしました。");
            }
            else
            {
                Debug.LogWarning("ItemSelectionPhaseManager が設定されていないため、インベントリUIをクリアできません。");
            }
            // ★ ここまで追加 ★
        }
        else
        {
            Debug.LogWarning("Item群のGameObjectが設定されていません。処理を行いません。");
        }
    }


    // ★ ここから追加 ★
    /// <summary>
    /// 特定のアイテムだけを元の位置・回転に戻す
    /// </summary>
    public void ResetOneItem(Transform targetTransform)
    {
        if (targetTransform == null)
        {
            Debug.LogWarning("[ItemResetButtonManager] ResetOneItem: targetTransform が null です。");
            return;
        }

        foreach (ItemData data in itemDataList)
        {
            if (data.itemTransform == targetTransform)
            {
                data.itemTransform.gameObject.SetActive(true);
                data.itemTransform.position = data.position;
                data.itemTransform.rotation = data.rotation;

                Debug.Log($"[ItemResetButtonManager] 単体アイテムをリセットしました: {data.itemTransform.name}");
                return;
            }
        }

        Debug.LogWarning($"[ItemResetButtonManager] ResetOneItem: 対象が itemDataList に見つかりませんでした: {targetTransform.name}");
    }
    // ★ ここまで追加 ★

}