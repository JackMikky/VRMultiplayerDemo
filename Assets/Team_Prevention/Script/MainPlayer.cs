using Assets.Team_Prevention.Script.UI;
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using Assets.Team_Prevention.Script;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using System.Text;

/// <summary>
/// プレイヤー情報を管理するクラス（静止オブジェクトへアタッチ）
/// プレイヤーの Transform を参照して各種処理を行う
/// </summary>
public class MainPlayer : MonoBehaviour
{
    // --- プレイヤー参照（動くオブジェクト） ---
    [Header("プレイヤー参照")]
    [Tooltip("実際に動くプレイヤーのGameObject（必須）")]
    public GameObject PlayerObject;

    [Header("����Ώہi���Ȃǁj")]
    [Tooltip("�v���C���[�̓��ȂǁA�ʒu����Ɏg���I�u�W�F�N�g�i�C�Ӂj")]
    [SerializeField] public GameObject HeadObject;

    // --- 表示/UI ---
    [Header("UI")]
    [Tooltip("体力表示Text (TMP_Text)")]
    public TMP_Text healthText;

    // --- 連携コンポーネント ---
    [Header("連携コンポーネント")]
    [SerializeField] private ItemBoxComponent itemBoxComponent;

    // --- スポット／フェーズ判定用 ---
    [Header("スポット判定用オブジェクト")]
    public GameObject Phase1Object;
    public GameObject Phase2Object;
    public GameObject Phase3Object;

    [Header("全スポット到達時に無効化するオブジェクト")]
    [SerializeField] private GameObject allSpotsCompletedObject;

    // --- サウンド ---
    [Header("サウンド")]
    [SerializeField] private AudioSource audioSource;           // 再生に使う AudioSource（任意のGameObjectにアタッチ）
    [SerializeField] private AudioSource loopAudioSource;       // ループ用の別のAudioSource
    [SerializeField] private AudioClip correctClip;             // 正解時のクリップ
    [SerializeField] private AudioClip wrongClip;               // 誤り時のクリップ
    [SerializeField] private AudioClip fireClip;                // 燃えている音

    // --- ステータス ---
    [Header("ステータス")]
    [Range(0, 100)] public float CurrentHP = 100f;
    public float MaxHP = 100f;
    public float Score = 100f;

    // --- アイテム ---
    [Header("インベントリ（最大5個）")]
    public List<ItemInfo> ItemsList = new List<ItemInfo>(5);


    // ★ 追加：使用したアイテム＋Spot のログ
    private readonly List<string> _usedLogs = new List<string>();


    // --- イベント ---
    public event System.Action<float, float> OnHPChanged;        // (CurrentHP, MaxHP)
    public event System.Action<float> OnScoreChanged;            // (Score)
    public event System.Action<float> OnItemUsed;                // (+/- point)
    public event System.Action<ItemInfo> OnItemAdded;
    public event System.Action<ItemInfo> OnItemRemoved;
    public event System.Action<List<ItemInfo>> OnInventoryChanged;

    // --- 内部状態（スポット到達トラッキング） ---
    private bool visitedSpot1 = false;
    private bool visitedSpot2 = false;
    private bool visitedSpot3 = false;

    [Header("アイテム生成設定")]
    [SerializeField] private Assets.Team_Prevention.Script.ItemUseSpawner _itemUseSpawner;

    [Tooltip("このプレイヤーの手（Interactor）を明示指定したい場合に設定してください。未設定なら本オブジェクト配下から自動検出します。")]
    [SerializeField] public XRBaseInteractor HandInteractor;

    [SerializeField] private NetworkScoreManager _networkScoreManager;

    [Header("UI")]
    [Tooltip("プレイヤースコアText (TMP_Text)")]
    public TMP_Text PlayerScoreText;

    public int CurrentSpotId { get; private set; }
    private void Start()
    {
        // 初期表示
        if (healthText != null) healthText.text = CurrentHP.ToString();

        if (_networkScoreManager == null)
        {
            _networkScoreManager = FindFirstObjectByType<NetworkScoreManager>();
        }
    }


    private void OnEnable()
    {
        if (_networkScoreManager == null)
            _networkScoreManager = FindFirstObjectByType<NetworkScoreManager>();

        if (_networkScoreManager != null)
        {
            _networkScoreManager.OnScoresUpdated += UpdatePlayerScoreText;
        }
    }

    private void OnDisable()
    {
        if (_networkScoreManager != null)
        {
            _networkScoreManager.OnScoresUpdated -= UpdatePlayerScoreText;
        }
    }

