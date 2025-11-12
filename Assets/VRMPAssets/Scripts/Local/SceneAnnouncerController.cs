using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace XRMultiplayer
{
    internal enum SceneAnnouncerType
    {
        OnSceneLoadStart,
        OnSceneLoaded,
        OnSceneLoadFailed
    }

    public class SceneAnnouncerController : MonoBehaviour
    {
        private NetworkSceneManager _networkSceneManager;

        [SerializeField] private AudioSource announcerAudioSource;

        private AudioClip _onSceneLoadedClip;

        private AudioClip _onSceneLoadStartClip;

        private AudioClip _onSceneLoadFailedClip;

        private const string ANNOUNCER_CLIP_FOLDER = "Announcers";

        private const string START_SUFFIX = "Start";

        private const string LOADED_SUFFIX = "Loaded";

        private const string LOAD_FAILED_SUFFIX = "LoadFailed";

        private CustomEvent OnSceneLoaded;

        private CustomEvent OnSceneLoadStart;

        [SerializeField] private WarpController warpController;

        private Queue<(AudioClip clip, UnityAction onStart)> _clipQueue = new Queue<(AudioClip, UnityAction)>();

        private bool _isProcessingQueue = false;

        private void Awake()
        {
            _onSceneLoadStartClip = this.LoadSceneAnnouncerClip("Lobby", SceneAnnouncerType.OnSceneLoadStart);

            _onSceneLoadedClip = this.LoadSceneAnnouncerClip("Lobby", SceneAnnouncerType.OnSceneLoaded);

            var failedClipPath = $"{LOAD_FAILED_SUFFIX}/{LOAD_FAILED_SUFFIX}";
            _onSceneLoadFailedClip = this.LoadAnnouncerClipFormResources(failedClipPath);
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
            //warpController.onWarpFadeOutStart.AddOnceListener((sceneName) =>
            //{
            //    this.HandleSceneLoadStart();
            //});

            warpController.onWarpFadeInStart.AddOnceListener((sceneName) =>
            {
                this.HandleSceneLoadStart();
            });

            _networkSceneManager = XRINetworkGameManager.Instance.networkSceneManager;
            if (_networkSceneManager != null)
            {
                warpController.onWarpFadeOutStart.AddListener((sceneName) =>
                {
                    _onSceneLoadStartClip = this.LoadSceneAnnouncerClip(sceneName, SceneAnnouncerType.OnSceneLoadStart);

                    _onSceneLoadedClip = this.LoadSceneAnnouncerClip(sceneName, SceneAnnouncerType.OnSceneLoaded);

                    this.HandleSceneLoadStart();
                    Debug.Log($"[SceneAnnouncerController] onSceneLoadStart event received for scene: {sceneName}");
                });

                warpController.onWarpFadeInComplete.AddListener((sceneName) =>
                {
                    this.HandleOnSceneLoaded();

                    Debug.Log($"[SceneAnnouncerController] Playing announcer clip for scene: {sceneName}");
                });
            }

            XRINetworkGameManager.Instance.networkSceneManager.onSceneLoadFailed.AddListener((sceneName) =>
            {
            });
        }

        private void OnDestroy()
        {
            if (_networkSceneManager != null)
            {
                //todo:release reference
            }
        }

        private void EnqueueClip(AudioClip clip, UnityAction onStart = null)
        {
            if (clip == null || announcerAudioSource == null) return;

            _clipQueue.Enqueue((clip, onStart));

            if (!_isProcessingQueue && !announcerAudioSource.isPlaying)
            {
                StartCoroutine(ProcessClipQueue());
            }
        }

        private IEnumerator ProcessClipQueue()
        {
            _isProcessingQueue = true;
            while (_clipQueue.Count > 0)
            {
                var item = _clipQueue.Dequeue();
                announcerAudioSource.clip = item.clip;
                announcerAudioSource.Play();
                item.onStart?.Invoke();

                yield return new WaitWhile(() => announcerAudioSource != null && announcerAudioSource.isPlaying);
            }
            _isProcessingQueue = false;
        }

        private void HandleOnSceneLoaded()
        {
            EnqueueClip(_onSceneLoadedClip, () => this.OnSceneLoaded?.Invoke());
        }

        private void HandleSceneLoadStart()
        {
            EnqueueClip(_onSceneLoadStartClip, () => this.OnSceneLoadStart?.Invoke());
        }

        private AudioClip LoadSceneAnnouncerClip(string sceneName, SceneAnnouncerType announcerType)
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

                case SceneAnnouncerType.OnSceneLoadFailed:
                default:
                    break;
            }

            try
            {
                var relativePath = $"{sceneName}/{suffixPath}/{sceneName}_{suffixPath}";
                return LoadAnnouncerClipFormResources(relativePath);
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[SceneAnnouncerController] Error loading announcer clip for scene: {sceneName}, Exception: {ex}");
                return null;
            }
        }

        private AudioClip LoadAnnouncerClipFormResources(string relativePath)
        {
            var loadPath = $"{ANNOUNCER_CLIP_FOLDER}/{relativePath}";
            var audioClip = Resources.Load<AudioClip>(loadPath);
            if (audioClip != null)
            {
                return audioClip;
            }
            else
            {
                Debug.LogWarning($"[SceneAnnouncerController] Announcer clip not found at path: {loadPath}");
                return null;
            }
        }
    }
}