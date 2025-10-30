using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

namespace XRMultiplayer
{
    public class NetworkSceneManager : NetworkBehaviour
    {
        [System.Serializable]
        public struct SceneRoomInfo
        {
            [Tooltip("scene ID in Scene build list")]
            public string sceneID;

            [Tooltip("scene name in Scene build list")]
            public string sceneName;
        }

        [SerializeField] List<SceneRoomInfo> sceneList = new List<SceneRoomInfo>();

        [SerializeField] LoadSceneMode loadSceneMode = LoadSceneMode.Additive;

        public UnityEvent<string> onSceneLoadDone;

        public UnityEvent<string> onSceneLoadStart;

        public UnityEvent<string> onSceneLoadFailed;

        private readonly HashSet<ulong> m_ClientsFinishedLoading = new HashSet<ulong>();

        private string m_ExpectedSceneName;

        [SerializeField] WarpController warpController;

        private void Start()
        {
            warpController.onWarpFadeOutComplete.AddListener(this.LoadSceneByID);
        }

        [Obsolete]
        public void LoadSceneByIDWithWarpFadeOut(string sceneID)
        {
            warpController.StartFadeOutBySceneID(sceneID);
        }

        public void LoadSceneByID(string sceneID)
        {
            SceneRoomInfo? targetScene = null;
            foreach (var scene in sceneList)
            {
                if (scene.sceneID == sceneID)
                {
                    targetScene = scene;
                    break;
                }
            }

            if (targetScene.HasValue)
            {
                if (NetworkManager.Singleton.IsServer)
                {
                    Debug.Log($"[NetworkSceneManager] Loading scene: {targetScene.Value.sceneName}");
                    m_ExpectedSceneName = targetScene.Value.sceneName;
                    m_ClientsFinishedLoading.Clear();

                    NetworkManager.Singleton.SceneManager.LoadScene(targetScene.Value.sceneName,
                        this.loadSceneMode);
                    onSceneLoadStart.Invoke(targetScene.Value.sceneName);
                }
                else
                {
                    Debug.LogWarning("Only the server can initiate scene loading.");
                }
            }
            else
            {
                Debug.LogError($"Scene with ID {sceneID} not found.");
            }
        }


        public void LoadSceneByName(string sceneName)
        {
            SceneRoomInfo? targetScene = null;
            foreach (var scene in sceneList)
            {
                if (scene.sceneName == sceneName)
                {
                    targetScene = scene;
                    break;
                }
            }

            if (targetScene.HasValue)
            {
                if (NetworkManager.Singleton.IsServer)
                {
                    Debug.Log($"[NetworkSceneManager] Loading scene: {targetScene.Value.sceneName}");
                    m_ExpectedSceneName = targetScene.Value.sceneName;
                    m_ClientsFinishedLoading.Clear();

                    NetworkManager.Singleton.SceneManager.LoadScene(targetScene.Value.sceneName,
                        this.loadSceneMode);
                    onSceneLoadStart.Invoke(sceneName);
                }
                else
                {
                    Debug.LogWarning("Only the server can initiate scene loading.");
                }
            }
            else
            {
                Debug.LogError($"Scene with name {sceneName} not found.");
            }
        }

        void OnEnable()
        {
            SceneManager.sceneLoaded += HandleSceneLoaded;
        }

        void OnDisable()
        {
            SceneManager.sceneLoaded -= HandleSceneLoaded;
        }

        private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            Debug.Log($"[NetworkSceneManager] Local scene loaded: {scene.name}");

            if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsClient)
            {
                ClientSceneLoadedServerRpc(scene.name);
            }

            if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer &&
                NetworkManager.Singleton.IsHost)
            {
                var localId = NetworkManager.Singleton.LocalClientId;
                if (!string.IsNullOrEmpty(m_ExpectedSceneName) && scene.name == m_ExpectedSceneName)
                {
                    if (m_ClientsFinishedLoading.Add(localId))
                    {
                        TryFinishOnServer(scene.name);
                    }
                }
            }
        }

        [ServerRpc(RequireOwnership = false)]
        private void ClientSceneLoadedServerRpc(string sceneName, ServerRpcParams rpcParams = default)
        {
            var clientId = rpcParams.Receive.SenderClientId;
            Debug.Log($"[NetworkSceneManager] Server received scene-loaded from {clientId}: {sceneName}");

            if (string.IsNullOrEmpty(m_ExpectedSceneName) || sceneName != m_ExpectedSceneName) return;

            if (m_ClientsFinishedLoading.Add(clientId))
            {
                TryFinishOnServer(sceneName);
            }
        }

        private void TryFinishOnServer(string sceneName)
        {
            if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsServer) return;

            int connectedCount = NetworkManager.Singleton.ConnectedClientsList.Count;
            Debug.Log($"[NetworkSceneManager] Loaded count: {m_ClientsFinishedLoading.Count} / {connectedCount}");

            if (m_ClientsFinishedLoading.Count >= connectedCount)
            {
                Debug.Log($"[NetworkSceneManager] All clients finished loading {m_ExpectedSceneName}");
                onSceneLoadDone?.Invoke(sceneName);

                m_ExpectedSceneName = null;
                m_ClientsFinishedLoading.Clear();
            }
        }
    }
}