using System.Collections.Generic;
using TMPro;
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
        [SerializeField] private TMP_Text titleUI;

        [Header("SubGraphics Setup")]
        [SerializeField] private List<SubGraphicSetting> subGraphics;
        [SerializeField] private GameObject subGraphicsContent;

        [Header("SubDialog")]
        [SerializeField] private GameObject subDialog;

        [SerializeField] private Button confirmButton;
        [SerializeField] private Button cancelButton;

        [Header("Random Selection")]
        [SerializeField] private bool selectRandomOnStart = true;

        private SubGraphicSetting currentSubGraphicSetting;
        private string currentRoomName;

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
                        SetStandbyObjects(false);
                        HideAllStandbyObjectClientRpc();
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
                        ShowStandbyObjectClientRpc();
                    }
                }
            });

            this.subDialog.SetActive(false);
            foreach (var item in subGraphics)
            {
                if (item.standbyObject != null) item.standbyObject.SetActive(false);
            }
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
            if (subGraphics[0].graphic != null)
            {
                SelectSubGraphicInternal(subGraphics[0].graphic);
            }
        }

        private void SelectRandomSubGraphic()
        {
            int randomIndex = Random.Range(0, subGraphics.Count);
            if (subGraphics[randomIndex].graphic != null)
            {
                SelectSubGraphicInternal(subGraphics[randomIndex].graphic);
            }
        }

        public void OnSubGraphicClicked(SceneChangeSubGraphic clickedSubGraphic)
        {
            if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsConnectedClient)
            {
                if (!IsHost) return;
            }

            SelectSubGraphicInternal(clickedSubGraphic);

            if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsConnectedClient && IsHost)
            {
                SyncSelectedRoomClientRpc(clickedSubGraphic.ChangeRoomName, clickedSubGraphic.Title);
            }
        }

        private void SelectSubGraphicInternal(SceneChangeSubGraphic subGraphic)
        {
            this.currentRoomName = subGraphic.ChangeRoomName;

            for (int i = 0; i < subGraphics.Count; i++)
            {
                if (subGraphics[i].graphic != null && subGraphics[i].graphic.ChangeRoomName == currentRoomName)
                {
                    currentSubGraphicSetting = subGraphics[i];
                    break;
                }
            }

            ApplyMainGraphicUpdate(subGraphic.GraphicTexture, subGraphic.ChangeRoomName, subGraphic.Title);
            UpdateAllSubGraphicBackgrounds(currentRoomName);
        }

        private void ApplyMainGraphicUpdate(Texture2D texture, string roomName, string title)
        {
            confirmButton.onClick.RemoveAllListeners();
            this.mainGraphicImage.texture = texture;
            this.titleUI.text = title;

            confirmButton.onClick.AddListener(() =>
            {
                if (IsHost)
                {
                    this.subDialog.SetActive(false);
                    mainGraphicButton.interactable = false;

                    SceneChangeSubGraphic[] allSubGraphics = subGraphicsContent.GetComponentsInChildren<SceneChangeSubGraphic>(true);
                    foreach (var sg in allSubGraphics)
                    {
                        sg.UpdateInteractable(false);
                    }

                    XRINetworkGameManager.Instance.networkSceneManager.LoadSceneByNameWithWarpFadeOut(roomName);
                }
            });
        }

        private void UpdateAllSubGraphicBackgrounds(string activeRoomName)
        {
            SceneChangeSubGraphic[] allSubGraphics = subGraphicsContent.GetComponentsInChildren<SceneChangeSubGraphic>(true);
            foreach (var sg in allSubGraphics)
            {
                bool isSelected = (sg.ChangeRoomName == activeRoomName);
                sg.SetBackgroundActive(isSelected);
            }
        }

        [ClientRpc]
        private void SyncSelectedRoomClientRpc(string roomName, string title)
        {
            if (!IsHost)
            {
                SceneChangeSubGraphic[] allSubGraphics = subGraphicsContent.GetComponentsInChildren<SceneChangeSubGraphic>(true);
                Texture2D texture = null;

                foreach (var sg in allSubGraphics)
                {
                    if (sg.ChangeRoomName == roomName)
                    {
                        texture = sg.GraphicTexture;
                        break;
                    }
                }

                this.currentRoomName = roomName;
                for (int i = 0; i < subGraphics.Count; i++)
                {
                    if (subGraphics[i].graphic != null && subGraphics[i].graphic.ChangeRoomName == roomName)
                    {
                        currentSubGraphicSetting = subGraphics[i];
                        break;
                    }
                }

                if (texture != null)
                {
                    ApplyMainGraphicUpdate(texture, roomName, title);
                }
                UpdateAllSubGraphicBackgrounds(roomName);
            }
        }

        [ClientRpc]
        private void ShowStandbyObjectClientRpc()
        {
            SetStandbyObjects(false);
            if (currentSubGraphicSetting != null && currentSubGraphicSetting.standbyObject != null)
            {
                currentSubGraphicSetting.standbyObject.SetActive(true);
            }
        }

        [ClientRpc]
        private void HideAllStandbyObjectClientRpc()
        {
            SetStandbyObjects(false);
        }

        private void SetStandbyObjects(bool value)
        {
            this.subGraphics.ForEach(sg =>
            {
                if (sg.standbyObject != null) sg.standbyObject.SetActive(value);
            });
        }
    }
}