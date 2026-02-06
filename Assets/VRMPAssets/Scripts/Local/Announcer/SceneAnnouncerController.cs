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
        [SerializeField] private SceneAnnounceClip entranceAnnounceClip;

        [Header("Lobby Clips")]
        [SerializeField]
        private SceneAnnounceClip lobbyAnnounceClip;

        [Header("Construction Clips")]
        [SerializeField]
        private SceneAnnounceClip constructionAnnounceClip;

        [Header("Manufacturing Clips")]
        [SerializeField] private SceneAnnounceClip manufacturingAnnounceClip;

        [Header("Prevention_Basic Clips")]
        [SerializeField] private SceneAnnounceClip prevention_BasicAnnounceClip;

        [Header("Medical Clips")]
        [SerializeField] private SceneAnnounceClip medicalAnnounceClip;

        [Header("ConnectFailed Clips")]
        [SerializeField] private AudioClip[] connectFailedAnnounceClips = null;

        private Dictionary<SceneListEnum, SceneAnnounceClip> _sceneClips;

        private void Awake()
        {
            entranceAnnounceClip = new SceneAnnounceClip(SceneListEnum.Entrance.ToString());
            entranceAnnounceClip.LoadClips();

            lobbyAnnounceClip = new SceneAnnounceClip(SceneListEnum.Lobby.ToString());
            lobbyAnnounceClip.LoadClips();

            constructionAnnounceClip = new SceneAnnounceClip(SceneListEnum.Construction.ToString());
            constructionAnnounceClip.LoadClips();

            manufacturingAnnounceClip = new SceneAnnounceClip(SceneListEnum.Manufacturing.ToString());
            manufacturingAnnounceClip.LoadClips();

            prevention_BasicAnnounceClip = new SceneAnnounceClip(SceneListEnum.Prevention_Basic.ToString());
            prevention_BasicAnnounceClip.LoadClips();

            medicalAnnounceClip = new SceneAnnounceClip(SceneListEnum.Medical.ToString());
            medicalAnnounceClip.LoadClips();

            // Dictionary初期化
            _sceneClips = new Dictionary<SceneListEnum, SceneAnnounceClip>
            {
                { SceneListEnum.Entrance, entranceAnnounceClip },
                { SceneListEnum.Lobby, lobbyAnnounceClip },
                { SceneListEnum.Construction, constructionAnnounceClip },
                { SceneListEnum.Manufacturing, manufacturingAnnounceClip },
                { SceneListEnum.Prevention_Basic, prevention_BasicAnnounceClip },
                { SceneListEnum.Medical, medicalAnnounceClip }
            };

            connectFailedAnnounceClips = this.LoadAllClipFormResources(CONNECTION_FAILED);

            warpController.onWarpFadeInStart.AddListener((sceneName) =>
            {
                this.HandleOnSceneLoaded(sceneName);
            });

            warpController.onWarpFadeOutStart.AddListener((sceneName) =>
            {
                this.HandleSceneLoadStart(sceneName);
            });
            XRINetworkGameManager.Instance.networkSceneManager.onSceneLoadFailed.AddListener((sceneName) =>
                        {
                            this.HandleSceneLoadFailed(sceneName);
                        });

            XRINetworkGameManager.Instance.OnConnectionFailedAction += (message) =>
            {
                this.HandleSceneLoadFailed("Lobby");
            };
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
            XRINetworkGameManager.Instance.OnConnectionFailedAction -= (message) =>
            {
                this.HandleSceneLoadFailed("Lobby");
            };
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
            var sceneEnum = this.GetSceneEnumFromListByName(sceneName);

            if (_sceneClips.TryGetValue(sceneEnum, out var announceClip))
            {
                var clip = announceClip.GetLoadedClipRandom();
                EnqueueClip(clip, () => this.OnSceneLoaded?.Invoke());
            }
        }

        public void HandleSceneLoadStart(string sceneName)
        {
            var sceneEnum = this.GetSceneEnumFromListByName(sceneName);

            if (_sceneClips.TryGetValue(sceneEnum, out var announceClip))
            {
                var clip = announceClip.GetLoadStartClipRandom();
                EnqueueClip(clip, () => this.OnSceneLoadStart?.Invoke());
            }
        }

        public void HandleSceneLoadFailed(string sceneName)
        {
            var sceneEnum = this.GetSceneEnumFromListByName(sceneName);

            if (_sceneClips.TryGetValue(sceneEnum, out var announceClip))
            {
                var clip = announceClip.LoadFailedClipRandom();
                EnqueueClip(clip, () => this.OnSceneLoadStart?.Invoke());
            }
        }

        public void HandleConnectFailed()
        {
            if (connectFailedAnnounceClips.Length == 0) return;
            var randomIndex = Random.Range(0, connectFailedAnnounceClips.Length);
            var clip = connectFailedAnnounceClips[randomIndex];
            EnqueueClip(clip, () => this.OnSceneLoadStart?.Invoke());
        }

        public void ForceStop()
        {
            if (announcerAudioSource != null && announcerAudioSource.isPlaying)
            {
                this.announcerAudioSource.Stop();
            }
        }

        private SceneListEnum GetSceneEnumFromListByName(string sceneName)
        {
            if (!System.Enum.TryParse<SceneListEnum>(sceneName, out var sceneEnum))
            {
                Debug.LogWarning($"[SceneAnnouncerController] Unknown scene name: {sceneName}");
                return SceneListEnum.None;
            }
            return sceneEnum;
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