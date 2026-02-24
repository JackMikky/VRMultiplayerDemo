using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Assets.Team_Prevention.Script.UI
{
    /// <summary>
    /// ItemBox（所持アイテムの一覧）を制御するコンポーネント。
    /// ListView: PlayerInfo.ItemsList（ItemInfo）
    /// Buttons: Remove / Use
    /// </summary>
    public class ItemBoxComponent : MonoBehaviour
    {
        [SerializeField] private UIDocument uiDocument;
        [SerializeField] private MainPlayer targetPlayer;

        /// <summary>
        /// UIルートのヒット判定を無効化する（レイの常時ヒットを避けるための対策）。
        /// 無効化するとUI全体のクリックも無効になります。必要に応じてON/OFFしてください。
        /// </summary>
        [Header("Ray対策")]
        [SerializeField] private bool disableRootPicking = false;


        [Header("アイテムカタログ")]
        /// <summary>
        /// ItemDataHolderからカタログを自動生成するか
        /// </summary>
        [Tooltip("trueの場合、シーン内のItemDataHolderから自動的にカタログを生成します")]
        [SerializeField] private bool autoGenerateCatalog = true;

        /// <summary>
        /// カタログ生成時に検索する親オブジェクト
        /// </summary>
        [Tooltip("nullの場合はシーン全体を検索します。指定すると、その子階層のみを検索します")]
        [SerializeField] private GameObject catalogSearchRoot = null;

        /// <summary>
        /// アイテム効果の参照用カタログ（名前で引き当てます）
        /// </summary>
        [SerializeField] private List<ItemData> itemCatalog=new List<ItemData>();

        /// <summary>
        /// 行UIのテンプレート（ItemView.uxml）
        /// </summary>
        [SerializeField] private VisualTreeAsset itemRowTemplate;

        /// <summary>
        /// 行UIのスタイル（ItemView.uss）
        /// </summary>
        [SerializeField] private StyleSheet itemRowStyle;

        [Header("環境/判定用")]
        [SerializeField] public int currentSpotId = 0; // 使用判定用スポットID

        // --- デモ用設定 ---
        [Header("デモ設定")]
        [SerializeField] private bool demoAutoFillOnStart = false; // 起動時に自動で入れる
        [SerializeField] [Range(0, 5)] private int demoAutoFillCount = 3; // 自動投入する個数

        private ListView _listView;
        private Button _btnRemove;
        private Button _btnUse;

        private List<ItemInfo> _items; // 所持品（PlayerInfo.ItemsList を参照）

        // イベント（必要なら外部で購読）
        public event Action<ItemInfo> OnSelected;
        public event Action<ItemInfo> OnUsed;
        public event Action<ItemInfo> OnRemoved;
        private VisualElement _uiRoot;

        private bool _initialized = false;   // ★ 追加


        private void Start()
        {
            // カタログ自動生成
            if (autoGenerateCatalog)
            {
                GenerateCatalogFromScene();
            }

            // UI要素取得
            var root = uiDocument?.rootVisualElement;
            _uiRoot = root;

            // レイ常時ヒット対策：ルートのピッキングを無効化（必要時）

            if (root != null && disableRootPicking)
            {
                root.pickingMode = PickingMode.Ignore;
            }


            _listView = root?.Q<ListView>("ItemList");
            _btnRemove = root?.Q<Button>("Remove");
            _btnUse    = root?.Q<Button>("Use");

            // 参照チェック
            if (_listView == null)
            {
                Debug.LogError("ListView 'ItemList' not found in UIDocument.");
                return;
            }

            //// ★ targetPlayer がまだ null の場合は、
            ////    Awake ではエラーにせず「後から Initialize される前提」とする
            if (targetPlayer == null)
            {
                Debug.LogWarning("[ItemBoxComponent] targetPlayer が Awake 時点で未設定。後から Initialize() で設定される想定です。");
                return;
            }


            // ★ 共通初期化処理
            InitializeCore();


            // 所持品データを参照（PlayerInfo.ItemsList を使う）
            _items = targetPlayer.ItemsList ?? new List<ItemInfo>();

            // ListView 構成＆データ反映
            SetupListView();
            BindData();

            // 選択イベント（onSelectionChange は非推奨 → selectionChanged）
            _listView.selectionChanged += HandleSelectionChanged;

            // ボタンイベント購読
            if (_btnRemove != null)
            {
                _btnRemove.clicked += HandleRemoveClicked;
            }
            if (_btnUse    != null)
            {
                _btnUse.clicked    += HandleUseClicked;
            }

            // 起動時にデモ用アイテムを投入
            if (demoAutoFillOnStart)
            {
                AutoFillDemoItems();
            }

            // PlayerInfo のイベント購読
            if (targetPlayer != null)
            {
                targetPlayer.OnItemAdded += HandlePlayerItemAdded;
                targetPlayer.OnItemRemoved += HandlePlayerItemRemoved;
                targetPlayer.OnInventoryChanged += HandleInventoryChanged;
            }

            // 初期は選択なし → ボタン無効
            UpdateButtonsEnabled(null);
        }

        // ★ 追加：実際の初期化処理
        private void InitializeCore()
        {
            if (_initialized) return;
            _initialized = true;

            // 所持品データを参照
            _items = targetPlayer.ItemsList ?? new List<ItemInfo>();

            // ListView 構成＆データ反映
            SetupListView();
            BindData();

            _listView.selectionChanged += HandleSelectionChanged;

            if (_btnRemove != null)
                _btnRemove.clicked += HandleRemoveClicked;
            if (_btnUse != null)
                _btnUse.clicked += HandleUseClicked;

            if (demoAutoFillOnStart)
            {
                AutoFillDemoItems();
            }

            if (targetPlayer != null)
            {
                targetPlayer.OnItemAdded += HandlePlayerItemAdded;
                targetPlayer.OnItemRemoved += HandlePlayerItemRemoved;
                targetPlayer.OnInventoryChanged += HandleInventoryChanged;
            }

            // 初期は選択なし → ボタン無効
            UpdateButtonsEnabled(null);
        }

        // ★ 追加：外から targetPlayer / catalogSearchRoot を設定して初期化するAPI
        public void Initialize(MainPlayer player, GameObject searchRoot)
        {
            targetPlayer = player;
            catalogSearchRoot = searchRoot;

            // カタログもここで作り直したい場合
            if (autoGenerateCatalog)
            {
                GenerateCatalogFromScene();
            }

            // Awake で UI 参照が取れていれば、本格初期化
            if (_listView != null && !_initialized && targetPlayer != null)
            {
                InitializeCore();
            }
        }

        /// <summary>
        /// シーン内のItemDataHolderからカタログを自動生成
        /// </summary>
        private void GenerateCatalogFromScene()
        {
            if (itemCatalog == null)
            {
                itemCatalog = new List<ItemData>();
            }
            else
            {
                itemCatalog.Clear();
            }

            // 検索対象を決定
            ItemDataHolder[] holders;
            if (catalogSearchRoot != null)
            {
                // 指定されたルート配下のみ検索
                holders = catalogSearchRoot.GetComponentsInChildren<ItemDataHolder>(true);
                Debug.Log($"[ItemBoxComponent] カタログ生成: {catalogSearchRoot.name} 配下を検索");
            }
            else
            {
                // シーン全体を検索
                holders = FindObjectsByType<ItemDataHolder>(FindObjectsSortMode.None);
                Debug.Log($"[ItemBoxComponent] カタログ生成: シーン全体を検索");
            }

            // ItemDataHolderから重複なくItemDataを収集
            HashSet<ItemData> uniqueItems = new HashSet<ItemData>();
            foreach (var holder in holders)
            {
                if (holder.ItemData != null)
                {
                    if (uniqueItems.Add(holder.ItemData))
                    {
                        itemCatalog.Add(holder.ItemData);
                        Debug.Log($"[ItemBoxComponent] カタログに追加: {holder.ItemData.Name}");
                    }
                }
                else
                {
                    Debug.LogWarning($"[ItemBoxComponent] ItemDataHolderにItemDataが設定されていません: {holder.gameObject.name}", holder);
                }
            }

            Debug.Log($"[ItemBoxComponent] カタログ生成完了: {itemCatalog.Count} 個のアイテム");
        }

        private void OnDestroy()
        {
            if (_listView != null)
            {
                _listView.selectionChanged -= HandleSelectionChanged;
            }
            if (_btnRemove != null)
            {
                _btnRemove.clicked -= HandleRemoveClicked;
            }
            if (_btnUse != null)
            {
                _btnUse.clicked -= HandleUseClicked;
            }

            if (targetPlayer != null)
            {
                targetPlayer.OnItemAdded -= HandlePlayerItemAdded;
                targetPlayer.OnItemRemoved -= HandlePlayerItemRemoved;
                targetPlayer.OnInventoryChanged -= HandleInventoryChanged;
            }
        }

        ///// <summary>
        ///// ListView の設定（ItemView.uxmlによる行テンプレートを適用）
        ///// </summary>
        //private void SetupListView()
        //{
        //    // ★ 高さは UXML/USS と一致させる（あなたの親UXMLは fixed-item-height="100"）
        //    // ここでは 100 に統一する例（30 を使いたいなら USS 側も 30 に合わせてください）
        //    _listView.fixedItemHeight = 100;
        //    _listView.selectionType = SelectionType.Single;

        //    _listView.makeItem = () =>
        //    {
        //        Debug.Log("[ItemBoxComponent] makeItem called");


        //        // itemRowTemplate の状態
        //        if (itemRowTemplate == null)
        //        {
        //            Debug.LogWarning("[ItemBoxComponent] itemRowTemplate is NULL");
        //        }
        //        else
        //        {
        //            Debug.Log($"[ItemBoxComponent] itemRowTemplate OK: {itemRowTemplate.name}");
        //        }


        //        var root = itemRowTemplate != null ? itemRowTemplate.CloneTree() : new VisualElement();


        //        if (root == null)
        //        {
        //            Debug.LogError("[ItemBoxComponent] root is NULL after CloneTree / new VisualElement");
        //        }

        //        root.name = "ItemRowRoot";

        //        // USSを適用
        //        if (itemRowStyle != null)
        //            root.styleSheets.Add(itemRowStyle);

        //        // 行ルートにベースクラス（USSの .item-row セレクタに合わせる）
        //        root.AddToClassList("item-row");

        //        // 背景を塗る対象（UXML側で name="Row" を想定）
        //        var row = root.Q<VisualElement>("Row");
        //        if (row != null)
        //        {
        //            row.style.position = Position.Relative;
        //            row.style.minHeight = 100;           // fixedItemHeight と一致
        //            row.style.left = row.style.right = row.style.top = row.style.bottom = StyleKeyword.Null;

        //            // 交互色/選択色は "row" に付けるため、キャッシュしておく
        //            root.userData = row;
        //        }
        //        return root;
        //    };

        //    // ❷ 行バインド：交互色・選択色をRowに付け外しする
        //    _listView.bindItem = (element, index) =>
        //    {
        //        if (index < 0 || index >= _items.Count) return;

        //        var li = _items[index];

        //        // コンテンツ更新（省略可）
        //        var icon = element.Q<Image>("Icon");
        //        var title = element.Q<Label>("Title");
        //        if (title != null) title.text = li?.Name ?? string.Empty;
        //        if (icon != null)
        //        {
        //            Texture2D tex = null;
        //            if (li != null)
        //            {
        //                var data = FindItemDataByName(li.Name);
        //                if (data != null) tex = data.Icon;
        //            }
        //            icon.image = tex;
        //        }

        //        // 背景を塗るターゲット（Row）を取得
        //        var row = element.userData as VisualElement ?? element;

        //        // used（必要ならRow or elementどちらに付けてもOK。背景に影響ないならelementへ）
        //        element.RemoveFromClassList("used");
        //        if (li is { IsUsed: true }) element.AddToClassList("used");

        //        // 交互色・選択色は Row に対して操作（毎回リセット→付与）
        //        row.RemoveFromClassList("row-even");
        //        row.RemoveFromClassList("row-odd");
        //        row.RemoveFromClassList("row-selected");

        //        if ((index & 1) == 0)
        //            row.AddToClassList("row-even");   // 0,2,4...
        //        else
        //            row.AddToClassList("row-odd");    // 1,3,5...

        //        // 選択中なら選択色を最優先（交互色を外す）
        //        if (_listView.selectedIndex == index)
        //        {
        //            row.RemoveFromClassList("row-even");
        //            row.RemoveFromClassList("row-odd");
        //            row.AddToClassList("row-selected");
        //        }
        //    };


        //    // 選択変更時：表示中の行のみ見た目を更新（RefreshItemsでも可）
        //    _listView.selectionChanged += HandleSelectionChanged;
        //}

        // Awake や InitializeCore の中など
        private void SetupListView()
        {
            _listView.fixedItemHeight = 100;
            _listView.selectionType = SelectionType.Single;

            // ★ ラムダではなくメソッドを指定
            _listView.makeItem = MakeItem;
            _listView.bindItem = BindItem;

            _listView.selectionChanged += HandleSelectionChanged;
        }

        // ListView に 1 行分の VisualElement を生成するメソッド
        private VisualElement MakeItem()
        {
            Debug.Log("[ItemBoxComponent] MakeItem called");

            // itemRowTemplate の状態
            if (itemRowTemplate == null)
            {
                Debug.LogWarning("[ItemBoxComponent] itemRowTemplate is NULL");
            }
            else
            {
                Debug.Log($"[ItemBoxComponent] itemRowTemplate OK: {itemRowTemplate.name}");
            }

            // テンプレートから行を生成 or プレーンな VisualElement
            var root = itemRowTemplate != null
                ? itemRowTemplate.CloneTree()
                : new VisualElement();

            if (root == null)
            {
                Debug.LogError("[ItemBoxComponent] root is NULL after CloneTree / new VisualElement");
            }

            root.name = "ItemRowRoot";

            // USS を適用
            if (itemRowStyle != null)
            {
                Debug.Log($"[ItemBoxComponent] itemRowStyle OK: {itemRowStyle.name}");
                root.styleSheets.Add(itemRowStyle);
            }
            else
            {
                Debug.LogWarning("[ItemBoxComponent] itemRowStyle is NULL (no row style will be applied)");
            }

            // 行ルートにベースクラス（USS の .item-row 用）
            root.AddToClassList("item-row");

            // 背景を塗る対象（UXML 側で name="Row" を想定）
            var row = root.Q<VisualElement>("Row");
            if (row != null)
            {
                Debug.Log("[ItemBoxComponent] row element found");

                row.style.position = Position.Relative;
                row.style.minHeight = 100; // fixedItemHeight と一致
                row.style.left = StyleKeyword.Null;
                row.style.right = StyleKeyword.Null;
                row.style.top = StyleKeyword.Null;
                row.style.bottom = StyleKeyword.Null;

                // 交互色/選択色は "row" に付けるため、キャッシュしておく
                root.userData = row;
            }
            else
            {
                Debug.LogWarning("[ItemBoxComponent] row(Q<VisualElement>(\"Row\")) is NULL. " +
                                 "ItemView.uxml 内に name=\"Row\" の要素があるか確認してください。");
            }

            return root;
        }

        // もともと：
        // _listView.bindItem = (element, index) => { ... };

        private void BindItem(VisualElement element, int index)
        {
            if (index < 0 || index >= _items.Count)
                return;

            var li = _items[index];

            var icon = element.Q<Image>("Icon");
            var title = element.Q<Label>("Title");

            if (title != null)
                title.text = li?.Name ?? string.Empty;

            if (icon != null)
            {
                Texture2D tex = null;
                if (li != null)
                {
                    var data = FindItemDataByName(li.Name);
                    if (data != null)
                        tex = data.Icon;
                }
                icon.image = tex;
            }

            // 背景を塗るターゲット（Row）を取得
            var row = element.userData as VisualElement ?? element;

            // used フラグ
            element.RemoveFromClassList("used");
            if (li is { IsUsed: true })
                element.AddToClassList("used");

            // 交互色・選択色のクラス付け替え
            row.RemoveFromClassList("row-even");
            row.RemoveFromClassList("row-odd");
            row.RemoveFromClassList("row-selected");

            if ((index & 1) == 0)
                row.AddToClassList("row-even");
            else
                row.AddToClassList("row-odd");

            if (_listView.selectedIndex == index)
            {
                row.RemoveFromClassList("row-even");
                row.RemoveFromClassList("row-odd");
                row.AddToClassList("row-selected");
            }
        }

        /// <summary>
        /// ListView に所持品を反映
        /// </summary>
        private void BindData()
        {
            _listView.itemsSource = _items;
            _listView.RefreshItems();
        }

        /// <summary>
        /// 選択変更時のハンドラ
        /// </summary>
        private void HandleSelectionChanged(IEnumerable<object> selected)
        {
            ItemInfo selectedItem = null;
            foreach (var obj in selected)
            {
                if (obj is ItemInfo ii)
                {
                    selectedItem = ii;
                    break;
                }
            }
            _listView.RefreshItems();

            OnSelected?.Invoke(selectedItem);
            UpdateButtonsEnabled(selectedItem);
        }

        /// <summary>
        /// ボタンの有効/無効を選択に応じて更新
        /// </summary>
        private void UpdateButtonsEnabled(ItemInfo selected)
        {
            bool hasSelection = selected != null;
            _btnRemove?.SetEnabled(hasSelection);
            _btnUse?.SetEnabled(hasSelection && !selected.IsUsed);
        }

        /// <summary>
        /// Remove クリック：選択所持品をインベントリから削除
        /// </summary>
        private void HandleRemoveClicked()
        {
            var selected = _listView.selectedItem as ItemInfo;
            if (selected == null)
            { 
                return; 
            }

            // 直接 ItemsList を操作するのではなく PlayerInfo の API を使う
            bool ok = targetPlayer.RemoveItem(selected);
            if (ok)
            {
                RefreshFromPlayer();
                UpdateButtonsEnabled(null);
                OnRemoved?.Invoke(selected);
            }
        }

        /// <summary>
        /// Use クリック：選択所持品を使用（スコア加減算など）
        /// </summary>
        private void HandleUseClicked()
        {
            Debug.LogWarning($"Use Click!");

            var selected = _listView.selectedItem as ItemInfo;
            if (selected == null || selected.IsUsed)
            {
                return;
            }

            var data = FindItemDataByName(selected.Name);
            if (data == null)
            {
                Debug.LogWarning($"ItemData not found in catalog: {selected.Name}");
                return;
            }

            // Spot未確定(=0)は「使用」ではなく「プレビューで手に持つ」
            if (currentSpotId == 0)
            {
                var created = targetPlayer.PreviewSpawnItemToHand(data);
                if (created == null)
                {
                    Debug.LogWarning($"[ItemBoxComponent] PreviewSpawnItemToHand failed: {selected.Name}");
                }

                // インベントリ状態は変えない
                return;
            }

            bool ok = targetPlayer.UsedItem(data, currentSpotId);
            if (ok)
            {
                selected.IsUsed = true;
                BindData();
                UpdateButtonsEnabled(selected);
                OnUsed?.Invoke(selected);
            }
            else
            {
                Debug.LogWarning($"Failed to use item: {selected.Name}");
            }
        }

        /// <summary>
        /// 名前一致で ItemData を取得
        /// </summary>
        private ItemData FindItemDataByName(string name)
        {
            if (itemCatalog == null || string.IsNullOrEmpty(name))
            {
                return null;
            }
            for (int i = 0; i < itemCatalog.Count; i++)
            {
                var d = itemCatalog[i];
                if (d != null && d.Name == name)
                {
                    return d;
                }
            }
            return null;
        }

        /// <summary>
        /// 外部更新があった際に所持品表示を最新にしたい場合に呼ぶヘルパ
        /// </summary>
        public void RefreshFromPlayer()
        {
            _items = targetPlayer.ItemsList ?? new List<ItemInfo>();
            BindData();
            UpdateButtonsEnabled(_listView.selectedItem as ItemInfo);
        }

        // ===== ここからデモ用API =====

        /// <summary>
        /// 名前指定でカタログから取得して所持に追加（デモ用）
        /// </summary>
        public bool AddItemToInventory(string itemName)
        {
            var data = FindItemDataByName(itemName);
            if (data == null)
            {
                Debug.LogWarning($"Demo Add: ItemData not found: {itemName}");
                return false;
            }

            var ok = targetPlayer.GetItem(data);
            if (ok)
            {
                RefreshFromPlayer();
            }
            return ok;
        }

        /// <summary>
        /// ItemData を直接指定して所持に追加（デモ用）
        /// </summary>
        public bool AddItemToInventory(ItemData data)
        {
            if (data == null)
            {
                return false;
            }
            var ok = targetPlayer.GetItem(data);
            if (ok)
            {
                RefreshFromPlayer();
            }
            return ok;
        }

        /// <summary>
        /// 起動時に itemCatalog から前方 N 件を投入（デモ用）
        /// </summary>
        private void AutoFillDemoItems()
        {
            if (itemCatalog == null || itemCatalog.Count == 0)
            {
                return;
            }
            int count = Mathf.Clamp(demoAutoFillCount, 0, Mathf.Min(5, itemCatalog.Count));
            for (int i = 0; i < count; i++)
            {
                targetPlayer.GetItem(itemCatalog[i]);
            }
            RefreshFromPlayer();
        }

        private void HandlePlayerItemAdded(ItemInfo added)
        { // UI更新 RefreshFromPlayer();
          // 簡易的な暗影（0.5秒）
            if (_uiRoot != null)
            {
                _uiRoot.AddToClassList("dimmed");
                StartCoroutine(RemoveDimAfterSeconds(0.5f));
            }
        }

        private void HandlePlayerItemRemoved(ItemInfo removed) 
        { 
            RefreshFromPlayer();
        }

        private void HandleInventoryChanged(List<ItemInfo> list) 
        { 
            RefreshFromPlayer(); 
        }

        private System.Collections.IEnumerator RemoveDimAfterSeconds(float seconds) 
        { 
            yield return new WaitForSeconds(seconds); 
            if (_uiRoot != null) 
            { 
                _uiRoot.RemoveFromClassList("dimmed"); 
            } 
        }

    }
}