    private void Update()
    {
        if (itemBoxComponent == null) return;
        if (PlayerObject == null) return;

        // 判定対象のワールド座標（HeadObject が設定されていればそれを使う、なければ PlayerObject、さらに最後のフォールバックとして自身）
        Vector3 selfPos;
        if (HeadObject != null)
        {
            selfPos = HeadObject.transform.position;
        }
        else
        {
            selfPos = PlayerObject.transform.position;
        }

        int spot = 0;
        bool inPhase2 = false;

        // Collider.bounds.Contains による領域判定
        if (Phase1Object != null)
        {
            var col1 = Phase1Object.GetComponentInChildren<Collider>();
            if (col1 != null && col1.bounds.Contains(selfPos))
            {
                spot = 1;
                visitedSpot1 = true;
            }
        }

        if (Phase2Object != null)
        {
            var col2 = Phase2Object.GetComponentInChildren<Collider>();
            if (col2 != null && col2.bounds.Contains(selfPos))
            {
                spot = 2;
                visitedSpot2 = true;
                inPhase2 = true;
            }
        }

        if (Phase3Object != null)
        {
            var col3 = Phase3Object.GetComponentInChildren<Collider>();
            if (col3 != null && col3.bounds.Contains(selfPos))
            {
                spot = 3;
                visitedSpot3 = true;
            }
        }

        // Spot変更時 ItemBoxComponent に反映
        if (itemBoxComponent.currentSpotId != spot)
        {
            itemBoxComponent.currentSpotId = spot;
            // Debug.Log($"[MainPlayer] Spot changed -> {itemBoxComponent.currentSpotId}");
        }

        // フェーズ2滞在中は火災音ループ、抜けたら停止
        if (loopAudioSource != null)
        {
            if (inPhase2)
            {
                if (!loopAudioSource.isPlaying)
                {
                    loopAudioSource.clip = fireClip;
                    loopAudioSource.loop = true;
                    loopAudioSource.Play();
                }
            }
            else
            {
                if (loopAudioSource.isPlaying && loopAudioSource.clip == fireClip)
                {
                    loopAudioSource.Stop();
                }
            }
        }

        // 全スポット到達時の表示制御
        if (visitedSpot1 && visitedSpot2 && visitedSpot3 && allSpotsCompletedObject != null)
        {
            allSpotsCompletedObject.SetActive(false);
        }

        CurrentSpotId = spot;
    }

    private void UpdatePlayerScoreText()
    {
        if (PlayerScoreText == null)
            return;

        if (_networkScoreManager == null)
            _networkScoreManager = FindFirstObjectByType<NetworkScoreManager>();

        if (_networkScoreManager == null)
        {
            PlayerScoreText.text = "スコア情報なし（Manager 未Spawn）";
            return;
        }

        var list = _networkScoreManager.GetAllScores();
        if (list == null || list.Count == 0)
        {
            PlayerScoreText.text = "nothing score inform yet";
            return;
        }

        // 表示用ビルダー
        System.Text.StringBuilder sb = new System.Text.StringBuilder();

        foreach (var ps in list)
        {
            string name = ps.playerName;
            if (string.IsNullOrEmpty(name))
                name = $"Player {ps.clientId}";

            sb.AppendLine($"{name} : {ps.score:0}");

            // ★ 使用したアイテム＋Spot情報も表示
            if (!string.IsNullOrEmpty(ps.usedInfo))
            {
                sb.AppendLine($"  Used: {ps.usedInfo}");
            }

            sb.AppendLine(); // プレイヤーごとに空行
        }

        PlayerScoreText.text = sb.ToString();
    }

    private void ReportScoreToNetwork()
    {
        if (_networkScoreManager == null)
        {
            _networkScoreManager = FindFirstObjectByType<NetworkScoreManager>();
        }

        if (_networkScoreManager == null)
        {
            Debug.LogWarning("[MainPlayer] NetworkScoreManager が見つかりません（まだ Spawn されていない可能性）。");
            return;
        }

        if (!_networkScoreManager.IsSpawned)
        {
            Debug.LogWarning("[MainPlayer] NetworkScoreManager は存在するが NetworkObject が Spawn されていません。");
            return;
        }

        // ★ 使用ログを1つの文字列にまとめる
        string usedInfo = BuildUsedInfo();

        // ★ スコア + 使用アイテム情報を送信
        _networkScoreManager.ReportLocalScore(CurrentHP, usedInfo);
    }


    // --- ステータス操作 ---
    public void ResetHPToMax()
    {
        CurrentHP = MaxHP;
        OnHPChanged?.Invoke(CurrentHP, MaxHP);
        if (healthText != null) healthText.text = CurrentHP.ToString();
        ReportScoreToNetwork();
        UpdatePlayerScoreText();     // ★ 追加
    }

