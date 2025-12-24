using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;
using XRMultiplayer;

namespace VRMPAssets.Scripts.UI
{
    public class SceneChangeMainGraphic : NetworkBehaviour
    {
        [Header("MainGraphic")]
        [SerializeField] private RawImage mainGraphicImage;

        [SerializeField] private Button mainGraphicButton;

        [Header("SubGraphics")]
        [SerializeField] private List<SceneChangeSubGraphic> subGraphics;

        [Header("SubDialog")]
        [SerializeField] private GameObject subDialog;

        [SerializeField] private Button confirmButton;
        [SerializeField] private Button cancelButton;

        [Header("Random Selection")]
        [SerializeField] private bool selectRandomOnStart = true;

        private void Awake()
        {
            var buttonImage = cancelButton.GetComponentInChildren<Image>();
            var originalColor = buttonImage.color;
            var originalZPosition = cancelButton.transform.localPosition.z;
            cancelButton.onClick.AddListener(() =>
            {
                if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsConnectedClient)
                {
                    if (IsHost)
                    {
                        buttonImage.color = originalColor;
                        var pos = buttonImage.transform.localPosition;
                        pos.z = originalZPosition;
                        buttonImage.transform.localPosition = pos;
                        this.subDialog.SetActive(false);
                    }
                }
            });
            this.mainGraphicButton.onClick.AddListener(() =>
            {
                if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsConnectedClient)
                {
                    if (IsHost)
                    {
                        this.subDialog.SetActive(true);
                    }
                }
            });
            this.subDialog.SetActive(false);
        }

        private void Start()
        {
            if (selectRandomOnStart && subGraphics != null && subGraphics.Count > 0)
            {
                SelectRandomSubGraphic();
            }
            else if (!selectRandomOnStart && subGraphics != null && subGraphics.Count > 0)
            {
                SelectDefaultSubGraphic();
            }
        }

        private void SelectDefaultSubGraphic()
        {
            int index = 0;
            SceneChangeSubGraphic defaultSubGraphic = subGraphics[index];

            if (defaultSubGraphic != null)
            {
                defaultSubGraphic.SetDefaultGraphic();
                Debug.Log($"SubGraphic selected: {index}");
            }
        }

        private void SelectRandomSubGraphic()
        {
            int randomIndex = Random.Range(0, subGraphics.Count);
            SceneChangeSubGraphic randomSubGraphic = subGraphics[randomIndex];

            if (randomSubGraphic != null)
            {
                randomSubGraphic.SetDefaultGraphic();
                Debug.Log($"Random SubGraphic selected: {randomIndex}");
            }
        }

        public void UpdateMainGraphic(Texture2D texture, string roomName, SceneChangeSubGraphic subGraphic)
        {
            ApplyMainGraphicUpdate(texture, roomName);

            if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsConnectedClient)
            {
                if (IsHost)
                {
                    int subGraphicIndex = subGraphics != null ? subGraphics.IndexOf(subGraphic) : -1;
                    if (subGraphicIndex >= 0)
                    {
                        UpdateMainGraphicClientRpc(subGraphicIndex, roomName);
                    }
                }
            }
        }

        private void ApplyMainGraphicUpdate(Texture2D texture, string roomName)
        {
            confirmButton.onClick.RemoveAllListeners();
            this.mainGraphicImage.texture = texture;
            confirmButton.onClick.AddListener(() =>
            {
                if (IsHost)
                {
                    this.subDialog.SetActive(false);
                    mainGraphicButton.interactable = false;
                    foreach (var subGraphic in subGraphics)
                    {
                        subGraphic.UpdateInteractable(false);
                    }
                    XRINetworkGameManager.Instance.networkSceneManager.LoadSceneByNameWithWarpFadeOut(roomName);
                }
            });
        }

        [ClientRpc]
        private void UpdateMainGraphicClientRpc(int subGraphicIndex, string roomName)
        {
            if (!IsHost)
            {
                if (subGraphicIndex >= 0 && subGraphicIndex < subGraphics.Count)
                {
                    Texture2D texture = subGraphics[subGraphicIndex].GraphicTexture;
                    ApplyMainGraphicUpdate(texture, roomName);
                }
            }
        }

        [ClientRpc]
        private void SetSubDialogActiveClientRpc(bool value)
        {
            this.subDialog.SetActive(value);
            if (!IsHost && value)
            {
                this.confirmButton.interactable |= false;
                this.cancelButton.interactable |= false;
            }
        }

        public void HideOtherBackgrounds(SceneChangeSubGraphic activeSubGraphic)
        {
            if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsConnectedClient)
            {
                if (!IsHost)
                {
                    return;
                }
            }

            foreach (var subGraphic in subGraphics)
            {
                if (subGraphic != activeSubGraphic)
                {
                    subGraphic.HideBackground();
                }
            }
        }
    }
}