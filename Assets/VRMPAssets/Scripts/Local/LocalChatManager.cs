using System;
using System.Collections.Generic;
using UnityEngine;

namespace XRMultiplayer
{
    public class LocalChatManager : MonoBehaviour
    {
        public Action<ChatEntry> OnIncomingChatMessageHandled;

        public struct ChatEntry
        {
            public string user;
            public string message;
            public string time;
        }

        private NetworkChatManager _networkChatManager;

        private List<ChatEntry> _messageHistory;

        public List<ChatEntry> messageHistory
        {
            get { return _messageHistory; }
            set { _messageHistory = value; }
        }

        /// <summary>
        /// The maximum number of messages that can be displayed.
        /// </summary>
        [SerializeField] private int _maxMessageHistoryCount = 100;

        public int maxMessageHistoryCount
        {
            get { return _maxMessageHistoryCount; }
        }

        /// <summary>
        /// The maximum number of characters that can be displayed in a message.
        /// </summary>
        [SerializeField] private int m_MaxCharacterCount = 256;

        private void Awake()
        {
            _messageHistory = new List<ChatEntry>();
        }

        private void Start()
        {
            XRINetworkGameManager.Connected.Subscribe(ConnectedToNetwork);
            _networkChatManager = GameObject.FindGameObjectWithTag("NetworkChatManager")
                ?.GetComponent<NetworkChatManager>();

            if (_networkChatManager != null)
            {
                _networkChatManager.OnMessageReceived += HandleIncomingChatMessage;
            }
        }

        private void ConnectedToNetwork(bool connected)
        {
            if (connected && _networkChatManager != null)
            {
                //TODO: Subscribe to network chat events
            }
        }

        private void HandleIncomingChatMessage(string userName, string message, string time)
        {
            var messageList = new ChatEntry
            {
                user = userName,
                message = message,
                time = time
            };
            StoreMessage(messageList);
            OnIncomingChatMessageHandled?.Invoke(messageList);
        }

        private void StoreMessage(ChatEntry chatEntry)
        {
            // Store message locally
            _messageHistory.Add(chatEntry);

            if (_messageHistory.Count > _maxMessageHistoryCount)
            {
                _messageHistory.RemoveAt(0);
            }
        }
    }
}