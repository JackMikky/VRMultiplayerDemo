using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace VRMPAssets.Scripts.UI
{
    public class SceneChangeSubGraphic : MonoBehaviour
    {
        [SerializeField] private string changeRoomName;
        public string ChangeRoomName => changeRoomName;

        [SerializeField] private string title;
        public string Title => title;

        [SerializeField] private TMP_Text titleUI;

        [Header("Graphics")]
        [SerializeField] private SceneChangeMainGraphic mainGraphic;

        [SerializeField] private RawImage roomGraphicImage;
        [SerializeField] private Texture2D graphicTexture;
        public Texture2D GraphicTexture => graphicTexture;

        [SerializeField] private GameObject background;

        private Button _button;

        private void Awake()
        {
            _button = GetComponent<Button>();
            if (titleUI != null) titleUI.text = title;
            if (background != null) background.SetActive(false);

            _button.onClick.AddListener(() =>
            {
                if (mainGraphic != null)
                {
                    mainGraphic.OnSubGraphicClicked(this);
                }
            });
        }

        private void Start()
        {
            if (roomGraphicImage != null)
            {
                roomGraphicImage.texture = graphicTexture;
            }
        }

        public void SetBackgroundActive(bool active)
        {
            if (background != null)
            {
                background.SetActive(active);
            }
        }

        public void UpdateInteractable(bool interactable)
        {
            if (_button != null)
            {
                _button.interactable = interactable;
            }
        }
    }
}