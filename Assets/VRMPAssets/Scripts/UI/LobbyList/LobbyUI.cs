using System.Collections;
using System.Net;
using TMPro;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Multiplayer;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace XRMultiplayer
{
    public class LobbyUI : MonoBehaviour
    {
        /// <summary>
        ///  The timeout for direct join in seconds. This is the time we wait for a direct join to complete before giving up.
        ///  This is used to prevent the client from waiting indefinitely if the server is not responding.
        /// </summary>
        private const float k_DirectJoinTimeout = 4.5f;

        private enum ConnectionSubPanel
        {
            LobbyPanel = 0,
            CreationPanel = 1,
            ConnectionPanel = 2,
            ConnectionSuccessPanel = 3,
            ConnectionFailurePanel = 4,
            NoConnectionPanel = 5
        }

        [Header("Lobby List")]
        [SerializeField]
        private Transform m_LobbyListParent;

        [SerializeField] private GameObject m_LobbyListPrefab;

        [SerializeField] private GameObject m_SessionPanelObject;

        [SerializeField] private GameObject m_LocalPanelObject;

        [SerializeField] private Button m_RefreshButton;

        [SerializeField] private Image m_CooldownImage;

        [SerializeField] private float m_AutoRefreshTime = 5.0f;

        [SerializeField] private float m_RefreshCooldownTime = .5f;

        [Header("Connection Texts")]
        private string[] m_InputIPAdress = { "192", "168", "1", "0" };

        [SerializeField] private TMP_InputField[] m_IPInputFields;

        [SerializeField]
        private TMP_Text m_ConnectionUpdatedText;

        [SerializeField] private TMP_Text m_ConnectionSuccessText;

        [SerializeField] private TMP_Text m_ConnectionFailedText;

        [SerializeField] private TMP_Dropdown[] m_Dropdowns;

        private bool isChangingFromInput = false;
        private bool isChangingFromDropdown = false;

        [Header("Room Creation")]
        [SerializeField]
        private TMP_InputField m_RoomNameText;

        [SerializeField] private Toggle m_PrivacyToggle;

        [SerializeField] private GameObject[] m_ConnectionSubPanels;

        [Header("Connection Event")]
        [SerializeField] private CustomEvent OnConnectionSuccessful;

        [SerializeField] private CustomEvent OnConnectionFailed;

        private VoiceChatManager m_VoiceChatManager;

        private Coroutine m_UpdateLobbiesRoutine;
        private Coroutine m_CooldownFillRoutine;

        private bool m_Private = false;
        private int m_PlayerCount;

        private void Awake()
        {
            m_VoiceChatManager = FindFirstObjectByType<VoiceChatManager>();
            SessionManager.status.Subscribe(ConnectedUpdated);
            m_CooldownImage.enabled = false;
        }

        private void Start()
        {
            m_PrivacyToggle.onValueChanged.AddListener(TogglePrivacy);

            bool isLocal = XRINetworkGameManager.CurrentSessionType != SessionType.LocalOnly;
            //m_SessionPanelObject.SetActive(!isLocal);
            m_LocalPanelObject.SetActive(true);
            m_PlayerCount = XRINetworkGameManager.maxPlayers / 2;
            XRINetworkGameManager.Instance.OnConnectionFailedAction += FailedToConnect;
            XRINetworkGameManager.Instance.OnConnectionUpdated += ConnectedUpdated;

            this.OnConnectionFailed.AddListener(() =>
            {
                var andouncer = LocalManager.Instance._SceneAnnouncerController;
                andouncer.ForceStop();
                andouncer.HandleConnectFailed();
            });

            foreach (Transform t in m_LobbyListParent)
            {
                Destroy(t.gameObject);
            }

            InitializeIPFields();

            for (int i = 0; i < m_Dropdowns.Length; i++)
            {
                int index = i;
                if (m_Dropdowns[i] != null)
                {
                    m_Dropdowns[i].onValueChanged.RemoveAllListeners();
                    m_Dropdowns[i].onValueChanged.AddListener((valueIndex) =>
                    {
                        OnDropdownValueChanged(index, valueIndex);
                    });
                }
            }
        }

        private void OnEnable()
        {
            ToggleConnectionSubPanel(ConnectionSubPanel.LobbyPanel);
        }

        private void OnDisable()
        {
            HideLobbies();
        }

        private void OnDestroy()
        {
            XRINetworkGameManager.Instance.OnConnectionFailedAction -= FailedToConnect;
            XRINetworkGameManager.Instance.OnConnectionUpdated -= ConnectedUpdated;

            this.OnConnectionSuccessful.RemoveAllListeners();
            this.OnConnectionFailed.RemoveAllListeners();

            SessionManager.status.Unsubscribe(ConnectedUpdated);
        }

        private void InitializeIPFields()
        {
            isChangingFromDropdown = true;

            for (int i = 0; i < 4; i++)
            {
                if (m_Dropdowns[i] != null && m_IPInputFields[i] != null)
                {
                    string defaultText = m_Dropdowns[i].options[m_Dropdowns[i].value].text;

                    m_InputIPAdress[i] = defaultText;
                    m_IPInputFields[i].text = defaultText;
                    int index = i;
                    m_IPInputFields[i].onEndEdit.AddListener((value) =>
                    {
                        OnIPInputEndEdit(index, value);
                    });
                }
            }

            isChangingFromDropdown = false;
            Debug.Log($"[Initialized] default IP: {GetFullIPAddress()}");
            this.UpdateIP();
        }

        public void CreateLobby()
        {
            XRINetworkGameManager.Connected.Subscribe(OnConnected);
            if (string.IsNullOrEmpty(m_RoomNameText.text) || m_RoomNameText.text == "<Room Name>")
            {
                m_RoomNameText.text = $"{XRINetworkGameManager.LocalPlayerName.Value}'s Room";
            }

            XRINetworkGameManager.Instance.CreateNewLobby(m_RoomNameText.text, m_Private, m_PlayerCount);
            m_ConnectionSuccessText.text = $"Joining {m_RoomNameText.text}";
        }

        public void CancelConnection()
        {
            XRINetworkGameManager.Instance.CancelMatchmaking();
        }

        /// <summary>
        /// Set the room name
        /// </summary>
        /// <param name="roomName">The name of the room</param>
        /// <remarks> This function is called from <see cref="XRIKeyboardDisplay"/>
        public void SetRoomName(string roomName)
        {
            if (!string.IsNullOrEmpty(roomName))
            {
                m_RoomNameText.text = roomName;
            }
        }

        /// <summary>
        /// Join a room by code
        /// </summary>
        /// <param name="roomCode">The room code to join</param>
        /// <remarks> This function is called from <see cref="XRIKeyboardDisplay"/>
        public void EnterRoomCode(string roomCode)
        {
            ToggleConnectionSubPanel(ConnectionSubPanel.ConnectionPanel);
            XRINetworkGameManager.Connected.Subscribe(OnConnected);
            XRINetworkGameManager.Instance.JoinLobbyByCode(roomCode.ToUpper());
            m_ConnectionSuccessText.text = $"Joining Room: {roomCode.ToUpper()}";
        }

        public void JoinLobby(ISessionInfo Session)
        {
            ToggleConnectionSubPanel(ConnectionSubPanel.ConnectionPanel);
            XRINetworkGameManager.Connected.Subscribe(OnConnected);
            XRINetworkGameManager.Instance.JoinLobbySpecific(Session);
            m_ConnectionSuccessText.text = $"Joining {Session.Name}";
        }

        public void SetVoiceChatConversationalDistance(int conversationalDistance)
        {
            m_VoiceChatManager.ConversationalDistance = conversationalDistance;
        }

        public void SetVoiceChatAudioFadeIntensity(float fadeIntensity)
        {
            m_VoiceChatManager.AudioFadeIntensity = fadeIntensity;
        }

        public void SetVoiceChatAudioFadeModel(int fadeModel)
        {
        }

        public void TogglePrivacy(bool toggle)
        {
            m_Private = toggle;
        }

        private void ToggleConnectionSubPanel(ConnectionSubPanel panel)
        {
            ToggleConnectionSubPanel((int)panel);
        }

        public void ToggleConnectionSubPanel(int panelId)
        {
            for (int i = 0; i < m_ConnectionSubPanels.Length; i++)
            {
                m_ConnectionSubPanels[i].SetActive(i == panelId);
            }

            if (panelId == 0)
            {
                ShowLobbies();
            }
            else
            {
                HideLobbies();
            }
        }

        private void OnConnected(bool connected)
        {
            if (connected)
            {
                ToggleConnectionSubPanel(ConnectionSubPanel.ConnectionSuccessPanel);
                XRINetworkGameManager.Connected.Unsubscribe(OnConnected);
            }
        }

        private void ConnectedUpdated(string update)
        {
            m_ConnectionUpdatedText.text = $"<b>Status:</b> {update}";
        }

        public void FailedToConnect(string reason)
        {
            ToggleConnectionSubPanel(ConnectionSubPanel.ConnectionFailurePanel);
            m_ConnectionFailedText.text = $"<b>Error:</b> {reason}";
        }

        public void HideLobbies()
        {
            EnableRefresh();
            if (m_UpdateLobbiesRoutine != null) StopCoroutine(m_UpdateLobbiesRoutine);
        }

        public void ShowLobbies()
        {
            UpdateLobbyDisplay();
            if (m_UpdateLobbiesRoutine != null) StopCoroutine(m_UpdateLobbiesRoutine);
            m_UpdateLobbiesRoutine = StartCoroutine(UpdateAvailableLobbies());
        }

        private IEnumerator UpdateAvailableLobbies()
        {
            while (true)
            {
                yield return new WaitForSeconds(m_AutoRefreshTime);
                UpdateLobbyDisplay();
            }
        }

        public void HostLobbyAfterFadeOut()
        {
            var warp = XRINetworkGameManager.Instance.networkSceneManager.WarpController;
            var loadMode = XRINetworkGameManager.Instance.networkSceneManager.LoadSceneMode;
            warp.StartFadeOut("Lobby", (sn) =>
            {
                HostLocalRoom(() =>
                {
                    NetworkManager.Singleton.SceneManager.LoadScene(sn, loadMode);
                });
            });
        }

        public void JoinLobbyAfterFadeOut()
        {
            var warp = XRINetworkGameManager.Instance.networkSceneManager.WarpController;
            warp.StartFadeOut("Lobby", (sn) =>
            {
                JoinLocalRoom();
                warp.StartFadeIn(sn);
            });
        }

        private void EnableRefresh()
        {
            m_CooldownImage.enabled = false;
            m_RefreshButton.interactable = true;
        }

        private IEnumerator UpdateButtonCooldown()
        {
            m_RefreshButton.interactable = false;

            m_CooldownImage.enabled = true;
            for (float i = 0; i < m_RefreshCooldownTime; i += Time.deltaTime)
            {
                m_CooldownImage.fillAmount = Mathf.Clamp01(i / m_RefreshCooldownTime);
                yield return null;
            }

            EnableRefresh();
        }

        private async void UpdateLobbyDisplay()
        {
            if (m_CooldownImage.enabled || (int)XRINetworkGameManager.CurrentConnectionState.Value < 2) return;
            if (m_CooldownFillRoutine != null) StopCoroutine(m_CooldownFillRoutine);
            m_CooldownFillRoutine = StartCoroutine(UpdateButtonCooldown());

            await System.Threading.Tasks.Task.Yield(); // Wait for the end of the frame
            if (XRINetworkGameManager.CurrentSessionType == SessionType.LocalOnly)
                return;

            QuerySessionsResults results =
                await MultiplayerService.Instance.QuerySessionsAsync(SessionManager.GetQuickJoinFilterOptions());

            foreach (Transform t in m_LobbyListParent)
            {
                Destroy(t.gameObject);
            }

            if (results != null && results.Sessions.Count > 0)
            {
                foreach (var session in results.Sessions)
                {
                    if (SessionManager.CheckForSessionFilter(session))
                        continue;

                    if (SessionManager.CheckForIncompatibilityFilter(session))
                    {
                        LobbyListSlotUI newLobbyUI = Instantiate(m_LobbyListPrefab, m_LobbyListParent)
                            .GetComponent<LobbyListSlotUI>();
                        newLobbyUI.CreateNonJoinableLobbyUI(session, this, "Version Conflict");
                        continue;
                    }
                }
            }
        }

        public void HostLocalRoom(UnityAction onNetworkConnected)
        {
            if (XRINetworkGameManager.Instance.HostConnection())
            {
                ToggleConnectionSubPanel(ConnectionSubPanel.ConnectionSuccessPanel);
                onNetworkConnected.Invoke();
                OnConnectionSuccessful.Invoke();
            }
            else
            {
                OnConnectionFailed.Invoke();
                Utils.LogError($"Failed to host local room:");
                m_ConnectionFailedText.text = $"<b>Error:</b> Room already exists or could not be created";
                ToggleConnectionSubPanel(ConnectionSubPanel.ConnectionFailurePanel);
            }
        }

        public void JoinLocalRoom()
        {
            if (XRINetworkGameManager.Instance.JoinLocalConnection())
            {
                StartCoroutine(CheckForFailedConnection());
                ToggleConnectionSubPanel(ConnectionSubPanel.ConnectionPanel);
                OnConnectionSuccessful.Invoke();
            }
            else
            {
                FailedToJoinLocal();
            }
        }

        private void FailedToJoinLocal()
        {
            OnConnectionFailed.Invoke();
            Utils.LogError($"Failed to join local room:");
            m_ConnectionFailedText.text = $"<b>Error:</b> Room does not exist or could not be joined";
            ToggleConnectionSubPanel(ConnectionSubPanel.ConnectionFailurePanel);
        }

        private IEnumerator CheckForFailedConnection()
        {
            yield return new WaitForSeconds(k_DirectJoinTimeout);
            if (!NetworkManager.Singleton.IsConnectedClient)
            {
                NetworkManager.Singleton.Shutdown();
                FailedToJoinLocal();
            }
            else
            {
                ToggleConnectionSubPanel(ConnectionSubPanel.ConnectionSuccessPanel);
            }
        }

        /// <summary>
        /// Called from the UI to set the IP address for joining a direct connection.
        /// </summary>
        /// <param name="address">IP address or DNS</param>
        public void SetIP(string address)
        {
            SetIPAsync(address);
        }

        public void UpdateIP()
        {
            this.SetIPAsync(this.GetFullIPAddress());
        }

        public void SetFirstIPBlock(string address) => this.SetIPByIndex(0, address);

        public void SetSecondIPBlock(string address) => this.SetIPByIndex(1, address);

        public void SetThirdIPBlock(string address) => this.SetIPByIndex(2, address);

        public void SetFourthIPBlock(string address) => this.SetIPByIndex(3, address);

        private void SetIPByIndex(int index, string address)
        {
            if (isChangingFromDropdown) return;

            if (index < 0 || index >= m_InputIPAdress.Length || index >= m_Dropdowns.Length) return;

            address = address.Trim();
            if (string.IsNullOrEmpty(address)) address = "0";

            if (m_InputIPAdress[index].Equals(address)) return;

            m_InputIPAdress[index] = address;

            isChangingFromInput = true;
            UpdateDropdownSelection(m_Dropdowns[index], address);
            isChangingFromInput = false;

            Debug.Log($"[Input Changed] Full IP: {GetFullIPAddress()}");
            this.UpdateIP();
        }

        private void OnIPInputEndEdit(int index, string value)
        {
            if (isChangingFromDropdown) return;

            if (m_IPInputFields[index] == null || m_IPInputFields[index] == null) return;

            if (m_InputIPAdress[index].Equals(value)) return;

            m_InputIPAdress[index] = value;

            isChangingFromInput = true;

            this.UpdateDropdownSelection(m_Dropdowns[index], value);

            isChangingFromInput = false;

            Debug.Log($"[Dropdown Changed] Input Field[{index}]Change to: {value} Full IP: {GetFullIPAddress()}");
            this.UpdateIP();
        }

        private void OnDropdownValueChanged(int index, int valueIndex)
        {
            if (isChangingFromInput) return;

            if (index < 0 || index >= m_Dropdowns.Length || index >= m_IPInputFields.Length) return;
            if (m_Dropdowns[index] == null || m_IPInputFields[index] == null) return;

            string selectedText = m_Dropdowns[index].options[valueIndex].text;

            if (m_InputIPAdress[index].Equals(selectedText)) return;

            m_InputIPAdress[index] = selectedText;

            isChangingFromDropdown = true;

            m_IPInputFields[index].text = selectedText;

            isChangingFromDropdown = false;

            Debug.Log($"[Dropdown Changed] Input Field[{index}]Change to: {selectedText} Full IP: {GetFullIPAddress()}");
            this.UpdateIP();
        }

        private void UpdateDropdownSelection(TMP_Dropdown dropdown, string targetValue)
        {
            int targetIndex = -1;
            for (int i = 0; i < dropdown.options.Count; i++)
            {
                if (dropdown.options[i].text == targetValue)
                {
                    targetIndex = i;
                    break;
                }
            }

            if (targetIndex != -1)
            {
                dropdown.value = targetIndex;
            }
            else
            {
                TMP_Dropdown.OptionData newOption = new TMP_Dropdown.OptionData(targetValue);
                dropdown.options.Add(newOption);
                dropdown.value = dropdown.options.Count - 1;
            }
            dropdown.RefreshShownValue();
        }

        private string GetFullIPAddress()
        {
            return string.Join(".", m_InputIPAdress);
        }

        /// <summary>
        ///  Asynchronously sets the IP address for joining a direct connection.
        ///  This method validates the IP address format and resolves it to ensure it's valid.
        /// </summary>
        /// <param name="address">IP address or DNS</param>
        public virtual async void SetIPAsync(string address)
        {
            // Validate the IP address format
            var hostEntry = await System.Net.Dns.GetHostEntryAsync(address);
            if (hostEntry == null || hostEntry.AddressList.Length == 0)
            {
                Utils.LogError($"Failed to resolve IP address: {address}");
                m_ConnectionFailedText.text = $"<b>Error:</b> Invalid IP address";
                ToggleConnectionSubPanel(ConnectionSubPanel.ConnectionFailurePanel);
                return;
            }

            // If multiple addresses are found, log a warning as we only use the first one
            if (hostEntry.AddressList.Length > 1)
                Utils.LogWarning(
                    $"Multiple IP addresses found for {address}. Using the first one: {hostEntry.AddressList[0]}");

            var ipAddress = hostEntry.AddressList[0].ToString();
#if UNITY_EDITOR
            if (XRINetworkGameManager.Instance.isLocalTest) ipAddress = address;
#endif
            var transport = (UnityTransport)NetworkManager.Singleton.NetworkConfig.NetworkTransport;
            transport.SetConnectionData(ipAddress,
                transport.ConnectionData.Port); // Assuming default port is 7777, change as needed
        }

        /// <summary>
        /// Called from the UI buttons to update the max players allowed per room.
        /// </summary>
        /// <param name="count">Amount of players to allow.</param>
        public void UpdatePlayerCount(int count)
        {
            m_PlayerCount = Mathf.Clamp(count, 1, XRINetworkGameManager.maxPlayers);
        }
    }
}