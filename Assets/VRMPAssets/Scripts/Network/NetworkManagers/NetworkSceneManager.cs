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

        [SerializeField] private List<SceneRoomInfo> sceneList = new List<SceneRoomInfo>();

        [SerializeField] private LoadSceneMode loadSceneMode = LoadSceneMode.Additive;

        public UnityEvent<string> onSceneLoaded;

        public UnityEvent<string> onSceneLoadStart;

        public UnityEvent<string> onSceneLoadFailed;

        private readonly HashSet<ulong> m_ClientsFinishedLoading = new HashSet<ulong>();

        private string m_ExpectedSceneName;

        [SerializeField] private WarpController warpController;

        public WarpController WarpController => warpController;

        public string currentSceneName = "";

        private void Start()
        {
            currentSceneName = SceneManager.GetActiveScene().name;
        }

        public void LoadSceneByNameWithWarpFadeOut(string name)
        {
            // Host/server should instruct all clients (and itself) to perform the warp fade-out.
            // Clients will notify the server when their fade-out completes, server will initiate the actual network scene load.
            if (NetworkManager.Singleton == null)
            {
                // Fallback: no networking available, behave locally.
                warpController?.StartFadeOut(name, (sceneName) => { LoadSceneByName(sceneName); });
                return;
            }

            if (NetworkManager.Singleton.IsServer)
            {
                // Tell all clients (including host) to start fade out.
                StartFadeOutOnClientsClientRpc(name);
                // The host (server) will also run the ClientRpc callback locally and will call LoadSceneByName directly when its fade completes.
            }
            else
            {
                // Not the server: request server to start the fade/load sequence.
                RequestLoadSceneServerRpc(name);
            }
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
                this.onSceneLoadFailed?.Invoke(targetScene.Value.sceneName);
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
                this.onSceneLoadFailed?.Invoke(sceneName);
                Debug.LogError($"Scene with name {sceneName} not found.");
            }
        }

        private void OnEnable()
        {
            SceneManager.sceneLoaded += HandleSceneLoaded;
        }

        private void OnDisable()
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
                SceneManager.UnloadSceneAsync(currentSceneName);
                Debug.Log($"[NetworkSceneManager] All clients finished loading {sceneName}");
                onSceneLoaded?.Invoke(sceneName);
                currentSceneName = sceneName;

                m_ExpectedSceneName = null;
                m_ClientsFinishedLoading.Clear();
            }
        }

        private void OnDestroy()
        {
            this.onSceneLoadStart.RemoveAllListeners();
            this.onSceneLoaded.RemoveAllListeners();
            this.onSceneLoadFailed.RemoveAllListeners();
        }

        // ClientRpc: invoked on all clients (and on host as client) to start the fade out.
        [ClientRpc]
        private void StartFadeOutOnClientsClientRpc(string sceneName)
        {
            warpController?.StartFadeOut(sceneName, (sn) =>
            {
                // Host/server will be both client & server: if this instance is server, perform the actual load directly.
                if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer)
                {
                    LoadSceneByName(sn);
                }
                else
                {
                    // Clients notify the server that their fade-out completed and request the server to load the scene.
                    RequestLoadSceneServerRpc(sn);
                }
            });
        }

        // ServerRpc: clients call this to request the server to start the network scene load.
        [ServerRpc(RequireOwnership = false)]
        private void RequestLoadSceneServerRpc(string sceneName, ServerRpcParams rpcParams = default)
        {
            // Only the server should proceed. Guard against duplicate requests.
            if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsServer) return;

            if (!string.IsNullOrEmpty(m_ExpectedSceneName))
            {
                // A load is already expected/started, ignore duplicate requests.
                Debug.Log($"[NetworkSceneManager] Ignoring duplicate load request for {sceneName}");
                return;
            }

            Debug.Log($"[NetworkSceneManager] Received load request from client. Server will load: {sceneName}");
            LoadSceneByName(sceneName);
        }
    }
}