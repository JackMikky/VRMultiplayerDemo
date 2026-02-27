using System;
using System.Collections;
using Assets.Team_Prevention.Script.Effects;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace Assets.Team_Prevention.Script
{
    /// <summary>
    /// XRGrabInteractable で掴んでいる最中にコントローラーのトリガーを押すと、
    /// エフェクトを再生し、Spotのエフェクトをトリガーするコンポーネント。
    /// FollowBody / WorldFixed などの SpawnMode でも動作するよう、
    /// 「掴まれているか」ではなく「アクティブ化済みか」で判断します。
    /// </summary>
    [RequireComponent(typeof(XRGrabInteractable))]
    public class ItemEffectTrigger : MonoBehaviour
    {
        /// <summary>
        /// このアイテムのエフェクト再生が完了したときに呼ばれる（成功/失敗に関わらず「再生処理が終わった」）
        /// </summary>
        public event Action<ItemEffectTrigger> OnEffectCompleted;

        [Header("エフェクト設定（Particle）")]
        [Tooltip("再生する ParticleSystem（未設定時は子から自動取得）")]
        [SerializeField] private ParticleSystem _effectPrefab;

        [Tooltip("エフェクトを生成する位置（未設定時はこのオブジェクトの位置）")]
        [SerializeField] private Transform _effectSpawnPoint;

        [Header("エフェクトの出現方式（Particle）")]
        [Tooltip("エフェクトを「オブジェクトに追従」させるか、「ワールドに固定」するかを選択します。")]
        [SerializeField] private EffectSpawnMode _effectSpawnMode = EffectSpawnMode.Follow;

        [Header("効果音（エフェクト開始時）")]
        [Tooltip("SE再生に使う AudioSource（未設定時は自動で追加/取得します）")]
        [SerializeField] private AudioSource _seAudioSource;

        [Tooltip("トリガー押下（エフェクト開始）時に鳴らすSE")]
        [SerializeField] private AudioClip _onTriggerSe;

        [Tooltip("SE音量（PlayOneShotのvolumeScale）")]
        [Range(0f, 1f)]
        [SerializeField] private float _onTriggerSeVolume = 1.0f;

        [Header("汎用エフェクト（IEffectPlayer）")]
        [Tooltip("子要素に付いている IEffectPlayer を自動取得して同時再生します。")]
        [SerializeField] private bool _playChildEffectPlayers = true;

        [Tooltip("SpawnMode が WorldAtFixedPoint のとき、ここに指定した Transform のワールド座標に固定生成します（オブジェクト位置に依存しない）。")]
        [SerializeField] private Transform _fixedWorldSpawnPoint;

        /// <summary>
        /// エフェクトの出現モードを定義します。
        /// </summary>
        public enum EffectSpawnMode
        {
            /// <summary>
            /// Follow (0)
            /// 発生元オブジェクトに追従して生成します。生成時に spawn point の子オブジェクトとして作成し、
            /// その後も位置・回転を追従します。
            /// 用途例: 所持中のアイテムに付随する継続的なエフェクト（光、オーラ等）。
            /// </summary>
            Follow = 0,

            /// <summary>
            /// WorldAtSpawnPoint (1)
            /// 発生時の spawn point のワールド位置・回転をコピーして独立して生成します（以後追従しない）。
            /// 用途例: 生成時点の位置に固定したい一時的エフェクト（爆発、着弾エフェクト等）。
            /// </summary>
            WorldAtSpawnPoint = 1,

            /// <summary>
            /// WorldAtFixedPoint (2)
            /// あらかじめ指定した固定ワールド座標（_fixedWorldSpawnPoint）に生成します。
            /// 発生元の位置に依存せず常に同じ場所に出したいエフェクト向け。
            /// 用途例: ステージ上の特定地点でのみ表示するエフェクト。
            /// </summary>
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

        [Tooltip("true のとき、このコンポーネントが _triggerActionReference.action を Enable/Disable します。false のとき参照のみ（所有しない）")]
        [SerializeField] private bool _manageTriggerActionEnabledState = false;

        [Header("生成後の自動開始（任意）")]
        [Tooltip("true のとき、トリガー入力ではなく「手元に生成（アクティブ化）後」に自動でエフェクトを開始します。")]
        [SerializeField] private bool _autoStartAfterActivated = true;

        [Tooltip("_autoStartAfterActivated が true のときの遅延秒数")]
        [SerializeField] private float _autoStartDelaySeconds = 1.5f;

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
        private Coroutine _autoStartRoutine;

        /// <summary>
        /// true のとき、このアイテムは「手元に出現済み（使用可能）」状態です。
        /// FollowHand: OnSelectEntered で true になります。
        /// FollowBody / WorldFixed: SetCurrentInteractorForFollowMode で true になります。
        /// </summary>
        private bool _isActivated = false;

        /// <summary>
        /// しきい値を跨いだ瞬間（押下エッジ）検出用
        /// </summary>
        private bool _wasTriggerPressed = false;

        /// <summary>
        /// _triggerAction を自分で Enable/Disable して良いか（所有しているか）
        /// - InputActionReference から来た場合は「このコンポーネントの所有物」とみなし制御してよい
        /// - Interactor から拾った activateActionValue は共有物になり得るため制御しない
        /// </summary>
        private bool _ownsTriggerAction = false;

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

            // SE用 AudioSource の準備（未設定なら自動付与）
            if (_seAudioSource == null)
            {
                _seAudioSource = GetComponent<AudioSource>();
                if (_seAudioSource == null)
                {
                    _seAudioSource = gameObject.AddComponent<AudioSource>();
                }
            }
        }

        private void OnEnable()
        {
            if (_grabInteractable != null)
            {
                _grabInteractable.selectEntered.AddListener(OnSelectEntered);
                _grabInteractable.selectExited.AddListener(OnSelectExited);
            }

            // ActionReference が指定されている場合：参照はするが「所有」は任意
            if (_triggerActionReference != null)
            {
                _triggerAction = _triggerActionReference.action;

                // 所有判定：明示的に「管理する」設定のときのみ
                _ownsTriggerAction = _manageTriggerActionEnabledState;

                if (_ownsTriggerAction && _triggerAction != null)
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

            // 「所有している」場合のみ Disable
            if (_ownsTriggerAction && _triggerAction != null)
            {
                _triggerAction.Disable();
            }

            _triggerAction = null;
            _ownsTriggerAction = false;
            _wasTriggerPressed = false;

            StopAutoStartRoutine();

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

            // FollowHand の場合は掴み時にアクティブ化
            _isActivated = true;

            // 下記の Setup 内で共有Actionに差し替わる可能性があるので、エッジ検出状態はリセット
            _wasTriggerPressed = false;

            // Interactor から Input Action を取得（未設定時のフォールバック）
            SetupTriggerActionFromInteractor(_currentInteractor);

            TryStartAutoEffect();
        }

        /// <summary>
        /// オブジェクトが離されたときの処理
        /// </summary>
        private void OnSelectExited(SelectExitEventArgs args)
        {
            _currentInteractor = null;
            _wasTriggerPressed = false;

            StopAutoStartRoutine();

            // Interactor から拾った共有Actionは Disable しない（他の動作と両立するため）
            if (_triggerActionReference == null)
            {
                _triggerAction = null;
                _ownsTriggerAction = false;
            }
        }

        private void Update()
        {
            // 自動開始モードの場合、入力で開始しない
            if (_autoStartAfterActivated)
            {
                return;
            }

            // 手元に出現済み（アクティブ化）でない場合は処理しない
            if (!_isActivated)
            {
                return;
            }

            // 押下エッジでのみ発火（押しっぱなしで連続発火しない）
            if (!WasTriggerJustPressed())
            {
                return;
            }

            TryTriggerEffect();
        }

        private void TryStartAutoEffect()
        {
            if (!_autoStartAfterActivated)
            {
                return;
            }

            if (!_isActivated)
            {
                return;
            }

            StopAutoStartRoutine();
            _autoStartRoutine = StartCoroutine(AutoStartAfterDelay());
        }

        private IEnumerator AutoStartAfterDelay()
        {
            float delay = Mathf.Max(0f, _autoStartDelaySeconds);

            if (delay > 0f)
            {
                yield return new WaitForSeconds(delay);
            }
            else
            {
                yield return null;
            }

            // 待機中に非アクティブ化/破棄されていたら中断
            if (!_isActivated)
            {
                yield break;
            }

            TryTriggerEffect();
        }

        private void StopAutoStartRoutine()
        {
            if (_autoStartRoutine == null)
            {
                return;
            }

            StopCoroutine(_autoStartRoutine);
            _autoStartRoutine = null;
        }

        private void TryTriggerEffect()
        {
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

            float seDuration = PlayTriggerSeAndGetDuration();
            float particleDuration = PlayParticleEffect();

            float childEffectsDuration = _playChildEffectPlayers ? PlayChildEffectPlayers() : 0f;

            float duration = Mathf.Max(seDuration, particleDuration, childEffectsDuration);

            TriggerSpotEffect();

            if (_effectCompletionRoutine != null)
            {
                StopCoroutine(_effectCompletionRoutine);
            }

            _effectCompletionRoutine = StartCoroutine(InvokeEffectCompletedAfter(duration));

            _hasTriggered = true;
            _lastTriggerTime = Time.time;
        }

        private float PlayChildEffectPlayers()
        {
            float maxDuration = 0f;

            var monos = GetComponentsInChildren<MonoBehaviour>(true);
            if (monos == null || monos.Length == 0)
            {
                return 0f;
            }

            foreach (var mono in monos)
            {
                // null チェック・自身の除外（将来 IEffectPlayer を実装しても事故らない）
                if (mono == null || mono == this)
                {
                    continue;
                }

                if (!(mono is IEffectPlayer player))
                {
                    continue;
                }

                float d = 0f;
                try
                {
                    d = player.Play();
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[ItemEffectTrigger] IEffectPlayer.Play() に失敗しました: {ex}", this);
                }

                maxDuration = Mathf.Max(maxDuration, Mathf.Max(0f, d));
            }

            return maxDuration;
        }

        private float PlayTriggerSeAndGetDuration()
        {
            if (_seAudioSource == null || _onTriggerSe == null)
            {
                return 0f;
            }

            _seAudioSource.PlayOneShot(_onTriggerSe, _onTriggerSeVolume);
            return Mathf.Max(0f, _onTriggerSe.length);
        }

        private IEnumerator InvokeEffectCompletedAfter(float duration)
        {
            // duration が正の値の場合のみ待機
            if (duration > 0f)
            {
                yield return new WaitForSeconds(duration);
            }
            else
            {
                yield return null;
            }

            OnEffectCompleted?.Invoke(this);

            if (_destroySelfAfterEffect)
            {
                yield return null;
                TryDeselectAndDestroySelf();
            }
        }

        private void TryDeselectAndDestroySelf()
        {
            try
            {
                // InteractionManager 経由で選択解除を試みる（FollowHand のみ有効）
                var manager = _grabInteractable != null && _grabInteractable.interactionManager != null
                    ? _grabInteractable.interactionManager
                    : FindFirstObjectByType<XRInteractionManager>();

                if (manager != null && _currentInteractor != null)
                {
                    manager.SelectExit(_currentInteractor, _grabInteractable);
                    return;
                }

                // フォールバック: リフレクションで EndManualInteraction を呼び出す
                if (_currentInteractor is XRBaseInteractor xrBase)
                {
                    var method = xrBase.GetType().GetMethod("EndManualInteraction");
                    method?.Invoke(xrBase, null);
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[ItemEffectTrigger] 選択解除に失敗しました: {ex}", this);
            }
            finally
            {
                if (this != null && gameObject != null)
                {
                    Destroy(gameObject);
                }
            }
        }

        private InputAction GetActivateAction(XRBaseInputInteractor interactor)
        {
            if (interactor == null)
            {
                return null;
            }

            // リフレクションで activateActionValue プロパティから InputAction を取得
            var propertyInfo = interactor.GetType().GetProperty("activateActionValue");
            if (propertyInfo?.GetValue(interactor) is InputActionProperty inputActionProperty)
            {
                return inputActionProperty.action;
            }

            return null;
        }

        /// <summary>
        /// トリガー押下の「瞬間」を検出（しきい値を跨いだ立ち上がりのみ true）
        /// </summary>
        private bool WasTriggerJustPressed()
        {
            if (_triggerAction == null)
            {
                _wasTriggerPressed = false;
                return false;
            }

            bool isPressed = _triggerAction.ReadValue<float>() >= _triggerThreshold;
            bool justPressed = isPressed && !_wasTriggerPressed;

            _wasTriggerPressed = isPressed;
            return justPressed;
        }

        private float PlayParticleEffect()
        {
            if (_effectPrefab == null)
            {
                return 0f;
            }

            Transform spawnPoint = _effectSpawnPoint != null ? _effectSpawnPoint : transform;

            ParticleSystem effectInstance;

            try
            {
                effectInstance = SpawnParticleByMode(spawnPoint);
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

            float duration = effectInstance.main.duration + effectInstance.main.startLifetime.constantMax;
            Destroy(effectInstance.gameObject, duration);

            Debug.Log($"[ItemEffectTrigger] エフェクト再生: {gameObject.name}, Mode={_effectSpawnMode}", this);

            return Mathf.Max(0f, duration);
        }

        /// <summary>
        /// EffectSpawnMode に応じた位置・回転で ParticleSystem をインスタンス化します。
        /// </summary>
        private ParticleSystem SpawnParticleByMode(Transform spawnPoint)
        {
            switch (_effectSpawnMode)
            {
                case EffectSpawnMode.Follow:
                    {
                        // 発生元に子として追従生成
                        var instance = Instantiate(_effectPrefab, spawnPoint);
                        instance.transform.localPosition = Vector3.zero;
                        instance.transform.localRotation = Quaternion.identity;
                        return instance;
                    }

                case EffectSpawnMode.WorldAtFixedPoint:
                    {
                        if (_fixedWorldSpawnPoint == null)
                        {
                            Debug.LogWarning("[ItemEffectTrigger] SpawnMode=WorldAtFixedPoint ですが、_fixedWorldSpawnPoint が未設定です。WorldAtSpawnPoint として生成します。", this);
                            // フォールバック: spawnPoint のワールド座標を使用
                            return Instantiate(_effectPrefab, spawnPoint.position, spawnPoint.rotation);
                        }

                        return Instantiate(_effectPrefab, _fixedWorldSpawnPoint.position, _fixedWorldSpawnPoint.rotation);
                    }

                case EffectSpawnMode.WorldAtSpawnPoint:
                default:
                    {
                        return Instantiate(_effectPrefab, spawnPoint.position, spawnPoint.rotation);
                    }
            }
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

            var itemBoxComponent = _playerInfo.GetComponentInChildren<UI.ItemBoxComponent>();
            if (itemBoxComponent == null)
            {
                Debug.LogWarning("[ItemEffectTrigger] ItemBoxComponent が見つかりません", this);
                return;
            }

            int currentSpotId = itemBoxComponent.currentSpotId;

            // デバッグ: Spot未確定(0) の場合に Fallback ID を使用
            bool isZeroSpot = (currentSpotId == 0);
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

            // デバッグモードで Spot 未確定のとき、インベントリを消費しない
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
            _onTriggerSeVolume = Mathf.Clamp01(_onTriggerSeVolume);

            if (_debugFallbackSpotIdWhenZero < 0)
            {
                _debugFallbackSpotIdWhenZero = 0;
            }

            _autoStartDelaySeconds = Mathf.Max(0f, _autoStartDelaySeconds);
        }

        /// <summary>
        /// 追従方式など「XRのSelectを使わない」構成向けに、入力元Interactorを外部から注入します。
        /// FollowBody / WorldFixed の SpawnMode でアイテムが手元に出現したタイミングで呼び出してください。
        /// </summary>
        public void SetCurrentInteractorForFollowMode(IXRSelectInteractor interactor)
        {
            if (interactor == null)
            {
                Debug.LogWarning("[ItemEffectTrigger] SetCurrentInteractorForFollowMode: interactor が null です。", this);
                return;
            }

            _currentInteractor = interactor;
            _hasTriggered = false;
            _isActivated = true;

            _wasTriggerPressed = false;

            SetupTriggerActionFromInteractor(_currentInteractor);

            TryStartAutoEffect();
        }

        public void Deactivate()
        {
            _isActivated = false;
            _currentInteractor = null;
            _wasTriggerPressed = false;

            StopAutoStartRoutine();

            if (_ownsTriggerAction && _triggerAction != null)
            {
                _triggerAction.Disable();
            }

            _triggerAction = null;
            _ownsTriggerAction = false;
        }

        private void SetupTriggerActionFromInteractor(IXRSelectInteractor interactor)
        {
            if (_triggerActionReference != null)
            {
                return;
            }

            if (!(interactor is XRBaseInputInteractor inputInteractor))
            {
                return;
            }

            var action = GetActivateAction(inputInteractor);
            if (action == null)
            {
                return;
            }

            // 共有され得るActionなので、このコンポーネントでは所有しない（Enable/Disableしない）
            _triggerAction = action;
            _ownsTriggerAction = false;
        }
    }
}