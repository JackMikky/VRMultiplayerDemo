using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.Interaction.Toolkit.Utilities.Tweenables.Primitives;

namespace XRMultiplayer
{
    public class WarpController : MonoBehaviour
    {
        [SerializeField] Material fullScreenMat;

        [Range(0, 10)] [SerializeField] float fadeTime = 1;

        [Range(0, 5)] [SerializeField] float waitTime = 1;

        [Tooltip("When the screen goes dark during the warp")]
        public UnityEvent<string> onWarpFadeOutComplete;

        [Tooltip("When the screen comes back after the warp")]
        public UnityEvent onWarpFadeInComplete;

        [System.Obsolete] readonly FloatTweenableVariable _fadeValue = new FloatTweenableVariable();

        const string fadeProperty = "_T";

        void Awake()
        {
            SetFadeValue(0);

            LocalManager.Instance.onLobbyLoaded.AddListener(() =>
            {
                StartCoroutine(_fadeValue.PlaySequence(1, 0, fadeTime, onWarpFadeInComplete.Invoke));
            });
        }

        [System.Obsolete]
        void Start()
        {
            _fadeValue.Value = 0;

            _fadeValue.Subscribe(SetFadeValue);

            XRINetworkGameManager.Instance.networkSceneManager.onSceneLoaded.AddListener((sceneName) =>
            {
                StartCoroutine(_fadeValue.PlaySequence(1, 0, fadeTime, onWarpFadeInComplete.Invoke));
            });
        }

        [System.Obsolete]
        public void StartFadeOutBySceneID(string sceneID)
        {
            StartCoroutine(_fadeValue.PlaySequence(0, 1, fadeTime,
                () => StartCoroutine(InvokeWarpFadeOutAfterDelay(sceneID))));
        }

        private IEnumerator InvokeWarpFadeOutAfterDelay(string sceneID)
        {
            yield return new WaitForSeconds(waitTime);

            if (onWarpFadeOutComplete != null)
                onWarpFadeOutComplete.Invoke(sceneID);  
        }

        void SetFadeValue(float value)
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

        public void AddOnceListenerToWarpFadeInComplete(UnityAction callback)
        {
            if (callback == null) return;

            if (onWarpFadeInComplete == null)
                onWarpFadeInComplete = new UnityEvent();

            UnityAction wrapper = null;
            wrapper = () =>
            {
                this.RemoveListenerFromWarpFadeInComplete(wrapper);
                callback();
            };

            onWarpFadeInComplete.AddListener(wrapper);
        }

        public void AddOnceListenerToWarpFadeOutComplete(UnityAction callback)
        {
            if (callback == null) return;

            if (onWarpFadeOutComplete == null)
                onWarpFadeOutComplete = new UnityEvent<string>();

            UnityAction<string> wrapper = null;
            wrapper = (str) =>
            {
                this.RemoveListenerFromWarpFadeOutComplete(wrapper);
                callback();
            };

            onWarpFadeOutComplete.AddListener(wrapper);
        }

        public void RemoveListenerFromWarpFadeInComplete(UnityAction unityAction)
        {
            this.onWarpFadeInComplete.RemoveListener(unityAction);
        }

        public void RemoveListenerFromWarpFadeOutComplete(UnityAction<string> unityAction)
        {
            this.onWarpFadeOutComplete.RemoveListener(unityAction);
        }
    }
}