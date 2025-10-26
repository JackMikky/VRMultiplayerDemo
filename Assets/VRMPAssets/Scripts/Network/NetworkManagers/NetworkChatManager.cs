using System;
using System.Globalization;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

namespace XRMultiplayer
{
    public class NetworkChatManager : NetworkBehaviour
    {
        // 保留现有外部 API（其他代码引用此回调）
        public Action<string, string, string> OnMessageReceived { get; internal set; }

        // 使用 NetworkList 保存消息（以 JSON 字符串形式）
        public NetworkList<FixedString512Bytes> ChatMessages = new NetworkList<FixedString512Bytes>();


        // 在网络对象激活时订阅列表变化
        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            ChatMessages.OnListChanged += OnChatMessagesChanged;
        }

        // 在网络对象销毁/取消激活时取消订阅
        public override void OnNetworkDespawn()
        {
            ChatMessages.OnListChanged -= OnChatMessagesChanged;
            base.OnNetworkDespawn();
        }

        // 当列表发生变化时回调
        void OnChatMessagesChanged(NetworkListEvent<FixedString512Bytes> changeEvent)
        {
            // 仅在新增时触发回调（也可以处理 Insert、Set 等）
            if (changeEvent.Type == NetworkListEvent<FixedString512Bytes>.EventType.Add)
            {
                string json = changeEvent.Value.ToString();
                try
                {
                    LocalChatManager.ChatEntry entry = JsonUtility.FromJson<LocalChatManager.ChatEntry>(json);
                    // 调用现有的内部触发方法，保持 API 一致
                    RaiseIncomingMessage(entry.user, entry.message, entry.time);
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"Failed to parse chat entry JSON: {e.Message}");
                }
            }
        }

        // 客户端调用此方法向服务器发送消息（会触发 ServerRpc）
        public void SubmitChatMessage(string userName, string message)
        {
            if (string.IsNullOrEmpty(message)) return;

            LocalChatManager.ChatEntry entry = new LocalChatManager.ChatEntry
            {
                user = userName,
                message = message,
                time = DateTime.Now.ToString("h:mm tt", CultureInfo.InvariantCulture)
            };

            string json = JsonUtility.ToJson(entry);

            // 如果是 Host（即同时是服务器），可直接添加；否则通过 ServerRpc 请求添加
            if (IsServer)
            {
                ChatMessages.Add(json);
            }
            else if (IsClient)
            {
                SendMessageServerRpc(json);
            }
        }

        // 允许客户端请求服务器将消息写入 NetworkList
        [ServerRpc(RequireOwnership = false)]
        void SendMessageServerRpc(string json, ServerRpcParams rpcParams = default)
        {
            // 可在这里根据需要检查/附加发送者信息，例如 rpcParams.Receive.SenderClientId
            ChatMessages.Add(json);
        }

        // 保持原有接口用于内部调用
        void RaiseIncomingMessage(string userName, string text, string time)
        {
            OnMessageReceived?.Invoke(userName, text, time);
        }

        public void SubmitMessage(string text)
        {
            SubmitChatMessage(XRINetworkGameManager.LocalPlayerName.Value, text);
        }
    }
}