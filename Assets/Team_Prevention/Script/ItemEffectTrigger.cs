using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace Assets.Team_Prevention.Script
{
    /// <summary>
    /// XRGrabInteractable で掴んでいる最中にコントローラーのトリガーを押すと、
    /// 指定した ParticleSystem エフェクトを再生するコンポーネント。
    /// </summary>
    [RequireComponent(typeof(XRGrabInteractable))]
    public class ItemEffectTrigger : MonoBehaviour
    {
        [Header("エフェクト設定")]
        [Tooltip("再生する ParticleSystem（未設定時は子から自動取得）")]
        [SerializeField] private ParticleSystem _effectPrefab;

        [Tooltip("エフェクトを生成する位置（未設定時はこのオブジェクトの位置）")]
        [SerializeField] private Transform _effectSpawnPoint;

        [Tooltip("エフェクトを複数回再生できるか")]
        [SerializeField] private bool _allowMultipleTriggers = true;

        [Tooltip("エフェクト再生後の待機時間（秒）")]
        [SerializeField] private float _cooldownTime = 1.0f;

        [Header("入力設定")]
        [Tooltip("トリガーボタンの入力閾値（0.0 ~ 1.0）")]
        [SerializeField] private float _triggerThreshold = 0.5f;

        [Tooltip("トリガー入力に使用する Input Action Reference（未設定時は activate を使用）")]
        [SerializeField] private InputActionReference _triggerActionReference;

        private XRGrabInteractable _grabInteractable;
        private IXRSelectInteractor _currentInteractor;
        private bool _hasTriggered = false;
        private float _lastTriggerTime = 0f;
        private InputAction _triggerAction;

        private void Awake()
        {
            // XRGrabInteractable を取得
            _grabInteractable = GetComponent<XRGrabInteractable>();

            // エフェクトが未設定なら子から取得
            if (_effectPrefab == null)
            {
                _effectPrefab = GetComponentInChildren<ParticleSystem>();
            }

            // スポーン位置が未設定なら自身を使用
            if (_effectSpawnPoint == null)
            {
                _effectSpawnPoint = transform;
            }
        }

        private void OnEnable()
        {
            if (_grabInteractable != null)
            {
                _grabInteractable.selectEntered.AddListener(OnSelectEntered);
                _grabInteractable.selectExited.AddListener(OnSelectExited);
            }

            // Input Action Reference が設定されている場合は有効化
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

            // Input Action の無効化
            if (_triggerAction != null)
            {
                _triggerAction.Disable();
            }
        }

        /// <summary>
        /// オブジェクトが掴まれたときの処理
        /// </summary>
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
                PlayEffect();
                _hasTriggered = true;
                _lastTriggerTime = Time.time;
            }
        }

        /// <summary>
        /// XRBaseInputInteractor から activate アクションを取得
        /// </summary>
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
        /// エフェクトを再生
        /// </summary>
        private void PlayEffect()
        {
            if (_effectPrefab == null)
            {
                Debug.LogWarning($"[ItemEffectTrigger] ParticleSystem が設定されていません: {gameObject.name}", this);
                return;
            }

            // エフェクトの位置を決定
            Vector3 spawnPosition = _effectSpawnPoint != null ? _effectSpawnPoint.position : transform.position;
            Quaternion spawnRotation = _effectSpawnPoint != null ? _effectSpawnPoint.rotation : transform.rotation;

            // ParticleSystem を生成して再生
            ParticleSystem effectInstance = Instantiate(_effectPrefab, spawnPosition, spawnRotation);
            effectInstance.Play();

            // 自動削除（ParticleSystem の duration に合わせる）
            float duration = effectInstance.main.duration + effectInstance.main.startLifetime.constantMax;
            Destroy(effectInstance.gameObject, duration);

            Debug.Log($"[ItemEffectTrigger] エフェクト再生: {gameObject.name}", this);
        }

        private void OnValidate()
        {
            // Inspector での設定ミスを防ぐ
            _triggerThreshold = Mathf.Clamp01(_triggerThreshold);
            _cooldownTime = Mathf.Max(0f, _cooldownTime);
        }
    }
}