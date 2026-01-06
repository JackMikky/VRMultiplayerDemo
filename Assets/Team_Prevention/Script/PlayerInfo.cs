using Assets.Team_Prevention.Script.UI;
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace Assets.Team_Prevention.Script
{
    public class PlayerInfo : MonoBehaviour
    {
        /// <summary>
        /// HP値
        /// </summary>
        [Range(0, 100)] public float CurrentHP;

        [Tooltip("体力表示Text")]
        public TMP_Text healthText;

        // PlayerInfo クラス内のフィールドに追加
        [SerializeField] private ItemBoxComponent itemBoxComponent;

        // フェーズ判定用の参照
        [Header("スポット判定用")]
        public GameObject Phase1Object;

        public GameObject Phase2Object;
        public GameObject Phase3Object;

        // PlayerInfo クラス内のフィールドに追加（インスペクタでアタッチ）
        [Header("判定対象（頭など）")]
        [SerializeField] private GameObject HeadObject;

        /// <summary>
        /// 最大HP値
        /// </summary>
        public float MaxHP = 100f;

        /// <summary>
        /// 体験スコア
        /// </summary>
        public float Score = 100;

        /// <summary>
        /// 選択アイテムリスト(5こ)
        /// </summary>
        public List<ItemInfo> ItemsList = new List<ItemInfo>(5);

        /// <summary>
        /// HP通知(CurrentHP, MaxHP)
        /// </summary>
        public event System.Action<float, float> OnHPChanged;

        /// <summary>
        /// スコア通知(Score)
        /// </summary>
        public event System.Action<float> OnScoreChanged;

        public event System.Action<float> OnItemUsed;

        // --- 追加イベント（インベントリ変更通知） ---
        /// <summary>
        /// アイテム取得時に発火（新しく追加されたItemInfoを引数で渡す）
        /// </summary>
        public event System.Action<ItemInfo> OnItemAdded;

        /// <summary>
        /// アイテム削除時に発火（削除されたItemInfoを引数で渡す）
        /// </summary>
        public event System.Action<ItemInfo> OnItemRemoved;

        /// <summary>
        /// 所持品一覧が変化したときに発火（現在のリストを渡す）
        /// </summary>
        public event System.Action<List<ItemInfo>> OnInventoryChanged;

        // -----------------------------------------------

        // 追加: 全スポット到達時に有効化するオブジェクト
        [SerializeField] private GameObject allSpotsCompletedObject;

        // 追加: スポット到達状態のトラッキング
        private bool visitedSpot1 = false;

        private bool visitedSpot2 = false;
        private bool visitedSpot3 = false;

        // -----------------------------------------------

        // -----------------------------------------------

        // 追加: 再生するサウンド
        [SerializeField] private AudioSource audioSource;           // 再生に使う AudioSource（任意のGameObjectにアタッチ）

        [SerializeField] private AudioSource loopAudioSource;       // ループ用の別のAudioSource
        [SerializeField] private AudioClip correctClip;             // 正解時のクリップ
        [SerializeField] private AudioClip wrongClip;               // 誤り時のクリップ
        [SerializeField] private AudioClip fireClip;                // 燃えている音

        // -----------------------------------------------

        // 毎フレーム、このコンポーネントがアタッチされているGameObject の位置が各フェーズオブジェクトのエリア内かを判定してcurrentSpotIdを更新
        private void Update()
        {
            if (itemBoxComponent == null) return;

            // 判定対象のワールド座標（HeadObject が設定されていればそれを使う）
            var selfPos = (HeadObject != null) ? HeadObject.transform.position : transform.position;
            int spot = 0;

            // 3D: Collider を使用した領域判定
            if (Phase1Object != null)
            {
                var col1 = Phase1Object.GetComponentInChildren<Collider>();
                if (col1 != null && col1.bounds.Contains(selfPos))
                {
                    spot = 1;
                    visitedSpot1 = true;
                }
            }
            bool inPhase2 = false;
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
            if (itemBoxComponent.currentSpotId != spot)
            {
                itemBoxComponent.currentSpotId = spot;
                // Debug.Log($"[PlayerInfo] Spot changed -> {itemBoxComponent.currentSpotId}");
            }

            // 追加: フェーズ2滞在判定に応じてループ再生制御
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

            if (visitedSpot1 && visitedSpot2 && visitedSpot3)
            {
                allSpotsCompletedObject.SetActive(false);
            }
        }

        // -----------------------------------------------

        /// <summary>
        /// HPリセット
        /// </summary>
        public void ResetHPToMax()
        {
            CurrentHP = MaxHP;
            OnHPChanged?.Invoke(CurrentHP, MaxHP);

            healthText.text = CurrentHP.ToString();
        }

        /// <summary>
        /// ダメージ計算
        /// </summary>
        /// <param name="amount"></param>
        public void Damage(float amount)
        {
            CurrentHP = Mathf.Clamp(CurrentHP - amount, 0, MaxHP);
            OnHPChanged?.Invoke(CurrentHP, MaxHP);

            healthText.text = CurrentHP.ToString();
        }

        /// <summary>
        /// 回復計算
        /// </summary>
        /// <param name="amount"></param>
        public void Heal(float amount)
        {
            CurrentHP = Mathf.Clamp(CurrentHP + amount, 0, MaxHP);
            OnHPChanged?.Invoke(CurrentHP, MaxHP);

            healthText.text = CurrentHP.ToString();
        }

        /// <summary>
        /// スコア獲得
        /// </summary>
        /// <param name="points"></param>
        public void AddScore(float points)
        {
            Score += points;
            OnScoreChanged?.Invoke(Score);
        }

        /// <summary>
        /// スコア減点
        /// </summary>
        /// <param name="points"></param>
        public void DeductScore(float points)
        {
            Score -= points;

            if (Score < 0)
            {
                Score = 0;
            }

            OnScoreChanged?.Invoke(Score);
        }

        /// <summary>
        /// アイテム取得（ListViewで選択されたアイテムデータを所持に追加）
        /// </summary>
        public bool GetItem(ItemData data)
        {
            if (data == null) return false;

            // 上限チェック
            if (ItemsList.Count >= 5)
            {
                Debug.LogWarning("Inventory full (max 5).");
                return false;
            }

            // 同名未使用が既にある場合は重複しない（必要なら許容へ変更）
            var existing = ItemsList.Find(i => i.Name == data.Name && !i.IsUsed);
            if (existing != null)
            {
                Debug.Log($"Already obtained: {data.Name}");
                return false;
            }

            var newItem = new ItemInfo { Name = data.Name, IsUsed = false };
            ItemsList.Add(newItem);

            // イベント通知
            OnItemAdded?.Invoke(newItem);
            OnInventoryChanged?.Invoke(ItemsList);

            Debug.Log($"Obtained item: {data.Name}");
            return true;
        }

        /// <summary>
        /// 全アイテム削除 API（外部から削除する場合は直接 ItemsList を操作せずこちらを使う）
        /// </summary>
        public void AllClear()
        {
            ItemsList.Clear();
            OnInventoryChanged?.Invoke(ItemsList);
        }

        /// <summary>
        /// アイテム削除 API（外部から削除する場合は直接 ItemsList を操作せずこちらを使う）
        /// </summary>
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

            Debug.LogWarning($"Failed to remove item (not found): {item.Name}");
            return false;
        }

        /// <summary>
        /// アイテム使用（正しいスポットなら加点、違えば減点）
        /// </summary>
        public bool UsedItem(ItemData data, int currentSpotId)
        {
            if (data == null) return false;

            var entry = ItemsList.Find(i => i.Name == data.Name && !i.IsUsed);
            if (entry == null)
            {
                Debug.LogWarning($"Item not in inventory or already used: {data.Name}");
                return false;
            }

            // 使用フラグ
            entry.IsUsed = true;

            // スポット判定：正解なら加点、誤りなら減点
            int rawPoint = data.Point;
            float applied = 0f;
            if (data.CorrectUseSpotId == currentSpotId)
            {
                applied = rawPoint; // 加点
                Heal(applied);

                // 正解サウンド
                if (audioSource != null && correctClip != null)
                {
                    audioSource.PlayOneShot(correctClip);
                }
            }
            else
            {
                applied = -Mathf.Abs(rawPoint); // 減点
                Damage(Mathf.Abs(applied));

                // 誤りサウンド
                if (audioSource != null && wrongClip != null)
                {
                    audioSource.PlayOneShot(wrongClip);
                }
            }

            // 所持品更新通知（使用済みによる状態変化）
            OnInventoryChanged?.Invoke(ItemsList);

            OnItemUsed?.Invoke(applied);
            Debug.Log($"Used item: {data.Name}, spot={currentSpotId}, point change={applied}");
            return true;
        }
    }
}