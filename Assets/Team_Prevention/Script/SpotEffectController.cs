using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace Assets.Team_Prevention.Script
{
    /// <summary>
    /// 特定のSpotに配置し、アイテム使用に連動してエフェクト（帆の縮小、煙の発生など）を制御するコンポーネント
    /// </summary>
    public class SpotEffectController : MonoBehaviour
    {
        [Header("Spot設定")]
        [Tooltip("このエフェクトが対応するSpot ID（1=Phase1, 2=Phase2, 3=Phase3）")]
        [SerializeField] private int _spotId = 1;

        [Header("エフェクト対象の定義")]
        [Tooltip("炎オブジェクト（ReduceFlame用）")]
        [SerializeField] private List<EffectTarget> _flameTargets = new List<EffectTarget>();

        [Tooltip("煙パーティクル（GenerateSmoke用）")]
        [SerializeField] private List<EffectTarget> _smokeTargets = new List<EffectTarget>();

        [Tooltip("帆オブジェクト（ReduceSail用）")]
        [SerializeField] private List<EffectTarget> _sailTargets = new List<EffectTarget>();

        [Tooltip("水パーティクル（SprayWater用）")]
        [SerializeField] private List<EffectTarget> _waterTargets = new List<EffectTarget>();

        [Tooltip("表示/非表示対象（HideObject/ShowObject用）")]
        [SerializeField] private List<EffectTarget> _toggleTargets = new List<EffectTarget>();

        [Header("変形設定")]
        [Tooltip("変形アニメーションの時間（秒）")]
        [SerializeField] private float _transformDuration = 2.0f;

        [Tooltip("変形のイージングカーブ")]
        [SerializeField] private AnimationCurve _transformCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

        [Header("デバッグ")]
        [SerializeField] private bool _debugMode = false;

        // アニメーション管理用
        private Dictionary<GameObject, TransformAnimation> _activeAnimations = new Dictionary<GameObject, TransformAnimation>();

        // 静的リスト（複数のSpotに対応）
        private static Dictionary<int, SpotEffectController> _spotControllers = new Dictionary<int, SpotEffectController>();

        /// <summary>
        /// エフェクト対象の定義
        /// </summary>
        [System.Serializable]
        public class EffectTarget
        {
            [Tooltip("対象のGameObject")]
            public GameObject Target;

            [Tooltip("識別名（ItemData の EffectTargetName と一致させる）")]
            public string TargetName;

            [Tooltip("縮小時の最終スケール倍率（ReduceFlame/ReduceSail用）")]
            public Vector3 TargetScale = new Vector3(0.1f, 0.1f, 0.1f);

            [Tooltip("パーティクルシステム（GenerateSmoke/SprayWater用）")]
            public ParticleSystem ParticleSystem;
        }

        /// <summary>
        /// アニメーション状態
        /// </summary>
        private class TransformAnimation
        {
            public Vector3 StartScale;
            public Vector3 TargetScale;
            public float Progress;
            public bool IsActive;
        }

        void Awake()
        {
            // 静的リストに登録
            if (!_spotControllers.ContainsKey(_spotId))
            {
                _spotControllers[_spotId] = this;
            }
            else
            {
                Debug.LogWarning($"[SpotEffectController] Spot ID {_spotId} は既に登録されています。", this);
            }
        }

        void OnDestroy()
        {
            // 静的リストから削除
            if (_spotControllers.ContainsKey(_spotId) && _spotControllers[_spotId] == this)
            {
                _spotControllers.Remove(_spotId);
            }
        }

        /// <summary>
        /// 指定されたSpot IDに対応するコントローラーを取得
        /// </summary>
        public static SpotEffectController GetControllerForSpot(int spotId)
        {
            if (_spotControllers.TryGetValue(spotId, out var controller))
            {
                return controller;
            }

            return null;
        }

        /// <summary>
        /// アイテム使用時にエフェクトを発動（外部から呼ばれる）
        /// </summary>
        public void TriggerEffect(ItemData itemData, bool isCorrectSpot)
        {
            if (_debugMode)
            {
                Debug.Log($"[SpotEffectController] Spot {_spotId}: エフェクト発動 - アイテム={itemData.Name}, 正解={isCorrectSpot}, エフェクト={itemData.EffectType}", this);
            }

            // 正解の場合のみエフェクトを発動
            if (!isCorrectSpot)
            {
                return;
            }

            // エフェクト種別に応じた処理
            switch (itemData.EffectType)
            {
                case ItemEffectType.ReduceFlame:
                    TriggerReduceEffect(_flameTargets, itemData);
                    break;

                case ItemEffectType.GenerateSmoke:
                    TriggerParticleEffect(_smokeTargets, itemData);
                    break;

                case ItemEffectType.ReduceSail:
                    TriggerReduceEffect(_sailTargets, itemData);
                    break;

                case ItemEffectType.SprayWater:
                    TriggerParticleEffect(_waterTargets, itemData);
                    break;

                case ItemEffectType.HideObject:
                    TriggerToggleEffect(_toggleTargets, itemData, false);
                    break;

                case ItemEffectType.ShowObject:
                    TriggerToggleEffect(_toggleTargets, itemData, true);
                    break;

                case ItemEffectType.None:
                default:
                    if (_debugMode)
                    {
                        Debug.Log($"[SpotEffectController] エフェクトなし: {itemData.Name}", this);
                    }
                    break;
            }
        }

        /// <summary>
        /// 縮小エフェクトを発動
        /// </summary>
        private void TriggerReduceEffect(List<EffectTarget> targets, ItemData itemData)
        {
            var matchingTargets = FindMatchingTargets(targets, itemData.EffectTargetName);

            foreach (var effectTarget in matchingTargets)
            {
                if (effectTarget.Target == null)
                {
                    continue;
                }

                var targetObj = effectTarget.Target;

                // 既存のアニメーションがあればキャンセル
                if (!_activeAnimations.ContainsKey(targetObj))
                {
                    _activeAnimations[targetObj] = new TransformAnimation
                    {
                        StartScale = targetObj.transform.localScale,
                        TargetScale = Vector3.Scale(targetObj.transform.localScale, effectTarget.TargetScale) * itemData.EffectIntensity,
                        Progress = 0f,
                        IsActive = true
                    };
                }
                else
                {
                    // 既にアニメーション中の場合は再開
                    _activeAnimations[targetObj].IsActive = true;
                }

                if (_debugMode)
                {
                    Debug.Log($"[SpotEffectController] 縮小開始: {targetObj.name}, 強度={itemData.EffectIntensity}", this);
                }
            }
        }

        /// <summary>
        /// パーティクルエフェクトを発動
        /// </summary>
        private void TriggerParticleEffect(List<EffectTarget> targets, ItemData itemData)
        {
            var matchingTargets = FindMatchingTargets(targets, itemData.EffectTargetName);

            foreach (var effectTarget in matchingTargets)
            {
                if (effectTarget.ParticleSystem == null)
                {
                    continue;
                }

                // パーティクルシステムの設定を調整
                var main = effectTarget.ParticleSystem.main;
                var emission = effectTarget.ParticleSystem.emission;

                // 強度に応じて放出量を調整
                float originalRate = emission.rateOverTimeMultiplier;
                emission.rateOverTimeMultiplier = originalRate * itemData.EffectIntensity;

                effectTarget.ParticleSystem.Play();

                if (_debugMode)
                {
                    Debug.Log($"[SpotEffectController] パーティクル再生: {effectTarget.ParticleSystem.name}, 強度={itemData.EffectIntensity}", this);
                }
            }
        }

        /// <summary>
        /// 表示/非表示エフェクトを発動
        /// </summary>
        private void TriggerToggleEffect(List<EffectTarget> targets, ItemData itemData, bool show)
        {
            var matchingTargets = FindMatchingTargets(targets, itemData.EffectTargetName);

            foreach (var effectTarget in matchingTargets)
            {
                if (effectTarget.Target == null)
                {
                    continue;
                }

                effectTarget.Target.SetActive(show);

                if (_debugMode)
                {
                    Debug.Log($"[SpotEffectController] 表示切替: {effectTarget.Target.name}, 表示={show}", this);
                }
            }
        }

        /// <summary>
        /// ターゲット名に一致する対象を検索（空文字の場合は全て返す）
        /// </summary>
        private List<EffectTarget> FindMatchingTargets(List<EffectTarget> targets, string targetName)
        {
            if (string.IsNullOrEmpty(targetName))
            {
                return targets;
            }

            return targets.Where(t => t.TargetName == targetName).ToList();
        }

        void Update()
        {
            // アクティブなアニメーションを更新
            var completedAnimations = new List<GameObject>();

            foreach (var kvp in _activeAnimations)
            {
                if (!kvp.Value.IsActive)
                {
                    continue;
                }

                var targetObj = kvp.Key;
                var anim = kvp.Value;

                anim.Progress += Time.deltaTime / _transformDuration;

                if (anim.Progress >= 1.0f)
                {
                    anim.Progress = 1.0f;
                    anim.IsActive = false;
                    completedAnimations.Add(targetObj);
                }

                float easedProgress = _transformCurve.Evaluate(anim.Progress);
                targetObj.transform.localScale = Vector3.Lerp(anim.StartScale, anim.TargetScale, easedProgress);
            }

            // 完了したアニメーションをクリーンアップ
            foreach (var obj in completedAnimations)
            {
                if (_debugMode)
                {
                    Debug.Log($"[SpotEffectController] アニメーション完了: {obj.name}", this);
                }
            }
        }

        /// <summary>
        /// エフェクトをリセット（デバッグ用）
        /// </summary>
        [ContextMenu("Reset All Effects")]
        public void ResetEffects()
        {
            // アニメーションを全てリセット
            foreach (var kvp in _activeAnimations)
            {
                if (kvp.Key != null)
                {
                    kvp.Key.transform.localScale = kvp.Value.StartScale;
                }
            }

            _activeAnimations.Clear();

            // パーティクルを全て停止
            StopAllParticles(_smokeTargets);
            StopAllParticles(_waterTargets);

            Debug.Log($"[SpotEffectController] Spot {_spotId}: エフェクトをリセットしました", this);
        }

        private void StopAllParticles(List<EffectTarget> targets)
        {
            foreach (var effectTarget in targets)
            {
                if (effectTarget.ParticleSystem != null)
                {
                    effectTarget.ParticleSystem.Stop();
                    effectTarget.ParticleSystem.Clear();
                }
            }
        }
    }
}