using UnityEngine;
using UnityEngine.UI;

namespace VRMPAssets.Scripts.UI
{
    public class SceneChangeSubGraphic : MonoBehaviour
    {
        [SerializeField] private string changeRoomName;

        private Button _button;

        public Button button => _button;

        [Header("Graphics")]
        [SerializeField] private SceneChangeMainGraphic mainGraphic;

        [SerializeField] private RawImage roomGraphicImage;

        [SerializeField] private Texture2D graphicTexture;

        [SerializeField] private GameObject background;

        private void Awake()
        {
            _button = GetComponent<Button>();
            _button.onClick.AddListener(() =>
            {
                mainGraphic.UpdateMainGraphic(graphicTexture, changeRoomName);

                mainGraphic.HideOtherBackgrounds(this);
                background.SetActive(true);
            });
            background.SetActive(false);
        }

        private void Start()
        {
            roomGraphicImage.texture = graphicTexture;
        }

        public void ShowBackground()
        {
            background.SetActive(true);
        }

        public void HideBackground()
        {
            background.SetActive(false);
        }

        public void UpdateInteractable(bool interactable)
        {
            _button.interactable = interactable;
        }
    }
}