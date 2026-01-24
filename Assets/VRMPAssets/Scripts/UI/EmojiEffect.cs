using UnityEngine;
using UnityEngine.UI;
using XRMultiplayer;

namespace XRMultiplayer
{
    public class EmojiEffect : MonoBehaviour
    {
        [SerializeField] private GameObject emojiEffectPrefab;

        [SerializeField]
        [Tooltip("Cooldown time in seconds")]
        private float cooldownTime = 1f;

        public int emojiID;

        private float effectHeightOffset = 1f;
        private Button button;
        private float m_LastClickTime = -999f;
        private EmojiEffectController m_EmojiEffectController;

        public EmojiEffectController EmojiEffectController
        {
            set { m_EmojiEffectController = value; }
        }

        private void Start()
        {
            button = GetComponent<Button>();
            if (button != null)
            {
                button.onClick.AddListener(CreateEmojiEffect);
            }
        }

        private void FixedUpdate()
        {
            if (button != null)
            {
                bool isCoolingDown = Time.time - m_LastClickTime < cooldownTime;
                button.interactable = !isCoolingDown;
            }
        }

        private void CreateEmojiEffect()
        {
            if (Time.time - m_LastClickTime < cooldownTime)
            {
                Debug.Log($"EmojiEffect: Cooling down, please wait {cooldownTime - (Time.time - m_LastClickTime):F1} seconds");
                return;
            }

            m_LastClickTime = Time.time;

            var position = Camera.main.transform.position + Camera.main.transform.forward + new Vector3(0, -effectHeightOffset, 0);
            var emojiManager = XRINetworkGameManager.Instance.networkEmojiManager;

            if (emojiManager != null && emojiManager.IsSpawned)
            {
                emojiManager.SpawnEmojiEffect(position, emojiID);
            }
            else
            {
                CreateLocalEffect(position);
            }
        }

        /// <summary>
        /// Create local effect (for offline mode or as fallback)
        /// </summary>
        private void CreateLocalEffect(Vector3 position)
        {
            if (m_EmojiEffectController == null)
            {
                Debug.LogError("EmojiEffect: EmojiEffectController is not set!");
                return;
            }

            Texture2D emojiTexture = m_EmojiEffectController.GetExtractedTextureByID(emojiID);
            if (emojiTexture == null)
            {
                Debug.LogError($"EmojiEffect: Failed to get emoji texture for ID {emojiID}!");
                return;
            }

            GameObject effectInstance = Instantiate(emojiEffectPrefab, position, emojiEffectPrefab.transform.rotation);
            m_EmojiEffectController.ApplyTextureToParticleSystem(effectInstance, emojiTexture);
            m_EmojiEffectController.DestroyEffectWhenComplete(effectInstance);
        }
    }
}