using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;
using TMPro;

namespace VRMPAssets.Scripts.UI
{
    public class SceneChangeSubGraphic : NetworkBehaviour
    {
        [SerializeField] private string changeRoomName;

        private Button _button;

        [SerializeField] private string title;

        [SerializeField] private TMP_Text titleUI;

        [Header("Graphics")]
        [SerializeField] private SceneChangeMainGraphic mainGraphic;

        [SerializeField] private RawImage roomGraphicImage;

        [SerializeField] private Texture2D graphicTexture;

        public Texture2D GraphicTexture => graphicTexture;

        [SerializeField] private GameObject background;

        private void Awake()
        {
            _button = GetComponent<Button>();
            _button.onClick.AddListener(() =>
            {
                if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsConnectedClient)
                {
                    if (!IsHost)
                    {
                        return;
                    }
                    else
                    {
                        mainGraphic.UpdateMainGraphic(this.graphicTexture, this.changeRoomName, this.title, this);
                        mainGraphic.HideOtherBackgrounds(this);
                        ShowBackgroundForAll();
                    }
                }
            });
            background.SetActive(false);
            this.titleUI.text = title;
        }

        private void Start()
        {
            roomGraphicImage.texture = graphicTexture;
        }

        public void SetDefaultGraphic()
        {
            mainGraphic.UpdateMainGraphic(this.graphicTexture, this.changeRoomName, this.title, this);
            mainGraphic.HideOtherBackgrounds(this);

            background.SetActive(true);
        }

        public void ShowBackgroundForAll()
        {
            if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsConnectedClient)
            {
                if (IsHost)
                {
                    background.SetActive(true);
                    ShowBackgroundClientRpc();
                }
            }
            else
            {
                background.SetActive(true);
            }
        }

        [ClientRpc]
        private void ShowBackgroundClientRpc()
        {
            if (!IsHost)
            {
                background.SetActive(true);
            }
        }

        public void HideBackground()
        {
            if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsConnectedClient)
            {
                if (IsHost)
                {
                    HideBackgroundForAll();
                }
            }
            else
            {
                background.SetActive(false);
            }
        }

        private void HideBackgroundForAll()
        {
            background.SetActive(false);
            HideBackgroundClientRpc();
        }

        [ClientRpc]
        private void HideBackgroundClientRpc()
        {
            if (!IsHost)
            {
                background.SetActive(false);
            }
        }

        public void UpdateInteractable(bool interactable)
        {
            _button.interactable = interactable;
        }
    }
}