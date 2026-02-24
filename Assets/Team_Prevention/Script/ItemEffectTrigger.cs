using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace Assets.Team_Prevention.Script
{
    /// <summary>
    /// XRGrabInteractable で掴んでいる最中にコントローラーのトリガーを押すと、
    /// 指定した ParticleSystem エフェクトを再生し、Spotのエフェクトをトリガーするコンポーネント。
    /// </summary>
    [RequireComponent(typeof(XRGrabInteractable))]
    public class ItemEffectTrigger : MonoBehaviour
    {
        /// <summary>
        /// このアイテムのエフェクト再生が完了したときに呼ばれる（成功/失敗に関わらず「再生処理が終わった」）
        /// </summary>
        public event Action<ItemEffectTrigger> OnEffectCompleted;

        [Header("エフェクト設定")]
        [Tooltip("再生する ParticleSystem（未設定時は子から自動取得）")]
        [SerializeField] private ParticleSystem _effectPrefab;

        [Tooltip("エフェクトを生成する位置（未設定時はこのオブジェクトの位置）")]
        [SerializeField] private Transform _effectSpawnPoint;

        [Header("エフェクトの出現方式")]
        [Tooltip("エフェクトを「オブジェクトに追従」させるか、「ワールドに固定」するかを選択します。")]
        [SerializeField] private EffectSpawnMode _effectSpawnMode = EffectSpawnMode.Follow;

        [Tooltip("SpawnMode が WorldAtFixedPoint のとき、ここに指定した Transform のワールド座標に固定生成します（オブジェクト位置に依存しない）。")]
        [SerializeField] private Transform _fixedWorldSpawnPoint;

        public enum EffectSpawnMode
        {
            Follow = 0,
            WorldAtSpawnPoint = 1,
            WorldAtFixedPoint = 2
        }

        [Tooltip("エフェクトを複数回再生できるか")]
        [SerializeField] private bool _allowMultipleTriggers = true;

        [Tooltip("エフェクト再生後の待機時間（秒）")]
        [SerializeField] private float _cooldownTime = 1.0f;

        [Header("追加挙動")]
        [Tooltip("エフェクト完了後にこの生成オブジェクトを削除するか（手元から消す）")]
        [SerializeField] private bool _destroySelfAfterEffect = true;

        [Header("入力設定")]
        [Tooltip("トリガーボタンの入力閾値（0.0 ~ 1.0）")]
        [SerializeField] private float _triggerThreshold = 0.5f;

        [Tooltip("トリガー入力に使用する Input Action Reference（未設定時は activate を使用）")]
        [SerializeField] private InputActionReference _triggerActionReference;

        [Header("PlayerInfo連携")]
        [Tooltip("PlayerInfo への参照（FindObjectOfType で自動取得可能）")]
        [SerializeField] private PlayerInfo _playerInfo;

        [Header("デバッグ（Spot未確定=0でもSpot演出を確認したい場合）")]
        [Tooltip("true のとき、currentSpotId が 0 の場合でもSpot側エフェクト（変形など）を確認できるようにします。")]
        [SerializeField] private bool _debugEnableSpotPreviewWhenZero = false;

        [Tooltip("currentSpotId が 0 の場合に、ここで指定した Spot ID として扱います（1/2/3）。0 は無効。")]
        [SerializeField] private int _debugFallbackSpotIdWhenZero = 1;

        [Tooltip("true のとき、Spot未確定(0)でのデバッグ確認ではインベントリを消費（UsedItem）しません。")]
        [SerializeField] private bool _debugDoNotConsumeInventoryWhenZero = true;

        private XRGrabInteractable _grabInteractable;
        private IXRSelectInteractor _currentInteractor;
        private bool _hasTriggered = false;
        private float _lastTriggerTime = 0f;
        private InputAction _triggerAction;
        private ItemDataHolder _itemDataHolder;

        private Coroutine _effectCompletionRoutine;

        private void Awake()
        {
            _grabInteractable = GetComponent<XRGrabInteractable>();
            _itemDataHolder = GetComponent<ItemDataHolder>();

            if (_effectPrefab == null)
            {
                _effectPrefab = GetComponentInChildren<ParticleSystem>();
            }

            if (_effectSpawnPoint == null)
            {
                _effectSpawnPoint = transform;
            }

            if (_playerInfo == null)
            {
                _playerInfo = FindFirstObjectByType<PlayerInfo>();
            }
        }

        private void OnEnable()
        {
            if (_grabInteractable != null)
            {
                _grabInteractable.selectEntered.AddListener(OnSelectEntered);
                _grabInteractable.selectExited.AddListener(OnSelectExited);
            }

            if (_triggerActionReference != null)
            {
                _triggerAction = _triggerActionReference.action;
                if (_triggerAction != null)
                {
                    _triggerAction.Enable();
                }
            }
        }

        private void OnDisable()
        {
            if (_grabInteractable != null)
            {
                _grabInteractable.selectEntered.RemoveListener(OnSelectEntered);
                _grabInteractable.selectExited.RemoveListener(OnSelectExited);
            }

            if (_triggerAction != null)
            {
                _triggerAction.Disable();
            }

            if (_effectCompletionRoutine != null)
            {
                StopCoroutine(_effectCompletionRoutine);
                _effectCompletionRoutine = null;
            }
        }

        private void OnSelectEntered(SelectEnterEventArgs args)
        {
            _currentInteractor = args.interactorObject;
            _hasTriggered = false;

            // Interactor から Input Action を取得（未設定時のフォールバック）
            if (_triggerActionReference == null && _currentInteractor is XRBaseInputInteractor inputInteractor)
            {
                // activateActionValue プロパティから InputActionProperty を取得
                var activateProperty = GetActivateAction(inputInteractor);
                if (activateProperty != null)
                {
                    _triggerAction = activateProperty;
                    if (_triggerAction != null)
                    {
                        _triggerAction.Enable();
                    }
                }
            }
        }

        /// <summary>
        /// オブジェクトが離されたときの処理
        /// </summary>
        private void OnSelectExited(SelectExitEventArgs args)
        {
            _currentInteractor = null;

            // フォールバックで取得した Action をクリーンアップ
            if (_triggerActionReference == null && _triggerAction != null)
            {
                _triggerAction.Disable();
                _triggerAction = null;
            }
        }

        private void Update()
        {
            // オブジェクトが掴まれていない場合は処理しない
            if (_currentInteractor == null)
            {
                return;
            }

            // 複数回トリガー不可で既に発動済みの場合はスキップ
            if (!_allowMultipleTriggers && _hasTriggered)
            {
                return;
            }

            // クールダウン中の場合はスキップ
            if (Time.time - _lastTriggerTime < _cooldownTime)
            {
                return;
            }

            // コントローラーのトリガー入力をチェック
            if (IsTriggerPressed())
            {
                float duration = PlayEffect();
                TriggerSpotEffect();

                if (_effectCompletionRoutine != null)
                {
                    StopCoroutine(_effectCompletionRoutine);
                }

                _effectCompletionRoutine = StartCoroutine(InvokeEffectCompletedAfter(duration));

                _hasTriggered = true;
                _lastTriggerTime = Time.time;
            }
        }

        private IEnumerator InvokeEffectCompletedAfter(float duration)
        {
            if (duration > 0f)
            {
                yield return new WaitForSeconds(duration);
            }
            else
            {
                yield return null;
            }

            OnEffectCompleted?.Invoke(this);

            // エフェクト完了後、自分自身（手元の生成オブジェクト）を削除する設定なら実行
            if (_destroySelfAfterEffect)
            {
                // 次フレームで選択解除してから破棄する（XRの内部状態回避）
                yield return null;
                TryDeselectAndDestroySelf();
            }
        }

        private void TryDeselectAndDestroySelf()
        {
            try
            {
                // まずは interactionManager 経由で SelectExit を試みる
                var manager = _grabInteractable != null && _grabInteractable.interactionManager != null
                    ? _grabInteractable.interactionManager
                    : FindFirstObjectByType<XRInteractionManager>();

                if (manager != null && _currentInteractor != null)
                {
                    manager.SelectExit(_currentInteractor, _grabInteractable);
                    return;
                }
                // フォールバック：Interactor に EndManualInteraction があれば呼ぶ
                if (_currentInteractor is XRBaseInteractor xrBase && xrBase != null)
                {
                    var method = xrBase.GetType().GetMethod("EndManualInteraction");
                    if (method != null)
                    {
                        method.Invoke(xrBase, null);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[ItemEffectTrigger] 選択解除に失敗しました: {ex}", this);
            }
            finally
            {
                // 最終的に自分自身を破棄（nullチェックして安全に）
                if (this != null && this.gameObject != null)
                {
                    Destroy(this.gameObject);
                }
            }
        }

        private InputAction GetActivateAction(XRBaseInputInteractor interactor)
        {
            if (interactor == null)
            {
                return null;
            }

            // リフレクションで activateActionValue プロパティを取得
            var propertyInfo = interactor.GetType().GetProperty("activateActionValue");
            if (propertyInfo != null)
            {
                var actionProperty = propertyInfo.GetValue(interactor);
                if (actionProperty is InputActionProperty inputActionProperty)
                {
                    return inputActionProperty.action;
                }
            }

            return null;
        }

        /// <summary>
        /// コントローラーのトリガーが押されているかチェック
        /// </summary>
        private bool IsTriggerPressed()
        {
            if (_triggerAction == null)
            {
                return false;
            }

            // Input Action の値を取得（float 値）
            float triggerValue = _triggerAction.ReadValue<float>();
            return triggerValue >= _triggerThreshold;
        }

        /// <summary>
        /// エフェクトを再生。待機用の duration を返す（再生できない場合は 0）。
        /// </summary>
        private float PlayEffect()
        {
            if (_effectPrefab == null)
            {
                Debug.LogWarning($"[ItemEffectTrigger] ParticleSystem が設定されていません: {gameObject.name}", this);
                return 0f;
            }

            Transform spawnPoint = _effectSpawnPoint != null ? _effectSpawnPoint : transform;

            ParticleSystem effectInstance;

            try
            {
                Vector3 p = new Vector3();
                Quaternion r = new Quaternion();
                switch (_effectSpawnMode)
                {
                    case EffectSpawnMode.Follow:
                    {
                        // 子として生成（手/アイテムの動きに追従）
                        effectInstance = Instantiate(_effectPrefab, spawnPoint);
                        effectInstance.transform.localPosition = Vector3.zero;
                        effectInstance.transform.localRotation = Quaternion.identity;
                        break;
                    }

                    case EffectSpawnMode.WorldAtFixedPoint:
                    {
                        if (_fixedWorldSpawnPoint == null)
                        {
                            Debug.LogWarning("[ItemEffectTrigger] SpawnMode=WorldAtFixedPoint ですが、_fixedWorldSpawnPoint が未設定です。WorldAtSpawnPoint として生成します。", this);
                            p = spawnPoint.position;
                            r = spawnPoint.rotation;
                            effectInstance = Instantiate(_effectPrefab, p, r);
                            break;
                        }

                        p = _fixedWorldSpawnPoint.position;
                        r = _fixedWorldSpawnPoint.rotation;
                        effectInstance = Instantiate(_effectPrefab, p, r);
                        break;
                    }

                    case EffectSpawnMode.WorldAtSpawnPoint:
                    default:
                    {
                        // 生成時点のワールド座標に固定（その場に残る）
                        p = spawnPoint.position;
                        r = spawnPoint.rotation;
                        effectInstance = Instantiate(_effectPrefab, p, r);
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[ItemEffectTrigger] エフェクト生成に失敗しました: {ex}", this);
                return 0f;
            }

            if (effectInstance == null)
            {
                Debug.LogWarning("[ItemEffectTrigger] エフェクトの生成結果が null です。", this);
                return 0f;
            }

            effectInstance.Play();

            // 自動削除（ParticleSystem の duration に合わせる）
            float duration = effectInstance.main.duration + effectInstance.main.startLifetime.constantMax;
            Destroy(effectInstance.gameObject, duration);

            Debug.Log($"[ItemEffectTrigger] エフェクト再生: {gameObject.name}, Mode={_effectSpawnMode}", this);

            return Mathf.Max(0f, duration);
        }

        /// <summary>
        /// Spotのエフェクトをトリガー
        /// </summary>
        private void TriggerSpotEffect()
        {
            if (_playerInfo == null)
            {
                Debug.LogWarning($"[ItemEffectTrigger] PlayerInfo が設定されていません: {gameObject.name}", this);
                return;
            }

            if (_itemDataHolder == null || _itemDataHolder.ItemData == null)
            {
                Debug.LogWarning($"[ItemEffectTrigger] ItemDataHolder が設定されていません: {gameObject.name}", this);
                return;
            }

            ItemData itemData = _itemDataHolder.ItemData;

            // PlayerInfo から現在のSpot IDを取得
            var itemBoxComponent = _playerInfo.GetComponentInChildren<UI.ItemBoxComponent>();
            if (itemBoxComponent == null)
            {
                Debug.LogWarning($"[ItemEffectTrigger] ItemBoxComponent が見つかりません", this);
                return;
            }

            int currentSpotId = itemBoxComponent.currentSpotId;

            var isZeroSpot = (currentSpotId == 0);
            if (isZeroSpot && _debugEnableSpotPreviewWhenZero)
            {
                if (_debugFallbackSpotIdWhenZero <= 0)
                {
                    Debug.LogWarning("[ItemEffectTrigger] デバッグSpotプレビューが有効ですが、Fallback Spot ID が不正です。", this);
                }
                else
                {
                    currentSpotId = _debugFallbackSpotIdWhenZero;
                }
            }

            bool isCorrectSpot = (itemData.CorrectUseSpotId == currentSpotId);

            // SpotEffectController を取得してエフェクトを発動
            var spotController = SpotEffectController.GetControllerForSpot(currentSpotId);
            if (spotController != null)
            {
                spotController.TriggerEffect(itemData, isCorrectSpot);
                Debug.Log($"[ItemEffectTrigger] Spotエフェクト発動: Spot={currentSpotId}, アイテム={itemData.Name}, 正解={isCorrectSpot}", this);
            }
            else
            {
                Debug.LogWarning($"[ItemEffectTrigger] Spot {currentSpotId} のコントローラーが見つかりません", this);
            }

            // デバッグ確認（spot=0）ではインベントリ消費を抑制（副作用対策）
            if (isZeroSpot && _debugEnableSpotPreviewWhenZero && _debugDoNotConsumeInventoryWhenZero)
            {
                return;
            }

            _playerInfo.UsedItem(itemData, currentSpotId);
        }

        private void OnValidate()
        {
            _triggerThreshold = Mathf.Clamp01(_triggerThreshold);
            _cooldownTime = Mathf.Max(0f, _cooldownTime);

            if (_debugFallbackSpotIdWhenZero < 0)
            {
                _debugFallbackSpotIdWhenZero = 0;
            }
        }
    }
}