using System.Collections;
using UnityEngine;
using UnityEngine.Events;
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
        public CustomEvent onWarpFadeOutStart;

        [Tooltip("Triggered when the warp fade-out is fully complete (screen is completely dark)")]
        public CustomEvent<string> onWarpFadeOutComplete;

        [Tooltip("Triggered when the warp fade-in begins (screen starts to brighten)")]
        public CustomEvent onWarpFadeInStart;

        [Tooltip("Triggered when the warp fade-in is fully complete (screen is fully visible again)")]
        public CustomEvent onWarpFadeInComplete;

        [System.Obsolete] private readonly FloatTweenableVariable _fadeValue = new FloatTweenableVariable();

        private const string fadeProperty = "_T";

        private void Awake()
        {
            SetFadeValue(0);

            LocalManager.Instance.onLobbyLoaded.AddListener(() =>
            {
                StartWarpFadeIn();
            });
        }

        [System.Obsolete]
        private void Start()
        {
            _fadeValue.Value = 0;

            _fadeValue.Subscribe(SetFadeValue);

            XRINetworkGameManager.Instance.networkSceneManager.onSceneLoaded.AddListener((sceneName) =>
            {
                StartWarpFadeIn();
            });
        }

        public void StartFadeOutBySceneID(string sceneID)
        {
            StartWarpFadeOut(sceneID);
        }

        private IEnumerator InvokeWarpFadeOutAfterDelay(string sceneID)
        {
            yield return new WaitForSeconds(fadeOutWaitTime);

            if (onWarpFadeOutComplete != null)
                onWarpFadeOutComplete.Invoke(sceneID);
        }

        private IEnumerator InvokeWarpFadeInAfterDelay()
        {
            yield return new WaitForSeconds(fadeInWaitTime);

            if (onWarpFadeInComplete != null)
                onWarpFadeInComplete.Invoke();
        }

        private void StartWarpFadeOut(string sceneID)
        {
            onWarpFadeOutStart.Invoke();
            StartCoroutine(_fadeValue.PlaySequence(0, 1, fadeTime,
                () => StartCoroutine(InvokeWarpFadeOutAfterDelay(sceneID))));
        }

        private void StartWarpFadeIn()
        {
            onWarpFadeInStart.Invoke();
            StartCoroutine(_fadeValue.PlaySequence(1, 0, fadeTime, () => StartCoroutine(InvokeWarpFadeInAfterDelay())));
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
            fullScreenMat.SetFloat(fadeProperty, 0);
            this.onWarpFadeInComplete.RemoveAllListeners();
            this.onWarpFadeOutComplete.RemoveAllListeners();
            this._fadeValue.Dispose();
        }
    }
}