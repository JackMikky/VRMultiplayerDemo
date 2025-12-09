using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace XRMultiplayer
{
    public class OfflineMenu : MonoBehaviour
    {
        /// <summary>
        /// Colors to choose from for the player.
        /// </summary>
        [SerializeField, Tooltip("Default name for the player")]
        private Color[] m_PlayerColors;

        [Header("Player Info")]
        /// <summary>
        /// Default name for the player.
        /// </summary>
        [SerializeField, Tooltip("Default name for the player")]
        private string m_DefaultPlayerName = "Unity Creator";

        [SerializeField] private TMP_Text m_PlayerNameText;
        [SerializeField] private TMP_Text m_PlayerInitialText;
        [SerializeField] private Image[] m_PlayerColorIcons;
        [SerializeField] private Image m_VolumeIndicator;
        [SerializeField] private Image m_MicIcon;
        [SerializeField] private Sprite m_MutedSprite;
        [SerializeField] private Sprite m_UnmutedSprite;

        [Header("Panel Objects")]
        [SerializeField]
        private GameObject m_CustomizationPanel;

        [SerializeField] private GameObject m_ConnectionPanel;

        private void Awake()
        {
            XRINetworkGameManager.Connected.Subscribe(OnConnected);
            XRINetworkGameManager.LocalPlayerName.Subscribe(SetPlayerName);
            XRINetworkGameManager.LocalPlayerColor.Subscribe(SetPlayerColor);

            OfflinePlayerAvatar.voiceAmp.Subscribe(UpdateMicIcon);

            SetupPlayerDefaults();
        }

        private void Start()
        {
            ShowCustomization();
            XRINetworkGameManager.Instance.OnConnectionFailedAction += ConnectionFailed;
        }

        private void OnDestroy()
        {
            XRINetworkGameManager.Connected.Unsubscribe(OnConnected);
            XRINetworkGameManager.LocalPlayerName.Unsubscribe(SetPlayerName);
            XRINetworkGameManager.LocalPlayerColor.Unsubscribe(SetPlayerColor);
            OfflinePlayerAvatar.voiceAmp.Unsubscribe(UpdateMicIcon);

            XRINetworkGameManager.Instance.OnConnectionFailedAction -= ConnectionFailed;
        }

        private void SetupPlayerDefaults()
        {
            XRINetworkGameManager.LocalPlayerName.Value = m_DefaultPlayerName;
            XRINetworkGameManager.LocalPlayerColor.Value = m_PlayerColors[Random.Range(0, m_PlayerColors.Length)];
        }

        private void SetPlayerName(string name)
        {
            if (name == string.Empty)
            {
                SetupPlayerDefaults();
                return;
            }

            m_PlayerNameText.text = name;

            string trimmed = name.Trim();
            string initials = "";
            if (trimmed.Length > 0)
            {
                var parts = trimmed.Split((char[])null, System.StringSplitOptions.RemoveEmptyEntries);
                foreach (var p in parts)
                {
                    initials += char.ToUpperInvariant(p[0]);
                }
            }
            m_PlayerInitialText.text = initials;
            m_PlayerNameText.rectTransform.sizeDelta = new Vector2(m_PlayerNameText.preferredWidth * .25f,
                m_PlayerNameText.rectTransform.sizeDelta.y);
        }

        private void SetPlayerColor(Color color)
        {
            foreach (var c in m_PlayerColorIcons)
            {
                c.color = color;
            }
        }

        private void UpdateMicIcon(float amp)
        {
            m_VolumeIndicator.fillAmount = amp;
        }

        public void ShowCustomization()
        {
            m_CustomizationPanel.SetActive(true);
            m_ConnectionPanel.SetActive(false);
        }

        public void CompleteCustomization()
        {
            m_CustomizationPanel.SetActive(false);
            m_ConnectionPanel.SetActive(true);
        }

        private void OnConnected(bool connected)
        {
            if (connected)
            {
                m_CustomizationPanel.SetActive(false);
            }
            else
            {
                gameObject.SetActive(true);
                ShowCustomization();
            }
        }

        private void MutedChanged(bool muted)
        {
            m_MicIcon.sprite = muted ? m_MutedSprite : m_UnmutedSprite;
        }

        private void ConnectionFailed(string reason)
        {
            CompleteCustomization();
        }
    }
}