    public void Damage(float amount)
    {
        CurrentHP = Mathf.Clamp(CurrentHP - amount, 0f, MaxHP);
        OnHPChanged?.Invoke(CurrentHP, MaxHP);
        if (healthText != null) healthText.text = CurrentHP.ToString();
        ReportScoreToNetwork();
        UpdatePlayerScoreText();     // ★ 追加
    }

    public void Heal(float amount)
    {
        CurrentHP = Mathf.Clamp(CurrentHP + amount, 0f, MaxHP);
        OnHPChanged?.Invoke(CurrentHP, MaxHP);
        if (healthText != null) healthText.text = CurrentHP.ToString();
        ReportScoreToNetwork();
        UpdatePlayerScoreText();     // ★ 追加
    }

    public void AddScore(float points)
    {
        Score += points;
        OnScoreChanged?.Invoke(Score);
        ReportScoreToNetwork();
    }

    public void DeductScore(float points)
    {
        Score -= points;
        if (Score < 0f) Score = 0f;
        OnScoreChanged?.Invoke(Score);
        ReportScoreToNetwork();
    }

    // --- インベントリ操作 ---
    public void AllClear()
    {
        ItemsList.Clear();
        OnInventoryChanged?.Invoke(ItemsList);
    }

    public bool GetItem(ItemData data)
    {
        if (data == null) return false;

        // 上限チェック
        if (ItemsList.Count >= 5)
        {
            Debug.LogWarning("Inventory full (max 5).");
            return false;
        }

        // 未使用の同名アイテムがある場合は重複取得しない
        var existing = ItemsList.Find(i => i.Name == data.Name && !i.IsUsed);
        if (existing != null)
        {
            Debug.Log($"Already obtained: {data.Name}");
            return false;
        }

        var newItem = new ItemInfo { Name = data.Name, IsUsed = false };
        ItemsList.Add(newItem);

        OnItemAdded?.Invoke(newItem);
        OnInventoryChanged?.Invoke(ItemsList);

        Debug.Log($"Obtained item: {data.Name}");
        return true;
    }

    public bool RemoveItem(ItemInfo item)
    {
        if (item == null) return false;

        bool removed = ItemsList.Remove(item);
        if (removed)
        {
            OnItemRemoved?.Invoke(item);
            OnInventoryChanged?.Invoke(ItemsList);
            Debug.Log($"Removed item: {item.Name}");
            return true;
        }

        Debug.LogWarning($"Failed to remove item (not found): {item?.Name}");
        return false;
    }

    // --- アイテム使用 ---
    public bool UsedItem(ItemData data, int currentSpotId)
    {
        if (data == null) return false;

        var entry = ItemsList.Find(i => i.Name == data.Name && !i.IsUsed);
        if (entry == null)
        {
            Debug.LogWarning($"Item not in inventory or already used: {data.Name}");
            return false;
        }

        entry.IsUsed = true;

        int rawPoint = data.Point;
        float applied = 0f;

        if (data.CorrectUseSpotId == currentSpotId)
        {
            applied = rawPoint;               // 加点（HP回復扱い）
            Heal(applied);

            if (audioSource != null && correctClip != null)
            {
                audioSource.PlayOneShot(correctClip);
            }
        }
        else
        {
            applied = -Mathf.Abs(rawPoint);   // 減点（ダメージ扱い）
            Damage(Mathf.Abs(applied));

            if (audioSource != null && wrongClip != null)
            {
                audioSource.PlayOneShot(wrongClip);
            }
        }

        OnInventoryChanged?.Invoke(ItemsList);
        OnItemUsed?.Invoke(applied);

        Debug.Log($"Used item: {data.Name}, spot={currentSpotId}, point change={applied}");
        TrySpawnItemToInteractor(data);


        // ★ ここを追加：使用ログに追記
        string log = $"{data.Name}@Spot{currentSpotId}";
        _usedLogs.Add(log);

        // ★ 使用ログも含めてスコア情報をネットへ送る
        ReportScoreToNetwork();

        return true;
    }

    /// <summary>
    /// デバッグ/確認用：アイテムを「使用」せずに手元へ生成して掴ませる（インベントリ消費なし）
    /// </summary>
    public XRGrabInteractable PreviewSpawnItemToHand(Assets.Team_Prevention.Script.ItemData data)
    {
        if (data == null)
        {
            return null;
        }

        return TrySpawnItemToInteractor(data);
    }

