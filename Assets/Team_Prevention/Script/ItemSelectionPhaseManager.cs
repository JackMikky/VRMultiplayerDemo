using Assets.Team_Prevention.Script;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using static UnityEngine.XR.OpenXR.Features.Interactions.HandInteractionProfile;

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

    private XRBaseInteractor _leftHandInteractor;
    // ★ 追加: すでに Use 済みの SpotId を記録する
    private HashSet<int> _usedSpotIds = new HashSet<int>();

    // ★ 追加: Near/Far 吸い寄せ→「手元に来たら取得」用
    [Header("Near/Far 取得判定（手元到達）")]
    [SerializeField, Tooltip("吸い寄せたアイテムが手元(attachTransform)へこの距離以内に来たら取得する")]
    private float _nearFarPickupDistance = 0.02f;

    // 吸い寄せ中（selectEntered 済み）アイテムを記録
    private readonly Dictionary<XRGrabInteractable, XRBaseInteractor> _pendingNearFar = new Dictionary<XRGrabInteractable, XRBaseInteractor>();

    // ★ 追加: XRGrabInteractable の購読管理（解除用）
    private readonly List<XRGrabInteractable> _subscribedGrabInteractables = new List<XRGrabInteractable>();

    private void OnDestroy()
    {
        UnsubscribeItemEvents();
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

                    if (removeBtn == null) { Debug.LogError("[ItemSelectionPhaseManager] Remove Button が見つかりません。"); }
                    if (useBtn == null) { Debug.LogError("[ItemSelectionPhaseManager] Use Button が見つかりません。"); }
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

                    if (upBtn == null) { Debug.LogError("[ItemSelectionPhaseManager] Up Button(Upper) が見つかりません。"); }
                    if (downBtn == null) { Debug.LogError("[ItemSelectionPhaseManager] Down Button が見つかりません。"); }
                }
                else
                {
                    Debug.LogError("[ItemSelectionPhaseManager] ItemSelectButton オブジェクトが見つかりません。");
                }

                // 取得できたものを代入
                if (removeBtn != null) { removeButton = removeBtn; }
                if (useBtn != null) { useButton = useBtn; }
                if (upBtn != null) { upButton = upBtn; }
                if (downBtn != null) { downButton = downBtn; }
            }
        }

        // Remove ボタン登録
        if (removeButton != null)
        {
            removeButton.onClick.AddListener(() =>
            {
                if (_selectedItem == null) { return; }

                // ★ 削除対象の ItemData をローカルに退避
                var removedItemData = _selectedItem;

                // Player から削除
                var info = targetPlayer.ItemsList.Find(i => i.Name == removedItemData.Name);
                if (info != null) { targetPlayer.RemoveItem(info); }

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
                if (_selectedItem == null) { return; }

                int spotId = targetPlayer.CurrentSpotId;

                if (targetPlayer == null)
                {
                    return;
                }

                if (_leftHandInteractor == null)
                {
                    Debug.LogWarning("[ItemSelectionPhaseManager] leftController 配下に XRBaseInteractor が見つかりません。");
                    return;
                }

                //SpotId == 0 は今まで通り「何もしない」
                if (spotId == 0)
                {
                    var spawner = FindFirstObjectByType<ItemUseSpawner>();
                    if (spawner != null && leftController != null)
                    {
                        var created = spawner.SpawnAndAttachToInteractor(_leftHandInteractor, _selectedItem.Name, null, leftController.transform);
                        if (created == null)
                        {
                            Debug.LogWarning($"[ItemSelectionPhaseManager] Spawner 経由の生成に失敗（フォールバックを試行）: {_selectedItem.Name}");
                            var fallback = targetPlayer.PreviewSpawnItemToHand(_selectedItem, _leftHandInteractor);
                            if (fallback == null)
                            {
                                Debug.LogWarning($"[ItemSelectionPhaseManager] フォールバック生成にも失敗: {_selectedItem.Name}");
                            }
                        }
                    }
                    else
                    {
                        var created = targetPlayer.PreviewSpawnItemToHand(_selectedItem);
                        if (created == null)
                        {
                            Debug.LogWarning($"[ItemSelectionPhaseManager] 左手への生成に失敗: {_selectedItem.Name}");
                        }
                    }
                    return;
                }

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
        {
            upButton.onClick.AddListener(SelectPrevItem);
        }

        if (downButton != null)
        {
            downButton.onClick.AddListener(SelectNextItem);
        }

        // ★ 最初は何も選択されていない状態にしてボタン無効化
        ApplySelection();

        _leftHandInteractor = ResolveInteractorFromController(leftController);
        targetPlayer.HandInteractor = _leftHandInteractor;

        // ★ Near/Far のイベント購読開始
        SubscribeItemEvents();
    }

    private void OnDisable()
    {
        // Disable 時に点滅を止めて元の色へ戻す
        StopBlink();
    }

    private void Update()
    {
        // Near/Far の selectEntered で拾うため、距離判定は行わない
        UpdateUseButtonState();
    }

    private void LateUpdate()
    {
        // ★ フレーム終端の pose 更新後に「手元到達チェック」
        ProcessPendingNearFarPickups();
    }

    /// <summary>
    /// itemsContainer 配下の XRGrabInteractable の selectEntered/selectExited を購読する。
    /// </summary>
    private void SubscribeItemEvents()
    {
        UnsubscribeItemEvents();

        if (itemsContainer == null)
        {
            Debug.LogWarning("[ItemSelectionPhaseManager] itemsContainer が未設定です。");
            return;
        }

        var grabs = itemsContainer.GetComponentsInChildren<XRGrabInteractable>(true);
        if (grabs == null || grabs.Length == 0)
        {
            Debug.LogWarning("[ItemSelectionPhaseManager] itemsContainer 配下に XRGrabInteractable が見つかりません。");
            return;
        }

        for (int i = 0; i < grabs.Length; i++)
        {
            var grab = grabs[i];
            if (grab == null)
            {
                continue;
            }

            grab.selectEntered.AddListener(OnWorldItemSelectEntered);
            grab.selectExited.AddListener(OnWorldItemSelectExited);

            _subscribedGrabInteractables.Add(grab);
        }
    }

    private void UnsubscribeItemEvents()
    {
        for (int i = 0; i < _subscribedGrabInteractables.Count; i++)
        {
            var grab = _subscribedGrabInteractables[i];
            if (grab == null)
            {
                continue;
            }

            grab.selectEntered.RemoveListener(OnWorldItemSelectEntered);
            grab.selectExited.RemoveListener(OnWorldItemSelectExited);
        }

        _subscribedGrabInteractables.Clear();
        _pendingNearFar.Clear();
    }

    /// <summary>
    /// Near/Far で select が成立した瞬間（吸い寄せ開始）に呼ばれる。
    /// ここでは「取得」はせず、手元到達待ちの対象として登録する。
    /// </summary>
    private void OnWorldItemSelectEntered(SelectEnterEventArgs args)
    {
        if (args == null)
        {
            return;
        }

        var grab = args.interactableObject as XRGrabInteractable;
        if (grab == null)
        {
            return;
        }

        var interactor = args.interactorObject as XRBaseInteractor;
        if (interactor == null)
        {
            return;
        }

        // 既に非表示なら対象外
        if (!grab.gameObject.activeInHierarchy)
        {
            return;
        }

        // 上限なら何もしない（ただし消さない）
        if (IsInventoryFull())
        {
            return;
        }

        // 手元に来たら拾うために記録
        _pendingNearFar[grab] = interactor;
    }

    /// <summary>
    /// select が外れたら（途中キャンセル等）待ち行列から外す。
    /// </summary>
    private void OnWorldItemSelectExited(SelectExitEventArgs args)
    {
        if (args == null)
        {
            return;
        }

        var grab = args.interactableObject as XRGrabInteractable;
        if (grab == null)
        {
            return;
        }

        _pendingNearFar.Remove(grab);
    }

    /// <summary>
    /// Near/Far で吸い寄せ中のアイテムが interactor.attachTransform 近傍へ来たら取得する。
    /// </summary>
    private void ProcessPendingNearFarPickups()
    {
        if (_pendingNearFar.Count == 0)
        {
            return;
        }

        if (IsInventoryFull())
        {
            return;
        }

        // 走査中に remove するので一旦リスト化
        var keys = ListPool<XRGrabInteractable>.Get();
        try
        {
            foreach (var kv in _pendingNearFar)
            {
                keys.Add(kv.Key);
            }

            for (int i = 0; i < keys.Count; i++)
            {
                var grab = keys[i];
                if (grab == null)
                {
                    _pendingNearFar.Remove(grab);
                    continue;
                }

                if (!_pendingNearFar.TryGetValue(grab, out var interactor) || interactor == null)
                {
                    _pendingNearFar.Remove(grab);
                    continue;
                }

                if (!grab.gameObject.activeInHierarchy)
                {
                    _pendingNearFar.Remove(grab);
                    continue;
                }

                Transform hand = (interactor.transform != null) ? interactor.transform : interactor.attachTransform;
                float d = Vector3.Distance(grab.transform.position, hand.position);
                Debug.Log($"アイテム取得：{grab.name}:{d} {_nearFarPickupDistance} \n {d <= _nearFarPickupDistance}");
                if (!(d <= _nearFarPickupDistance))
                {
                    continue;
                }

                // ★ 手元に来た：取得（成功時のみ消す）
                bool obtained = AddItemToPlayer(grab.gameObject);
                if (!obtained)
                {
                    // 取得できないなら待ち続けるとループするので、ここでは外す
                    _pendingNearFar.Remove(grab);
                    continue;
                }

                hiddenItems.Add(grab.gameObject);
                grab.gameObject.SetActive(false);

                _pendingNearFar.Remove(grab);
            }
        }
        finally
        {
            ListPool<XRGrabInteractable>.Release(keys);
        }
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

    private void UpdateUseButtonState()
    {
        if (useButton == null || targetPlayer == null)
        {
            return;
        }

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
    private bool AddItemToPlayer(GameObject item)
    {
        if (targetPlayer == null)
        {
            Debug.LogWarning("[ItemSelectionPhaseManager] PlayerInfo (targetPlayer) is not assigned.");
            return false;
        }

        if (IsInventoryFull())
        {
            return false;
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
            return false;
        }

        if (targetPlayer.ItemsList != null && targetPlayer.ItemsList.Count >= MaxKeep)
        {
            return false;
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

            return true;
        }

        Debug.LogWarning($"[ItemSelectionPhaseManager] アイテムの追加に失敗: {itemData.Name}（インベントリが満杯または重複）");
        return false;
    }

    // -----------------------------
    // 上下移動の処理
    // -----------------------------
    private void SelectNextItem()
    {
        if (uiItemList.Count == 0)
        {
            return;
        }

        selectedIndex = Mathf.Min(selectedIndex + 1, uiItemList.Count - 1);
        ApplySelection();
    }

    private void SelectPrevItem()
    {
        if (uiItemList.Count == 0)
        {
            return;
        }

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

            if (removeButton != null)
            {
                removeButton.interactable = false;
            }

            // Useボタンは共通メソッドで制御
            UpdateUseButtonState();
            return;
        }

        _selectedItem = uiItemList[selectedIndex].ItemData;

        if (removeButton != null)
        {
            removeButton.interactable = true;
        }

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
            if (removeButton != null) { removeButton.interactable = false; }
            if (useButton != null) { useButton.interactable = false; }
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

    /// <summary>
    /// Use 成功後に、選択中アイテムをプレイヤー・UI から削除し、
    /// 残っている場合は先頭アイテムを選択する。
    /// </summary>
    private void RemoveSelectedItemAfterUse()
    {
        if (_selectedItem == null)
        {
            return;
        }

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

        _blinkTarget = contents.GetComponentInChildren<Image>(includeInactive: true);
        if (_blinkTarget == null) return;

        _originalColor = _blinkTarget.color;

        var baseColor = _blinkColor;
        baseColor.a = 1f;
        _blinkTarget.color = baseColor;

        _blinkCo = StartCoroutine(BlinkRoutine());
    }

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

    private System.Collections.IEnumerator BlinkRoutine()
    {
        float t = 0f;
        while (_blinkTarget != null)
        {
            t += Time.unscaledDeltaTime;
            float phase = Mathf.PingPong(t / Mathf.Max(_blinkPeriod, 0.01f), 1f);
            float a = Mathf.Lerp(_blinkMinAlpha, _blinkMaxAlpha, phase);

            var c = _blinkTarget.color;
            c.a = a;
            _blinkTarget.color = c;

            yield return null;
        }
    }

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

    private XRBaseInteractor ResolveInteractorFromController(GameObject controllerObject)
    {
        if (controllerObject == null)
        {
            return null;
        }

        var interactors = controllerObject.GetComponentsInChildren<XRBaseInteractor>(true);
        if (interactors == null || interactors.Length == 0)
        {
            return null;
        }

        for (int i = 0; i < interactors.Length; i++)
        {
            if (interactors[i] is XRDirectInteractor && interactors[i].gameObject.activeSelf)
            {
                return interactors[i];
            }
        }

        for (int i = 0; i < interactors.Length; i++)
        {
            if (interactors[i] is NearFarInteractor && interactors[i].gameObject.activeSelf)
            {
                return interactors[i];
            }
        }

        for (int i = 0; i < interactors.Length; i++)
        {
            if (interactors[i] is XRRayInteractor && interactors[i].gameObject.activeSelf)
            {
                return interactors[i];
            }
        }

        return interactors[0];
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

        hiddenItems.Clear();
        selectedIndex = -1;
        _selectedItem = null;

        ApplySelection();

        Debug.Log("[ItemSelectionPhaseManager] インベントリUIと選択状態をリセットしました。");
    }
}

/// <summary>
/// GCを減らすための簡易ListPool（UnityEngine.Pool が使えない環境向け）
/// </summary>
internal static class ListPool<T>
{
    private static readonly Stack<List<T>> _pool = new Stack<List<T>>();

    public static List<T> Get()
    {
        if (_pool.Count > 0)
        {
            var list = _pool.Pop();
            list.Clear();
            return list;
        }

        return new List<T>(16);
    }

    public static void Release(List<T> list)
    {
        if (list == null)
        {
            return;
        }

        list.Clear();
        _pool.Push(list);
    }
}