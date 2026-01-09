using System;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Filtering;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace XRMultiplayer
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(XRBaseInteractable))]
    [RequireComponent(typeof(XRPokeFilter))]
    public class XRPressButton : MonoBehaviour
    {
        [Header("Press Settings")]
        [Range(0f, 1f)]
        [SerializeField] private float pressThreshold = 0.8f;

        [Range(0f, 1f)]
        [SerializeField] private float releaseThreshold = 0.2f;

        [Header("Optional Visual Travel")]
        [SerializeField] private Transform buttonObject;

        [Range(0, 1)]
        [SerializeField] private float maxTravel = 0.01f;

        [SerializeField] private Vector3 localPressAxis = new Vector3(0f, -1f, 0f);

        [Header("Events")]
        public CustomEvent onPressed;

        public CustomEvent onReleased;
        public CustomEvent<float> onPressValueChanged;

        private XRPokeFilter _pokeFilter;
        private XRBaseInteractable _interactable;
        private bool _pressed;
        private Vector3 _initialLocalPos;
        private bool _hasMoveTarget;

        private void Awake()
        {
            _interactable = GetComponent<XRBaseInteractable>();
            _pokeFilter = GetComponent<XRPokeFilter>();

            _hasMoveTarget = buttonObject != null;
            if (_hasMoveTarget)
                _initialLocalPos = buttonObject.localPosition;
        }

        private void OnEnable()
        {
            if (_pokeFilter != null && _pokeFilter.pokeStateData != null)
                _pokeFilter.pokeStateData.Subscribe(OnPokeStateChanged);
        }

        private void OnDisable()
        {
            if (_pokeFilter != null && _pokeFilter.pokeStateData != null)
                _pokeFilter.pokeStateData.Unsubscribe(OnPokeStateChanged);
        }

        private void OnPokeStateChanged(UnityEngine.XR.Interaction.Toolkit.Filtering.PokeStateData state)
        {
            var strength = Mathf.Clamp01(state.interactionStrength);

            if (_hasMoveTarget)
            {
                var offset = localPressAxis.normalized * (maxTravel * strength);
                buttonObject.localPosition = _initialLocalPos + offset;
            }

            onPressValueChanged?.Invoke(strength);

            if (!_pressed && strength >= pressThreshold)
            {
                _pressed = true;
                onPressed?.Invoke();
            }
            else if (_pressed && strength <= releaseThreshold)
            {
                _pressed = false;
                onReleased?.Invoke();
            }
        }

#if UNITY_EDITOR

        private void OnValidate()
        {
            releaseThreshold = Mathf.Clamp01(releaseThreshold);
            pressThreshold = Mathf.Clamp01(pressThreshold);
            if (releaseThreshold > pressThreshold)
                releaseThreshold = pressThreshold * 0.5f;
        }

#endif
    }
}