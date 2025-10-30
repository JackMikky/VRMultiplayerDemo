using System;
using System.Globalization;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

namespace XRMultiplayer
{
    public class NetworkChatManager : NetworkBehaviour
    {
        public Action<string, string, string> OnMessageReceived { get; internal set; }

        public NetworkList<FixedString512Bytes> ChatMessages = new NetworkList<FixedString512Bytes>();

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            ChatMessages.OnListChanged += OnChatMessagesChanged;
        }

        public override void OnNetworkDespawn()
        {
            ChatMessages.OnListChanged -= OnChatMessagesChanged;
            base.OnNetworkDespawn();
        }

        void OnChatMessagesChanged(NetworkListEvent<FixedString512Bytes> changeEvent)
        {
            if (changeEvent.Type == NetworkListEvent<FixedString512Bytes>.EventType.Add)
            {
                string json = changeEvent.Value.ToString();
                try
                {
                    LocalChatManager.ChatEntry entry = JsonUtility.FromJson<LocalChatManager.ChatEntry>(json);
                    RaiseIncomingMessage(entry.user, entry.message, entry.time);
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"Failed to parse chat entry JSON: {e.Message}");
                }
            }
        }

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

            if (IsServer)
            {
                ChatMessages.Add(json);
            }
            else if (IsClient)
            {
                SendMessageServerRpc(json);
            }
        }

        [ServerRpc(RequireOwnership = false)]
        void SendMessageServerRpc(string json, ServerRpcParams rpcParams = default)
        {
            ChatMessages.Add(json);
        }

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