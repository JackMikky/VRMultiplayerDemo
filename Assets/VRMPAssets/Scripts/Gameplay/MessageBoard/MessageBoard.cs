using UnityEngine;
using System;
using UnityEngine.XR.Interaction.Toolkit.Samples.SpatialKeyboard;
using System.Globalization;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace XRMultiplayer
{
    /// <summary>
    /// Represents a message board that allows players to submit and display messages in a local (non-networked) environment.
    /// </summary>
    public class MessageBoard : MonoBehaviour
    {
        /// <summary>
        /// The prefab for the message text.
        /// </summary>
        [SerializeField] GameObject m_MessagePrefab;

        /// <summary>
        /// The transform that contains the viewport for the messages.
        /// </summary>
        [SerializeField] Transform m_ContentViewport;

        [SerializeField] LocalChatManager localChatManager;


        // 在 Start() 中订阅
        void Start()
        {
            XRINetworkGameManager.Connected.Subscribe(ConnectedToNetwork);

            if (localChatManager != null)
            {
                localChatManager.OnIncomingChatMessageHandled += SubmitMessageLocal;
            }
        }

        void ConnectedToNetwork(bool connected)
        {
            if (!connected)
            {
                for (int i = m_ContentViewport.childCount - 1; i >= 0; i--)
                {
                    Destroy(m_ContentViewport.GetChild(i).gameObject);
                }
            }
        }

        // Called from XRIKeyboardDisplay
        public void ToggleKeyboardOpen(bool toggle)
        {
            GlobalNonNativeKeyboard.instance.keyboard.closeOnSubmit = !toggle;
        }

        // 重建界面显示（从本地缓存）
        void RebuildDisplay()
        {
            if (m_ContentViewport == null) return;

            // 清空现有
            for (int i = m_ContentViewport.childCount - 1; i >= 0; i--)
            {
                Destroy(m_ContentViewport.GetChild(i).gameObject);
            }

            var messageHistory = localChatManager.messageHistory;
            int count = messageHistory.Count;
            for (int i = 0; i < count; i++)
            {
                CreateText(messageHistory[i]);
            }
        }

        public void SubmitTextLocal(string text)
        {
            if (string.IsNullOrEmpty(text) || string.IsNullOrWhiteSpace(text)) return;

            if (text.Length > localChatManager.maxMessageHistoryCount)
            {
                text = text.Substring(0, localChatManager.maxMessageHistoryCount);
            }

            // 获取本地玩家名字（保留现有接口）
            string userName = XRINetworkPlayer.LocalPlayer != null ? XRINetworkPlayer.LocalPlayer.playerName : "Player";

            var chatEntry = new LocalChatManager.ChatEntry
            {
                user = userName,
                message = text,
                time = DateTime.Now.ToString("h:mm tt", CultureInfo.InvariantCulture)
            };
            SubmitMessageLocal(chatEntry);
        }

        void SubmitMessageLocal(LocalChatManager.ChatEntry chatEntry)
        {
            if (localChatManager.messageHistory.Count > localChatManager.maxMessageHistoryCount)
            {
                // 删除最早的消息
                localChatManager.messageHistory.RemoveAt(0);

                // 同步移除显示区第一个子项（如果存在）
                if (m_ContentViewport.childCount > 0)
                {
                    Destroy(m_ContentViewport.GetChild(0).gameObject);
                }
            }

            CreateText(chatEntry);
        }

        void CreateText(LocalChatManager.ChatEntry chatEntry)
        {
            if (m_MessagePrefab == null || m_ContentViewport == null) return;

            Instantiate(m_MessagePrefab, m_ContentViewport).GetComponent<MessageText>()
                .SetMessage(chatEntry.user, chatEntry.message, chatEntry.time);

            if (m_ContentViewport.childCount > localChatManager.maxMessageHistoryCount)
            {
                Destroy(m_ContentViewport.GetChild(0).gameObject);
            }
        }

        // 在类中添加处理器和取消订阅（避免内存泄漏）
        void OnDestroy()
        {
            if (localChatManager != null)
            {
                localChatManager.OnIncomingChatMessageHandled -= HandleNetworkChatMessage;
            }
        }

        private void HandleNetworkChatMessage(LocalChatManager.ChatEntry chatEntry)
        {
            // 复用现有本地提交显示流程
            SubmitMessageLocal(chatEntry);
        }

        private void OnEnable()
        {
            RebuildDisplay();
        }
    }

#if UNITY_EDITOR
    [CustomEditor(typeof(MessageBoard), true), CanEditMultipleObjects]
    public class NetworkMessageBoardEditor : Editor
    {
        [SerializeField, TextArea(10, 15)] string m_DebugText;

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            GUILayout.Space(10);
            GUILayout.Label("Debug Area", EditorStyles.boldLabel);
            GUI.enabled = XRINetworkGameManager.Connected.Value;
            if (!XRINetworkGameManager.Connected.Value)
            {
                GUILayout.Label("Connect to a network to submit messages.", EditorStyles.helpBox);
            }
            else
            {
                GUILayout.Label("Debug Text");
                m_DebugText = GUILayout.TextArea(m_DebugText);
            }

            if (GUILayout.Button("Submit Text Debug"))
            {
                ((MessageBoard)target).SubmitTextLocal(m_DebugText);
            }

            GUI.enabled = true;
        }
    }
#endif
}