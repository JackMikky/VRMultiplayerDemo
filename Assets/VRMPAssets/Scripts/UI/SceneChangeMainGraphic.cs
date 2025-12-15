using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using XRMultiplayer;
using Unity.Netcode;

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

        private string selectedRoomName;

        private void Awake()
        {
            var buttonImage = cancelButton.GetComponentInChildren<Image>();
            var originalColor = buttonImage.color;
            var originalZPosition = cancelButton.transform.localPosition.z;
            cancelButton.onClick.AddListener(() =>
            {
                buttonImage.color = originalColor;
                var pos = buttonImage.transform.localPosition;
                pos.z = originalZPosition;
                buttonImage.transform.localPosition = pos;
                this.subDialog.SetActive(false);
            });

            this.subDialog.SetActive(false);
        }

        private void Start()
        {
            if (selectRandomOnStart && subGraphics != null && subGraphics.Count > 0)
            {
                SelectRandomSubGraphic();
            }
            else if (selectRandomOnStart && subGraphics != null && subGraphics.Count > 0)
            {
                SelectDefaultSubGraphic();
            }
        }

        /// <summary>
        /// subGraphicsからランダムに1つ選択してボタンを発火する
        /// </summary>
        private void SelectDefaultSubGraphic()
        {
            int index = 0;
            SceneChangeSubGraphic randomSubGraphic = subGraphics[index];

            if (randomSubGraphic != null)
            {
                randomSubGraphic.button.onClick.Invoke();
                Debug.Log($"SubGraphic selected: {index}");
            }
        }

        /// <summary>
        /// subGraphicsからランダムに1つ選択してボタンを発火する
        /// </summary>
        private void SelectRandomSubGraphic()
        {
            int randomIndex = Random.Range(0, subGraphics.Count);
            SceneChangeSubGraphic randomSubGraphic = subGraphics[randomIndex];

            if (randomSubGraphic != null)
            {
                randomSubGraphic.button.onClick.Invoke();
                Debug.Log($"Random SubGraphic selected: {randomIndex}");
            }
        }

        public void UpdateMainGraphic(Texture2D texture, string roomName)
        {
            // ローカルで更新
            ApplyMainGraphicUpdate(texture, roomName);

            // ネットワーク経由で全Clientに通知
            if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsConnectedClient)
            {
                if (IsHost)
                {
                    // SubGraphicのインデックスを取得して送信
                    int subGraphicIndex = GetSubGraphicIndexByRoomName(roomName);
                    if (subGraphicIndex >= 0)
                    {
                        UpdateMainGraphicClientRpc(subGraphicIndex, roomName);
                    }
                }
            }
        }

        /// <summary>
        /// MainGraphicの更新を実際に適用する
        /// </summary>
        private void ApplyMainGraphicUpdate(Texture2D texture, string roomName)
        {
            confirmButton.onClick.RemoveAllListeners();
            this.mainGraphicImage.texture = texture;
            this.selectedRoomName = roomName;
            confirmButton.onClick.AddListener(() =>
            {
                this.subDialog.SetActive(false);
                mainGraphicButton.interactable = false;
                foreach (var subGraphic in subGraphics)
                {
                    subGraphic.UpdateInteractable(false);
                }
                XRINetworkGameManager.Instance.networkSceneManager.LoadSceneByNameWithWarpFadeOut(selectedRoomName);
            });
        }

        /// <summary>
        /// RoomNameからSubGraphicのインデックスを取得
        /// </summary>
        private int GetSubGraphicIndexByRoomName(string roomName)
        {
            for (int i = 0; i < subGraphics.Count; i++)
            {
                // SubGraphicのChangeRoomNameと比較するため、リフレクションを使用
                var changeRoomNameField = subGraphics[i].GetType().GetField("changeRoomName",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (changeRoomNameField != null)
                {
                    string changeRoomName = (string)changeRoomNameField.GetValue(subGraphics[i]);
                    if (changeRoomName == roomName)
                    {
                        return i;
                    }
                }
            }
            return -1;
        }

        /// <summary>
        /// 全クライアントにMainGraphic更新を通知
        /// </summary>
        [ClientRpc]
        private void UpdateMainGraphicClientRpc(int subGraphicIndex, string roomName)
        {
            if (!IsHost && subGraphicIndex >= 0 && subGraphicIndex < subGraphics.Count)
            {
                SceneChangeSubGraphic subGraphic = subGraphics[subGraphicIndex];

                // SubGraphicからTextureを取得
                var graphicTextureField = subGraphic.GetType().GetField("graphicTexture",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (graphicTextureField != null)
                {
                    Texture2D texture = (Texture2D)graphicTextureField.GetValue(subGraphic);
                    ApplyMainGraphicUpdate(texture, roomName);
                }
            }
        }

        public void HideSubGraphicBackground()
        {
            foreach (var subGraphic in subGraphics)
            {
                subGraphic.HideBackground();
            }
        }

        /// <summary>
        /// 指定された SubGraphic 以外のすべての背景を非表示にする
        /// Hostのみ実行可能
        /// </summary>
        public void HideOtherBackgrounds(SceneChangeSubGraphic activeSubGraphic)
        {
            // Hostチェック
            if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsConnectedClient)
            {
                if (!IsHost)
                {
                    Debug.LogWarning("背景の制御はHostのみ実行できます。");
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