    /// <summary>
    /// 指定した Interactor の手元へアイテムを生成して掴ませます（インベントリ消費なし）。
    /// ItemSelectionPhaseManager など「どの手に出すか」が決まっているケース向け。
    /// </summary>
    public XRGrabInteractable PreviewSpawnItemToHand(ItemData data, XRBaseInteractor interactor)
    {
        if (data == null)
        {
            return null;
        }

        if (interactor == null)
        {
            Debug.LogWarning("[MainPlayer] PreviewSpawnItemToHand: interactor が null です。");
            return null;
        }

        var spawner = _itemUseSpawner;
        if (spawner == null)
        {
            spawner = FindFirstObjectByType<Assets.Team_Prevention.Script.ItemUseSpawner>();
            if (spawner == null)
            {
                Debug.LogWarning("[MainPlayer] ItemUseSpawner が見つかりません。アイテムを手元に生成できません。");
                return null;
            }
        }

        XRGrabInteractable created = spawner.SpawnAndAttachToInteractor(interactor, data.Name);
        if (created == null)
        {
            created = spawner.SpawnAndAttachToInteractor(interactor);
        }

        if (created == null)
        {
            Debug.LogWarning($"[MainPlayer] アイテムの生成または手動掴みに失敗しました: {data.Name}");
            return null;
        }

        Debug.Log($"[MainPlayer] 指定Interactorへ生成して掴ませました: {data.Name}, interactor={interactor.name}");
        return created;
    }

    /// <summary>
    /// 生成に成功したら XRGrabInteractable を返す。失敗時は null。
    /// </summary>
    private XRGrabInteractable TrySpawnItemToInteractor(Assets.Team_Prevention.Script.ItemData data)
    {
        if (data == null)
        {
            return null;
        }

        var spawner = _itemUseSpawner;
        if (spawner == null)
        {
            spawner = FindFirstObjectByType<Assets.Team_Prevention.Script.ItemUseSpawner>();
            if (spawner == null)
            {
                Debug.LogWarning("[MainPlayer] ItemUseSpawner が見つかりません。アイテムを手元に生成できません。");
                return null;
            }
        }

        XRBaseInteractor targetInteractor = ResolveInteractorForThisPlayer();
        if (targetInteractor == null)
        {
            Debug.LogWarning("[MainPlayer] このプレイヤー配下に XRBaseInteractor が見つかりません。_preferredInteractor を設定してください。");
            return null;
        }

        XRGrabInteractable created = spawner.SpawnAndAttachToInteractor(targetInteractor, data.Name);
        if (created == null)
        {
            created = spawner.SpawnAndAttachToInteractor(targetInteractor);
        }

        if (created == null)
        {
            Debug.LogWarning("[MainPlayer] アイテムの生成または手動掴みに失敗しました。");
            return null;
        }

        Debug.Log($"[MainPlayer] アイテムを手元に生成して掴ませました: {data.Name}");
        return created;
    }

    private XRBaseInteractor ResolveInteractorForThisPlayer()
    {
        if (HandInteractor != null)
        {
            return HandInteractor;
        }

        if (PlayerObject == null)
        {
            return null;
        }

        // 1) PlayerObject の「同階層」(= 親配下 = sibling) から探す
        Transform parent = PlayerObject.transform.parent;
        if (parent != null)
        {
            var siblings = parent.GetComponentsInChildren<XRBaseInteractor>(true);
            var resolved = ResolveByPriority(siblings);
            if (resolved != null)
            {
                return resolved;
            }
        }

        // 2) フォールバック：PlayerObject の子から探す（構造差異に備える）
        var children = PlayerObject.GetComponentsInChildren<XRBaseInteractor>(true);
        return ResolveByPriority(children);
    }

    private XRBaseInteractor ResolveByPriority(XRBaseInteractor[] interactors)
    {
        if (interactors == null || interactors.Length == 0)
        {
            return null;
        }

        // 優先度：Direct → Ray → それ以外
        for (int i = 0; i < interactors.Length; i++)
        {
            if (interactors[i] is XRDirectInteractor)
            {
                return interactors[i];
            }
        }

        for (int i = 0; i < interactors.Length; i++)
        {
            if (interactors[i] is XRRayInteractor)
            {
                return interactors[i];
            }
        }

        return interactors[0];
    }


    private string BuildUsedInfo()
    {
        if (_usedLogs.Count == 0)
            return "";

        var sb = new StringBuilder();

        // 例: "消火器@Spot2, 通報@Spot1, ..."
        for (int i = 0; i < _usedLogs.Count; i++)
        {
            sb.Append(_usedLogs[i]);
            if (i < _usedLogs.Count - 1)
                sb.Append(", ");
        }

        return sb.ToString();
    }

}