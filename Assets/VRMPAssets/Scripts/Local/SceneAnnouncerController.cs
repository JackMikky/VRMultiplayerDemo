using UnityEngine;

namespace XRMultiplayer
{
    public class SceneAnnouncerController : MonoBehaviour
    {
        NetworkSceneManager _networkSceneManager;

        [SerializeField] AudioSource announcerAudioSource;

        AudioClip _currentAnnouncerClip;

        private void Start()
        {
            _networkSceneManager = XRINetworkGameManager.Instance.networkSceneManager;
            if (_networkSceneManager != null)
            {
                _networkSceneManager.onSceneLoadStart.AddListener((sceneName) =>
                {
                    this.LoadAnnouncerClipFormResource(sceneName);
                    Debug.Log($"[SceneAnnouncerController] onSceneLoadStart event received for scene: {sceneName}");
                });

                _networkSceneManager.onSceneLoadDone.AddListener((sceneName) =>
                {
                    this.OnSceneLoaded();

                    Debug.Log($"[SceneAnnouncerController] Playing announcer clip for scene: {sceneName}");
                });
            }
        }

        void OnDestroy()
        {
            if (_networkSceneManager != null)
            {
            }
        }

        void OnSceneLoaded()
        {
            this.announcerAudioSource.clip = _currentAnnouncerClip;
            this.announcerAudioSource.Play();
        }

        void LoadAnnouncerClipFormResource(string sceneName)
        {
            _currentAnnouncerClip = Resources.Load<AudioClip>($"Announcers/{sceneName}_Announcer");
            if (_currentAnnouncerClip != null)
            {
                if (announcerAudioSource != null)
                {
                    announcerAudioSource.clip = _currentAnnouncerClip;
                }
                else
                {
                    Debug.LogWarning("[SceneAnnouncerController] No AudioSource component found on the GameObject.");
                }
            }
            else
            {
                Debug.LogWarning($"[SceneAnnouncerController] No announcer clip found for scene: {sceneName}");
            }
        }
    }
}