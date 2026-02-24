using Assets.Team_Prevention.Script.UI;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

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

        [SerializeField] private ItemBoxComponent itemBoxComponent;

        [Header("スポット判定用")]
        public GameObject Phase1Object;
        public GameObject Phase2Object;
        public GameObject Phase3Object;

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

        [SerializeField] private GameObject allSpotsCompletedObject;

        private bool visitedSpot1 = false;
        private bool visitedSpot2 = false;
        private bool visitedSpot3 = false;

        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioSource loopAudioSource;
        [SerializeField] private AudioClip correctClip;
        [SerializeField] private AudioClip wrongClip;
        [SerializeField] private AudioClip fireClip;

        [Header("アイテム生成設定")]
        [SerializeField] private ItemUseSpawner _itemUseSpawner;

        [Tooltip("このプレイヤーの手（Interactor）を明示指定したい場合に設定してください。未設定なら本オブジェクト配下から自動検出します。")]
        [SerializeField] private XRBaseInteractor _preferredInteractor;

        private void Update()
        {
            if (itemBoxComponent == null)
            {
                return;
            }

            var selfPos = (HeadObject != null) ? HeadObject.transform.position : transform.position;
            int spot = 0;

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
            }

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

        public bool UsedItem(ItemData data, int currentSpotId)
        {
            if (data == null)
            {
                return false;
            }

            var entry = ItemsList.Find(i => i.Name == data.Name && !i.IsUsed);
            if (entry == null)
            {
                Debug.LogWarning($"Item not in inventory or already used: {data.Name}");
                return false;
            }

            entry.IsUsed = true;

            int rawPoint = data.Point;
            float applied = 0f;
            bool isCorrect = (data.CorrectUseSpotId == currentSpotId);

            if (isCorrect)
            {
                applied = rawPoint;
                Heal(applied);

                // 正解音はここでは鳴らさない（エフェクト完了後にする）
            }
            else
            {
                applied = -Mathf.Abs(rawPoint);
                Damage(Mathf.Abs(applied));

                if (audioSource != null && wrongClip != null)
                {
                    audioSource.PlayOneShot(wrongClip);
                }
            }

            OnInventoryChanged?.Invoke(ItemsList);

            OnItemUsed?.Invoke(applied);
            Debug.Log($"Used item: {data.Name}, spot={currentSpotId}, point change={applied}");

            // 正解時のみ「複製→エフェクト完了→音」へ
            if (isCorrect)
            {
                TrySpawnItemToInteractorAndPlayCorrectSoundAfterEffect(data);
            }
            else
            {
                TrySpawnItemToInteractor(data);
            }

            return true;
        }

        private void TrySpawnItemToInteractorAndPlayCorrectSoundAfterEffect(ItemData data)
        {
            XRGrabInteractable created = TrySpawnItemToInteractor(data);
            if (created == null)
            {
                // 生成できない場合はフォールバックとして即再生（無音のままよりはマシ、不要なら消してOK）
                if (audioSource != null && correctClip != null)
                {
                    audioSource.PlayOneShot(correctClip);
                }

                return;
            }

            var effectTrigger = created.GetComponent<ItemEffectTrigger>();
            if (effectTrigger == null)
            {
                // エフェクトスクリプトが無いなら即再生
                if (audioSource != null && correctClip != null)
                {
                    audioSource.PlayOneShot(correctClip);
                }

                return;
            }

            // 二重購読防止のため、ハンドラはローカルで作って一回で解除
            void Handler(ItemEffectTrigger _)
            {
                effectTrigger.OnEffectCompleted -= Handler;

                if (audioSource != null && correctClip != null)
                {
                    audioSource.PlayOneShot(correctClip);
                }
            }

            effectTrigger.OnEffectCompleted += Handler;
        }

        /// <summary>
        /// 生成に成功したら XRGrabInteractable を返す。失敗時は null。
        /// </summary>
        private XRGrabInteractable TrySpawnItemToInteractor(ItemData data)
        {
            if (data == null)
            {
                return null;
            }

            ItemUseSpawner spawner = _itemUseSpawner;
            if (spawner == null)
            {
                spawner = FindFirstObjectByType<ItemUseSpawner>();
                if (spawner == null)
                {
                    Debug.LogWarning("[PlayerInfo] ItemUseSpawner が見つかりません。アイテムを手元に生成できません。");
                    return null;
                }
            }

            XRBaseInteractor targetInteractor = ResolveInteractorForThisPlayer();
            if (targetInteractor == null)
            {
                Debug.LogWarning("[PlayerInfo] このプレイヤー配下に XRBaseInteractor が見つかりません。_preferredInteractor を設定してください。");
                return null;
            }

            XRGrabInteractable created = spawner.SpawnAndAttachToInteractor(targetInteractor, data.Name);
            if (created == null)
            {
                created = spawner.SpawnAndAttachToInteractor(targetInteractor);
            }

            if (created == null)
            {
                Debug.LogWarning("[PlayerInfo] アイテムの生成または手動掴みに失敗しました。");
                return null;
            }

            Debug.Log($"[PlayerInfo] アイテムを手元に生成して掴ませました: {data.Name}");
            return created;
        }

        private XRBaseInteractor ResolveInteractorForThisPlayer()
        {
            // 明示指定があるなら最優先
            if (_preferredInteractor != null)
            {
                return _preferredInteractor;
            }

            // マルチ対策：シーンから適当に拾わず「自分の子」から探す
            var interactors = GetComponentsInChildren<XRBaseInteractor>(true);
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

        /// <summary>
        /// デバッグ/確認用：アイテムを「使用」せずに手元へ生成して掴ませる（インベントリ消費なし）
        /// </summary>
        /// <remarks>
        /// アイテム選択フェーズなど、Spot未確定(spot=0)でもモーション/エフェクト確認をするためのAPI。
        /// </remarks>
        public XRGrabInteractable PreviewSpawnItemToHand(ItemData data)
        {
            if (data == null)
            {
                return null;
            }

            return TrySpawnItemToInteractor(data);
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
        /// 全アイテム削除 API（外部から削除する場合は直接 ItemsList を操作せずこちらを使う）
        /// </summary>
        public void AllClear()
        {
            if (ItemsList == null)
            {
                ItemsList = new List<ItemInfo>(5);
            }

            ItemsList.Clear();
            OnInventoryChanged?.Invoke(ItemsList);
        }
    }
}