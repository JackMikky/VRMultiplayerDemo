using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace XRMultiplayer
{
    public class SceneAnnouncerController : AudioClipLoader
    {
        private NetworkSceneManager _networkSceneManager;

        [SerializeField] private AudioSource announcerAudioSource;

        private CustomEvent OnSceneLoaded;

        private CustomEvent OnSceneLoadStart;

        [SerializeField] private WarpController warpController;

        private Queue<(AudioClip clip, UnityAction onStart)> _clipQueue = new Queue<(AudioClip, UnityAction)>();

        private bool _isProcessingQueue = false;

        [Header("Entrance Clips")]
        [SerializeField] private SceneAnnounceClip entranceAnnounceClip = new SceneAnnounceClip();

        [Header("Lobby Clips")]
        [SerializeField]
        private SceneAnnounceClip lobbyAnnounceClip = new SceneAnnounceClip();

        [Header("Room1 Clips")]
        [SerializeField]
        private SceneAnnounceClip room1AnnounceClip = new SceneAnnounceClip();

        [Header("Room2 Clips")]
        [SerializeField] private SceneAnnounceClip room2AnnounceClip = new SceneAnnounceClip();

        [Header("LoadFailed Clips")]
        [SerializeField] private AudioClip[] loadFailedAnnounceClips = null;

        private void Awake()
        {
            entranceAnnounceClip.LoadClips();

            lobbyAnnounceClip.LoadClips();

            room1AnnounceClip.LoadClips();

            room2AnnounceClip.LoadClips();

            if (loadFailedAnnounceClips == null)
            {
                loadFailedAnnounceClips = this.LoadAllClipFormResources(LOAD_FAILED_SUFFIX);
            }

            warpController.onWarpFadeInStart.AddListener((sceneName) =>
            {
                this.HandleOnSceneLoaded(sceneName);
            });

            warpController.onWarpFadeOutStart.AddListener((sceneName) =>
            {
                this.HandleSceneLoadStart(sceneName);
            });
        }

        private void OnDestroy()
        {
            if (_networkSceneManager != null)
            {
                _clipQueue.Clear();
                OnSceneLoaded.RemoveAllListeners();
                OnSceneLoadStart.RemoveAllListeners();
                _networkSceneManager = null;
                warpController = null;
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

        public void HandleOnSceneLoaded(string sceneName)
        {
            var clip = null as AudioClip;
            switch (sceneName)
            {
                case "Entrance":
                    clip = entranceAnnounceClip.GetLoadedClipRandom();
                    break;

                case "Lobby":
                    clip = lobbyAnnounceClip.GetLoadedClipRandom();
                    break;

                case "Room1":
                    clip = room1AnnounceClip.GetLoadedClipRandom();
                    break;

                case "Room2":
                    clip = room2AnnounceClip.GetLoadedClipRandom();
                    break;

                default:
                    break;
            }

            EnqueueClip(clip, () => this.OnSceneLoaded?.Invoke());
        }

        public void HandleSceneLoadStart(string sceneName)
        {
            var clip = null as AudioClip;
            switch (sceneName)
            {
                case "Entrance":
                    clip = entranceAnnounceClip.GetLoadStartClipRandom();
                    break;

                case "Lobby":
                    clip = lobbyAnnounceClip.GetLoadStartClipRandom();
                    break;

                case "Room1":
                    clip = room1AnnounceClip.GetLoadStartClipRandom();
                    break;

                case "Room2":
                    clip = room2AnnounceClip.GetLoadStartClipRandom();
                    break;

                default:
                    break;
            }

            EnqueueClip(clip, () => this.OnSceneLoadStart?.Invoke());
        }

        protected override AudioClip[] LoadAllClipFormResources(string relativePath)
        {
            var loadPath = $"{ANNOUNCER_CLIP_FOLDER}/{relativePath}";
            var audioClip = Resources.LoadAll<AudioClip>(loadPath);
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