using UnityEngine;
using UnityEngine.Events;

namespace XRMultiplayer
{
    internal enum SceneAnnouncerType
    {
        OnSceneLoadStart,
        OnSceneLoaded
    }

    public class SceneAnnouncerController : MonoBehaviour
    {
        private NetworkSceneManager _networkSceneManager;

        [SerializeField] private AudioSource announcerAudioSource;

        private AudioClip _onSceneLoadedClip;

        private AudioClip _onSceneLoadStartClip;

        private const string ANNOUNCER_CLIP_FOLDER = "Announcers";

        private const string START_SUFFIX = "Start";

        private const string LOADED_SUFFIX = "Loaded";

        private CustomEvent OnSceneLoaded;

        private CustomEvent OnSceneLoadStart;

        [SerializeField] private WarpController warpController;

        private void Awake()
        {
            _onSceneLoadStartClip = this.LoadAnnouncerClipFormResource("Lobby", SceneAnnouncerType.OnSceneLoadStart);

            _onSceneLoadedClip = this.LoadAnnouncerClipFormResource("Lobby", SceneAnnouncerType.OnSceneLoaded);
        }

        private void Start()
        {
            //todo:when network connected invoke HandleSceneLoadStart
            //warpController.AddOnceListenerToWarpFadeOutComplete(() =>
            //{
            //    this.HandleSceneLoadStart();
            //});
            //LocalManager.Instance.onLobbyLoadStart.AddListener(() =>
            //{
            //    this.HandleSceneLoadStart();
            //});
            warpController.onWarpFadeInStart.AddOnceListener(() =>
            {
                this.HandleSceneLoadStart();
            });

            warpController.onWarpFadeInComplete.AddOnceListener(() =>
            {
                this.HandleOnSceneLoaded();
            });

            _networkSceneManager = XRINetworkGameManager.Instance.networkSceneManager;
            if (_networkSceneManager != null)
            {
                _networkSceneManager.onSceneLoadStart.AddListener((sceneName) =>
                {
                    _onSceneLoadStartClip = this.LoadAnnouncerClipFormResource(sceneName, SceneAnnouncerType.OnSceneLoadStart);

                    _onSceneLoadedClip = this.LoadAnnouncerClipFormResource(sceneName, SceneAnnouncerType.OnSceneLoaded);

                    this.HandleSceneLoadStart();
                    Debug.Log($"[SceneAnnouncerController] onSceneLoadStart event received for scene: {sceneName}");
                });

                _networkSceneManager.onSceneLoaded.AddListener((sceneName) =>
                {
                    this.HandleOnSceneLoaded();

                    Debug.Log($"[SceneAnnouncerController] Playing announcer clip for scene: {sceneName}");
                });
            }
        }

        private void OnDestroy()
        {
            if (_networkSceneManager != null)
            {
                //todo:release reference
            }
        }

        private void HandleOnSceneLoaded()
        {
            this.announcerAudioSource.clip = _onSceneLoadedClip;
            this.announcerAudioSource.Play();
            this.OnSceneLoaded?.Invoke();
        }

        private void HandleSceneLoadStart()
        {
            this.announcerAudioSource.clip = _onSceneLoadStartClip;
            this.announcerAudioSource.Play();
            this.OnSceneLoadStart?.Invoke();
        }

        private AudioClip LoadAnnouncerClipFormResource(string sceneName, SceneAnnouncerType announcerType)
        {
            string suffixPath = "";
            switch (announcerType)
            {
                case SceneAnnouncerType.OnSceneLoadStart:
                    suffixPath = START_SUFFIX;
                    break;

                case SceneAnnouncerType.OnSceneLoaded:
                    suffixPath = LOADED_SUFFIX;
                    break;

                default:
                    break;
            }
            var loadPath = $"{ANNOUNCER_CLIP_FOLDER}/{sceneName}/{suffixPath}/{sceneName + suffixPath}";
            var audioClip = Resources.Load<AudioClip>(loadPath);
            if (audioClip != null)
            {
                return audioClip;
            }
            else
            {
                Debug.LogWarning($"[SceneAnnouncerController] No announcer clip found for scene: {sceneName}");
                return null;
            }
        }
    }
}