using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using XRMultiplayer;

namespace VRMPAssets.Scripts.UI
{
    public class SceneChangeMainGraphic : MonoBehaviour
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

        public void HideSubGraphicBackground()
        {
            foreach (var subGraphic in subGraphics)
            {
                subGraphic.HideBackground();
            }
        }

        /// <summary>
        /// 指定された SubGraphic 以外のすべての背景を非表示にする
        /// </summary>
        public void HideOtherBackgrounds(SceneChangeSubGraphic activeSubGraphic)
        {
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