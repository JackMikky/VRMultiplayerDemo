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
            SetFadeValue(0);

            LocalManager.Instance.onLobbyLoadStart.AddOnceListener(() =>
            {
                StartWarpFadeIn("Lobby");
            });
        }

        [System.Obsolete]
        private void Start()
        {
            _fadeValue.Value = 0;

            _fadeValue.Subscribe(SetFadeValue);

            XRINetworkGameManager.Instance.networkSceneManager.onSceneLoaded.AddListener((sceneName) =>
            {
                StartWarpFadeIn(sceneName);
            });
        }

        public void StartFadeOut(string scene)
        {
            StartWarpFadeOut(scene);
        }

        private IEnumerator InvokeWarpFadeOutAfterDelay(string sceneName)
        {
            yield return new WaitForSeconds(fadeOutWaitTime);

            if (onWarpFadeOutComplete != null)
                onWarpFadeOutComplete.Invoke(sceneName);
        }

        private IEnumerator InvokeWarpFadeInAfterDelay(string sceneName)
        {
            yield return new WaitForSeconds(fadeInWaitTime);

            if (onWarpFadeInComplete != null)
                onWarpFadeInComplete.Invoke(sceneName);
        }

        private void StartWarpFadeOut(string sceneName)
        {
            onWarpFadeOutStart.Invoke(sceneName);
            StartCoroutine(_fadeValue.PlaySequence(0, 1, fadeTime,
                () => StartCoroutine(InvokeWarpFadeOutAfterDelay(sceneName))));
        }

        private void StartWarpFadeIn(string sceneName)
        {
            onWarpFadeInStart.Invoke(sceneName);
            StartCoroutine(_fadeValue.PlaySequence(1, 0, fadeTime, () => StartCoroutine(InvokeWarpFadeInAfterDelay(sceneName))));
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