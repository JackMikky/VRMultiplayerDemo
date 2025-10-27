using System.Collections.Generic;
using NUnit.Framework;
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

        // 用于追踪哪些客户端已完成加载
        private readonly HashSet<ulong> m_ClientsFinishedLoading = new HashSet<ulong>();

        // 当前期望的场景名（由服务器在发起加载时设置）
        private string m_ExpectedSceneName;

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
                    // 在发起网络场景加载前设置期望场景并清空已完成列表
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
                    // 在发起网络场景加载前设置期望场景并清空已完成列表
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

            // 如果是客户端（包含 host 的客户端部分），主动把本地加载完成通知服务器
            if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsClient)
            {
                // 将场景名发送给服务器以便服务器只在期待的场景完成时计数
                ClientSceneLoadedServerRpc(scene.name);
            }

            // 如果当前是纯服务器（不走 RPC 的情况，或 host 时想直接标记自己）
            if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer &&
                NetworkManager.Singleton.IsHost)
            {
                // Host 会同时是服务器和客户端：直接在服务器端也标记一次（ServerRpc 来自 host 也会触发，但此处冗余安全处理）
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

            // 只在与服务器期望场景名相同的时候计数
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

            // 如果所有已连接客户端都报告完成，则触发事件
            if (m_ClientsFinishedLoading.Count >= connectedCount)
            {
                Debug.Log($"[NetworkSceneManager] All clients finished loading {m_ExpectedSceneName}");
                onSceneLoadDone?.Invoke(sceneName);

                // 清理状态
                m_ExpectedSceneName = null;
                m_ClientsFinishedLoading.Clear();
            }
        }
    }
}