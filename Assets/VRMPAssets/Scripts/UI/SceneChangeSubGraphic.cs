using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;

namespace VRMPAssets.Scripts.UI
{
    public class SceneChangeSubGraphic : NetworkBehaviour
    {
        [SerializeField] private string changeRoomName;

        // 外部からアクセス可能にする
        public string ChangeRoomName => changeRoomName;

        private Button _button;

        public Button button => _button;

        [Header("Graphics")]
        [SerializeField] private SceneChangeMainGraphic mainGraphic;

        [SerializeField] private RawImage roomGraphicImage;

        [SerializeField] private Texture2D graphicTexture;

        // 外部からアクセス可能にする
        public Texture2D GraphicTexture => graphicTexture;

        [SerializeField] private GameObject background;

        private void Awake()
        {
            _button = GetComponent<Button>();
            _button.onClick.AddListener(() =>
            {
                mainGraphic.UpdateMainGraphic(graphicTexture, changeRoomName);

                mainGraphic.HideOtherBackgrounds(this);

                // Hostのみ背景を表示できる
                if (IsHost || !NetworkManager.Singleton.IsConnectedClient)
                {
                    ShowBackgroundForAll();
                }
            });
            background.SetActive(false);
        }

        private void Start()
        {
            roomGraphicImage.texture = graphicTexture;
        }

        /// <summary>
        /// Hostが呼び出して全クライアントに背景表示を通知
        /// </summary>
        public void ShowBackgroundForAll()
        {
            if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsConnectedClient)
            {
                if (IsHost)
                {
                    // Hostの場合、すぐに表示してClientRpcを送信
                    background.SetActive(true);
                    ShowBackgroundClientRpc();
                }
            }
            else
            {
                // ネットワーク接続がない場合はローカルで表示
                background.SetActive(true);
            }
        }

        /// <summary>
        /// 全クライアントに背景表示を通知
        /// </summary>
        [ClientRpc]
        private void ShowBackgroundClientRpc()
        {
            if (!IsHost)
            {
                background.SetActive(true);
            }
        }

        public void ShowBackground()
        {
            // Hostのみ実行可能
            if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsConnectedClient)
            {
                if (IsHost)
                {
                    ShowBackgroundForAll();
                }
                else
                {
                    Debug.LogWarning("背景の表示はHostのみ実行できます。");
                }
            }
            else
            {
                // ネットワーク接続がない場合はローカルで表示
                background.SetActive(true);
            }
        }

        public void HideBackground()
        {
            // Hostのみ実行可能
            if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsConnectedClient)
            {
                if (IsHost)
                {
                    HideBackgroundForAll();
                }
                else
                {
                    Debug.LogWarning("背景の非表示はHostのみ実行できます。");
                }
            }
            else
            {
                // ネットワーク接続がない場合はローカルで非表示
                background.SetActive(false);
            }
        }

        /// <summary>
        /// Hostが呼び出して全クライアントに背景非表示を通知
        /// </summary>
        private void HideBackgroundForAll()
        {
            // Hostの場合、すぐに非表示にしてClientRpcを送信
            background.SetActive(false);
            HideBackgroundClientRpc();
        }

        /// <summary>
        /// 全クライアントに背景非表示を通知
        /// </summary>
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