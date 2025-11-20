using System.Collections;
using UnityEditor.SearchService;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Interaction.Toolkit.Utilities.Tweenables.Primitives;

namespace XRMultiplayer
{
    public class WarpController : MonoBehaviour
    {
        [SerializeField] private Material fullScreenMat;

        [Range(0, 10)][SerializeField] private float fadeTime = 1;

        [Range(0, 5)][SerializeField] private float fadeInWaitTime = 1;

        [Range(0, 5)][SerializeField] private float fadeOutWaitTime = 1;

        [Space(10)]
        [Header("Events")]
        [Space(10)]
        [Tooltip("Triggered when the warp fade-out begins (screen starts going dark)")]
        public CustomEvent<string> onWarpFadeOutStart;

        [Tooltip("Triggered when the warp fade-out is fully complete (screen is completely dark)")]
        public CustomEvent<string> onWarpFadeOutComplete;

        [Tooltip("Triggered when the warp fade-in begins (screen starts to brighten)")]
        public CustomEvent<string> onWarpFadeInStart;

        [Tooltip("Triggered when the warp fade-in is fully complete (screen is fully visible again)")]
        public CustomEvent<string> onWarpFadeInComplete;

        [System.Obsolete] private readonly FloatTweenableVariable _fadeValue = new FloatTweenableVariable();

        private const string fadeProperty = "_T";

        private void Awake()
        {
            SetFadeValue(1);
        }

        [System.Obsolete]
        private void Start()
        {
            _fadeValue.Value = 1;
            StartWarpFadeIn("Entrance");

            _fadeValue.Subscribe(SetFadeValue);
        }

        public void StartFadeOut(string scene, UnityAction<string> onCompleted = null)
        {
            StartWarpFadeOut(scene, onCompleted);
        }

        public void StartFadeIn(string scene, UnityAction<string> onCompleted = null)
        {
            StartWarpFadeIn(scene, onCompleted);
        }

        private IEnumerator InvokeWarpFadeOutAfterDelay(string sceneName, UnityAction<string> unityAction = null)
        {
            yield return new WaitForSeconds(fadeOutWaitTime);

            if (onWarpFadeOutComplete != null)
            {
                onWarpFadeOutComplete.Invoke(sceneName);
                unityAction?.Invoke(sceneName);
            }
        }

        private IEnumerator InvokeWarpFadeInAfterDelay(string sceneName, UnityAction<string> unityAction = null)
        {
            yield return new WaitForSeconds(fadeInWaitTime);

            if (onWarpFadeInComplete != null)
            {
                onWarpFadeInComplete.Invoke(sceneName);
                unityAction?.Invoke(sceneName);
            }
        }

        private void StartWarpFadeOut(string sceneName, UnityAction<string> onCompleted = null, bool invokeEvent = true)
        {
            if (invokeEvent)
            {
                onWarpFadeOutStart.Invoke(sceneName);
            }
            StartCoroutine(_fadeValue.PlaySequence(0, 1, fadeTime,
                () =>
                {
                    StartCoroutine(InvokeWarpFadeOutAfterDelay(sceneName, onCompleted));
                }));
        }

        private void StartWarpFadeIn(string sceneName, UnityAction<string> onCompleted = null, bool invokeEvent = true)
        {
            if (invokeEvent)
            {
                onWarpFadeInStart.Invoke(sceneName);
            }
            StartCoroutine(_fadeValue.PlaySequence(1, 0, fadeTime, () =>
            {
                StartCoroutine(InvokeWarpFadeInAfterDelay(sceneName, onCompleted));
            }));
        }

        private void SetFadeValue(float value)
        {
            if (fullScreenMat.HasProperty(fadeProperty))
            {
                fullScreenMat.SetFloat(fadeProperty, value);
            }
        }

        [System.Obsolete]
        private void OnDestroy()
        {
#if UNITY_EDITOR
            fullScreenMat.SetFloat(fadeProperty, 0);
#else
            fullScreenMat.SetFloat(fadeProperty, 1);
#endif
            this.onWarpFadeInComplete.RemoveAllListeners();
            this.onWarpFadeOutComplete.RemoveAllListeners();
            this._fadeValue.Dispose();
        }
    }
}