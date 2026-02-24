using Assets.Team_Prevention.Script;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

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
    public MainPlayer targetPlayer;

    [Header("フォールバック用")]
    [Tooltip("アイテムデータカタログ（ItemDataHolderがない場合の名前検索用）")]
    public List<ItemData> itemCatalog;

    [Header("インベントリUI")]
    [Tooltip("ScrollView の Content（ItemContents を並べる親）")]
    public Transform itemListContent;

    [Tooltip("OneItemContents のプレハブ（クリックなし表示用）")]
    public OneItemContents itemContentsPrefab;

    [Header("Inventory Buttons")]
    public Button removeButton;
    public Button useButton;
    public Button upButton;
    public Button downButton;
    #endregion 関連object

    // 現在選択中のアイテム
    private ItemData _selectedItem;

    // UI リストと選択インデックス
    private List<OneItemContents> uiItemList = new List<OneItemContents>();
    private int selectedIndex = -1;

    private List<GameObject> hiddenItems = new List<GameObject>();

    // === 選択中の点滅ハイライト用 ===
    private Coroutine _blinkCo = null;
    private Image _blinkTarget = null;      // 現在点滅させている Image
    private Color _originalColor = Color.white;

    [Header("選択ハイライト設定")]
    [SerializeField, Tooltip("選択中の点滅色（薄い青）")]
    private Color _blinkColor = new Color(0.6f, 0.8f, 1f, 1f);

    [SerializeField, Tooltip("点滅の明滅周期（秒）※小さいほど速い")]
    private float _blinkPeriod = 0.8f;

    [SerializeField, Tooltip("点滅の最小アルファ(0〜1)")]
    private float _blinkMinAlpha = 0.35f;

    [SerializeField, Tooltip("点滅の最大アルファ(0〜1)")]
    private float _blinkMaxAlpha = 1.0f;

    [Header("アイテムリセット連携")]
    [SerializeField, Tooltip("アイテムの元位置情報を持っているリセットマネージャー")]
    private ItemResetButtonManager itemResetButtonManager;


    // 取得した ItemData とシーン上の元オブジェクトの紐付け
    private Dictionary<ItemData, Transform> worldItemMap = new Dictionary<ItemData, Transform>();


    // ★ 追加: 保持上限
    private const int MaxKeep = 5;

    // ★ 追加: すでに Use 済みの SpotId を記録する
    private HashSet<int> _usedSpotIds = new HashSet<int>();


    private void OnDestroy()
    {
        ClearAllItems();
    }

    private void Start()
    {
        // XR Origin (XR Rig) オブジェクトを取得
        var xrOrigin = GameObject.Find("XR Origin Hands (XR Rig) MP Template Variant Customized");
        if (xrOrigin == null)
        {
            Debug.LogError("[ItemSelectionPhaseManager] XR Origin が見つかりません。UI探索をスキップします。");
        }
        else
        {
            // UIルート(Canvas)は XR Origin 直下にある前提（画像どおり）
            Transform uiRoot = xrOrigin.transform.Find("Canvas (Body-locked UI)");
            if (uiRoot == null)
            {
                // 念のため再帰検索（別名や多少の構造違いに備える）
                uiRoot = FindDeepChildByName(xrOrigin.transform, "Canvas (Body-locked UI)");
            }

            if (uiRoot == null)
            {
                Debug.LogError("[ItemSelectionPhaseManager] Canvas (Body-locked UI) が見つかりません。UI探索をスキップします。");
            }
            else
            {
                // --- Buttons: ButtonObjects/Remove, ButtonObjects/Use ---
                Button removeBtn = null;
                Button useBtn = null;

                var buttonObjects = uiRoot.Find("ButtonObjects") ?? FindDeepChildByName(uiRoot, "ButtonObjects");
                if (buttonObjects != null)
                {
                    removeBtn = buttonObjects.Find("Remove")?.GetComponent<Button>()
                               ?? FindDeepChildByName(buttonObjects, "Remove")?.GetComponent<Button>();
                    useBtn = buttonObjects.Find("Use")?.GetComponent<Button>()
                               ?? FindDeepChildByName(buttonObjects, "Use")?.GetComponent<Button>();

                    if (removeBtn == null) Debug.LogError("[ItemSelectionPhaseManager] Remove Button が見つかりません。");
                    if (useBtn == null) Debug.LogError("[ItemSelectionPhaseManager] Use Button が見つかりません。");
                }
                else
                {
                    Debug.LogError("[ItemSelectionPhaseManager] ButtonObjects が見つかりません。");
                }

                // --- Buttons: ItemSelectButton/Upper, ItemSelectButton/Down ---
                Button upBtn = null;
                Button downBtn = null;

                var itemSelectButton = uiRoot.Find("ItemSelectButton") ?? FindDeepChildByName(uiRoot, "ItemSelectButton");
                if (itemSelectButton != null)
                {
                    upBtn = itemSelectButton.Find("Upper")?.GetComponent<Button>()
                           ?? FindDeepChildByName(itemSelectButton, "Upper")?.GetComponent<Button>();
                    downBtn = itemSelectButton.Find("Down")?.GetComponent<Button>()
                           ?? FindDeepChildByName(itemSelectButton, "Down")?.GetComponent<Button>();

                    if (upBtn == null) Debug.LogError("[ItemSelectionPhaseManager] Up Button(Upper) が見つかりません。");
                    if (downBtn == null) Debug.LogError("[ItemSelectionPhaseManager] Down Button が見つかりません。");
                }
                else
                {
                    Debug.LogError("[ItemSelectionPhaseManager] ItemSelectButton オブジェクトが見つかりません。");
                }

                // 取得できたものを代入
                if (removeBtn != null) removeButton = removeBtn;
                if (useBtn != null) useButton = useBtn;
                if (upBtn != null) upButton = upBtn;
                if (downBtn != null) downButton = downBtn;
            }
        }

        // Remove ボタン登録
        if (removeButton != null)
        {
            removeButton.onClick.AddListener(() =>
            {
                if (_selectedItem == null) return;

                // ★ 削除対象の ItemData をローカルに退避
                var removedItemData = _selectedItem;

                // Player から削除
                var info = targetPlayer.ItemsList.Find(i => i.Name == removedItemData.Name);
                if (info != null) targetPlayer.RemoveItem(info);

                // ★ 元のオブジェクトを元位置に戻す
                if (itemResetButtonManager != null &&
                    worldItemMap.TryGetValue(removedItemData, out var worldTransform))
                {
                    itemResetButtonManager.ResetOneItem(worldTransform);

                    // マップと hiddenItems からも削除しておく
                    worldItemMap.Remove(removedItemData);
                    hiddenItems.Remove(worldTransform.gameObject);
                }

                // UI 削除（元の処理をそのままベースに）
                if (selectedIndex >= 0 && selectedIndex < uiItemList.Count)
                {
                    int deletedIndex = selectedIndex;

                    if (_blinkTarget != null && uiItemList[deletedIndex] != null &&
                        _blinkTarget.transform.IsChildOf(uiItemList[deletedIndex].transform))
                    {
                        StopBlink();
                    }

                    Destroy(uiItemList[deletedIndex].gameObject);
                    uiItemList.RemoveAt(deletedIndex);

                    selectedIndex = deletedIndex - 1;
                }

                // 選択更新
                UpdateSelectionAfterRemove();
            });
        }

        // Use ボタン登録
        if (useButton != null)
        {
            useButton.onClick.AddListener(() =>
            {
                if (targetPlayer == null) return;

                int spotId = targetPlayer.CurrentSpotId;

                // SpotId == 0 は今まで通り「何もしない」
                if (spotId == 0)
                {
                    Debug.Log("[ItemSelectionPhaseManager] CurrentSpotId == 0 のため、Use ボタンは何もしません。");
                    return;
                }

                // ★ すでにこの SpotId で Use 済みなら何もしない（念のためチェック）
                if (_usedSpotIds.Contains(spotId))
                {
                    Debug.Log($"[ItemSelectionPhaseManager] SpotId {spotId} ではすでにアイテム使用済みのため、Use できません。");
                    return;
                }

                if (_selectedItem == null) return;

                bool used = targetPlayer.UsedItem(_selectedItem, spotId);

                if (used)
                {
                    Debug.Log($"Used item: {_selectedItem.Name} (SpotId: {spotId})");

                    // ★ この SpotId は今後 Use 不可にする
                    _usedSpotIds.Add(spotId);

                    // ★ 使用したアイテムをインベントリ＆UIから削除
                    RemoveSelectedItemAfterUse();
                }

                // Spot の状態が変わったのでボタン状態を更新
                UpdateUseButtonState();
            });
        }

        // Up / Down ボタン登録
        if (upButton != null)
            upButton.onClick.AddListener(SelectPrevItem);

        if (downButton != null)
            downButton.onClick.AddListener(SelectNextItem);

        // ★ 最初は何も選択されていない状態にしてボタン無効化
        ApplySelection();
    }

    private void OnDisable()
    {
        // Disable 時に点滅を止めて元の色へ戻す
        StopBlink();
    }

    private void Update()
    {
        CheckCollisionAndRemoveItems();
        UpdateUseButtonState();
    }

    /// <summary>
    /// 5件上限チェック（UI と Player のどちらかが5到達で上限とみなす）
    /// </summary>
    private bool IsInventoryFull() // ★ 追加
    {
        int uiCount = uiItemList.Count;
        int playerCount = (targetPlayer != null && targetPlayer.ItemsList != null) ? targetPlayer.ItemsList.Count : 0;
        return (uiCount >= MaxKeep) || (playerCount >= MaxKeep);
    }

    /// <summary>
    /// コントローラー/手との衝突をチェックしてアイテムを隠す
    /// </summary>
    private void CheckCollisionAndRemoveItems()
    {
        if (itemsContainer == null) return;

        foreach (Transform item in itemsContainer.transform)
        {
            if (item.gameObject.activeSelf && IsCollidingWithControllerOrHand(item.gameObject))
            {
                // ★ 追加：上限に達していたら何もしない（アイテムを消さず、保存もしない）
                if (IsInventoryFull())
                {
                    // 上限時はスキップ（ログ連打を避けるため、ここではログを出さない）
                    continue;
                }

                // アイテムを隠す
                hiddenItems.Add(item.gameObject);
                item.gameObject.SetActive(false);

                // PlayerInfoのItemsListに追加
                AddItemToPlayer(item.gameObject);
            }
        }
    }

    private void UpdateUseButtonState()
    {
        if (useButton == null || targetPlayer == null)
            return;

        bool hasSelectedItem = (_selectedItem != null);

        int spotId = targetPlayer.CurrentSpotId;

        // SpotId == 0 は常に押下可能
        if (spotId == 0)
        {
            useButton.interactable = hasSelectedItem;
            return;
        }


        // ★ すでにこの SpotId で Use 済みなら使用不可
        bool alreadyUsedAtThisSpot = _usedSpotIds.Contains(spotId);

        bool canUse = hasSelectedItem && !alreadyUsedAtThisSpot;
        useButton.interactable = canUse;
    }

    // -----------------------------
    // ★ アイテム追加時 UI 作成処理 ＋ ItemData取得 ★
    // -----------------------------
    private void AddItemToPlayer(GameObject item)
    {
        if (targetPlayer == null)
        {
            Debug.LogWarning("[ItemSelectionPhaseManager] PlayerInfo (targetPlayer) is not assigned.");
            return;
        }

        if (IsInventoryFull())
        {
            return;
        }

        ItemData itemData = null;

        var holder = item.GetComponent<ItemDataHolder>();
        if (holder != null && holder.ItemData != null)
        {
            itemData = holder.ItemData;
            Debug.Log($"[ItemSelectionPhaseManager] ItemData取得成功（ItemDataHolder）: {itemData.Name}");
        }
        else
        {
            itemData = FindItemDataByName(item.name);

            if (itemData != null)
            {
                Debug.LogWarning($"[ItemSelectionPhaseManager] ItemData取得（カタログ検索）: {itemData.Name} - ItemDataHolderの設定を推奨します");
            }
        }

        if (itemData == null)
        {
            Debug.LogError($"[ItemSelectionPhaseManager] ItemDataが見つかりません: {item.name}");
            return;
        }

        if (targetPlayer.ItemsList != null && targetPlayer.ItemsList.Count >= MaxKeep)
        {
            return;
        }

        bool success = targetPlayer.GetItem(itemData);

        if (success)
        {
            Debug.Log($"[ItemSelectionPhaseManager] アイテムをインベントリに追加: {itemData.Name}");

            // ★ ここを追加: ItemData と元の GameObject を紐付ける
            if (!worldItemMap.ContainsKey(itemData))
            {
                worldItemMap.Add(itemData, item.transform);
            }

            // UI 追加は今まで通り
            if (itemContentsPrefab != null && itemListContent != null)
            {
                if (uiItemList.Count >= MaxKeep)
                {
                    Debug.LogWarning("[ItemSelectionPhaseManager] UI保持上限(5)に達しています。UI行は追加しません。");
                }
                else
                {
                    var ui = Instantiate(itemContentsPrefab, itemListContent);
                    ui.SetData(itemData);

                    uiItemList.Add(ui);
                    selectedIndex = uiItemList.Count - 1;
                    ApplySelection();
                }
            }
            else
            {
                Debug.LogWarning("[ItemSelectionPhaseManager] itemContentsPrefab または itemListContent が未設定です。");
            }
        }
        else
        {
            Debug.LogWarning($"[ItemSelectionPhaseManager] アイテムの追加に失敗: {itemData.Name}（インベントリが満杯または重複）");
        }
    }

    // -----------------------------
    // 上下移動の処理
    // -----------------------------
    private void SelectNextItem()
    {
        if (uiItemList.Count == 0) return;

        selectedIndex = Mathf.Min(selectedIndex + 1, uiItemList.Count - 1);
        ApplySelection();
    }

    private void SelectPrevItem()
    {
        if (uiItemList.Count == 0) return;

        selectedIndex = Mathf.Max(selectedIndex - 1, 0);
        ApplySelection();
    }

    // -----------------------------
    // 選択を適用（Remove/Use が正しく動くための核）
    // -----------------------------
    private void ApplySelection()
    {
        if (selectedIndex < 0 || selectedIndex >= uiItemList.Count)
        {
            _selectedItem = null;

            // ハイライト停止
            StopBlink();

            if (removeButton != null) removeButton.interactable = false;

            // Useボタンは共通メソッドで制御
            UpdateUseButtonState();
            return;
        }

        _selectedItem = uiItemList[selectedIndex].ItemData;

        if (removeButton != null) removeButton.interactable = true;

        // ★ ハイライト更新（新しい選択対象の OneItemContents を点滅）
        var selectedContents = uiItemList[selectedIndex];
        StartBlink(selectedContents);

        // Use ボタンの状態を更新
        UpdateUseButtonState();

        Debug.Log($"現在選択中: {_selectedItem.Name}");
    }

    // -----------------------------
    // 削除後の選択更新ロジック
    // -----------------------------
    private void UpdateSelectionAfterRemove()
    {
        if (uiItemList.Count == 0)
        {
            selectedIndex = -1;
            _selectedItem = null;
            if (removeButton != null) removeButton.interactable = false;
            if (useButton != null) useButton.interactable = false;
            return;
        }

        selectedIndex = Mathf.Clamp(selectedIndex, 0, uiItemList.Count - 1);
        ApplySelection();
    }

    /// <summary>
    /// 名前一致でItemDataを取得（フォールバック用）
    /// </summary>
    private ItemData FindItemDataByName(string name)
    {
        if (itemCatalog == null || string.IsNullOrEmpty(name))
        {
            return null;
        }

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
        return IsColliding(item, leftController) || IsColliding(item, rightController) ||
               IsColliding(item, leftHand) || IsColliding(item, rightHand);
    }

    private bool IsColliding(GameObject item, GameObject target)
    {
        if (target == null) return false;

        float distanceThreshold = 0.1f; // しきい値
        return Vector3.Distance(item.transform.position, target.transform.position) < distanceThreshold;
    }

    /// <summary>
    /// Use 成功後に、選択中アイテムをプレイヤー・UI から削除し、
    /// 残っている場合は先頭アイテムを選択する。
    /// </summary>
    private void RemoveSelectedItemAfterUse()
    {
        if (_selectedItem == null)
            return;

        var usedItemData = _selectedItem;

        // --- 1. プレイヤーの ItemsList から削除 ---
        if (targetPlayer != null && targetPlayer.ItemsList != null)
        {
            var info = targetPlayer.ItemsList.Find(i => i.Name == usedItemData.Name);
            if (info != null)
            {
                // UsedItem の中ですでに外している場合は info が null なので二重削除にはなりません
                targetPlayer.RemoveItem(info);
            }
        }

        // --- 2. worldItemMap / hiddenItems を掃除（シーン上には復活させない） ---
        if (worldItemMap.TryGetValue(usedItemData, out var tr))
        {
            hiddenItems.Remove(tr.gameObject);
            worldItemMap.Remove(usedItemData);
            // ※ Use は消費扱いなので、ResetOneItem 等は呼びません
        }

        // --- 3. UIから該当行を削除 ---
        if (selectedIndex >= 0 && selectedIndex < uiItemList.Count)
        {
            int deletedIndex = selectedIndex;

            // 点滅対象が削除対象の子であれば一旦止める
            if (_blinkTarget != null &&
                uiItemList[deletedIndex] != null &&
                _blinkTarget.transform.IsChildOf(uiItemList[deletedIndex].transform))
            {
                StopBlink();
            }

            Destroy(uiItemList[deletedIndex].gameObject);
            uiItemList.RemoveAt(deletedIndex);
        }

        // --- 4. 要件どおり、残っていれば先頭、なければ選択なしにする ---
        if (uiItemList.Count > 0)
        {
            selectedIndex = 0;   // ★ 先頭アイテムを選択
        }
        else
        {
            selectedIndex = -1;  // ★ 選択なし
        }

        ApplySelection();
    }

    // ========= 選択ハイライト（点滅）関連 =========

    // 選択された OneItemContents から、点滅対象の Image を取得して開始
    private void StartBlink(OneItemContents contents)
    {
        StopBlink(); // 以前の点滅を停止

        if (contents == null) return;

        // ここで「光らせたい Image」を決める
        // 例：OneItemContents のルート配下から最初に見つかった Image を対象にする
        _blinkTarget = contents.GetComponentInChildren<Image>(includeInactive: true);

        if (_blinkTarget == null) return;

        // 元の色を覚えてから、ベース色を薄い青に設定
        _originalColor = _blinkTarget.color;

        var baseColor = _blinkColor;
        baseColor.a = 1f;
        _blinkTarget.color = baseColor;

        _blinkCo = StartCoroutine(BlinkRoutine());
    }

    // 今の点滅を止めて元の色に戻す
    private void StopBlink()
    {
        if (_blinkCo != null)
        {
            StopCoroutine(_blinkCo);
            _blinkCo = null;
        }
        if (_blinkTarget != null)
        {
            _blinkTarget.color = _originalColor;
            _blinkTarget = null;
        }
    }

    // 点滅のコルーチン
    private System.Collections.IEnumerator BlinkRoutine()
    {
        float t = 0f;
        while (_blinkTarget != null)
        {
            t += Time.unscaledDeltaTime; // UI なので TimeScale の影響を受けにくくする
            // 0〜1 を往復する PingPong
            float phase = Mathf.PingPong(t / Mathf.Max(_blinkPeriod, 0.01f), 1f);
            // アルファを補間
            float a = Mathf.Lerp(_blinkMinAlpha, _blinkMaxAlpha, phase);

            var c = _blinkTarget.color;
            c.a = a;
            _blinkTarget.color = c;

            yield return null;
        }
    }

    // -------- ユーティリティ: 子孫を名前で再帰検索 --------
    private static Transform FindDeepChildByName(Transform parent, string name)
    {
        if (parent == null) return null;
        foreach (Transform child in parent)
        {
            if (child.name == name) return child;
            var result = FindDeepChildByName(child, name);
            if (result != null) return result;
        }
        return null;
    }


    // ★ ここから追加: 全リセット用の公開メソッド ★
    /// <summary>
    /// プレイヤーのインベントリと UI リストを全消去する（Resetボタンから呼ばれる想定）
    /// </summary>
    public void ClearAllItems()
    {
        // --- 1. プレイヤー側のインベントリを空にする ---
        if (targetPlayer != null)
        {
            if (targetPlayer.ItemsList != null)
            {
                targetPlayer.ItemsList.Clear();
                Debug.Log("[ItemSelectionPhaseManager] targetPlayer.ItemsList を全クリアしました。");
            }
            _usedSpotIds.Clear();

            // もし MainPlayer 側に AllClear() 的なメソッドがあるならそちらを優先
            // targetPlayer.AllClear(); など
        }

        // --- 2. UI のリストを全消去 ---
        // 点滅停止
        StopBlink();

        // OneItemContents の見た目を全部破棄
        foreach (var ui in uiItemList)
        {
            if (ui != null)
            {
                Destroy(ui.gameObject);
            }
        }
        uiItemList.Clear();

        // --- 3. 内部状態リセット ---
        hiddenItems.Clear();
        selectedIndex = -1;
        _selectedItem = null;

        // Remove / Use ボタン状態などを反映
        ApplySelection();   // index=-1 なのでボタン無効化される & ハイライト解除

        Debug.Log("[ItemSelectionPhaseManager] インベントリUIと選択状態をリセットしました。");
    }

}