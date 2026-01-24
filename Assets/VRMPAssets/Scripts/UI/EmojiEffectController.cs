using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace XRMultiplayer
{
    public class EmojiEffectController : MonoBehaviour
    {
        [SerializeField] private GameObject emojiIconPrefab;
        [SerializeField] private List<Sprite> sprites;
        [SerializeField] private GameObject emojiUIContainer;

        private Dictionary<int, Texture2D> m_ExtractedTextures = new Dictionary<int, Texture2D>();

        private void Awake()
        {
            Initialize();
        }

        private void Initialize()
        {
            for (int i = 0; i < this.GetEmojiCount(); i++)
            {
                GameObject emoji = Instantiate(emojiIconPrefab, emojiUIContainer.transform);
                Image image = emoji.GetComponent<Image>();
                EmojiEffect emojiEffect = emoji.GetComponent<EmojiEffect>();
                if (emojiEffect != null && image != null)
                {
                    emojiEffect.emojiID = i;
                    emojiEffect.EmojiEffectController = this;
                    image.sprite = sprites[i];
                }
            }
        }

        /// <summary>
        /// Get sprite by ID
        /// </summary>
        public Sprite GetSpriteByID(int emojiID)
        {
            if (emojiID >= 0 && emojiID < this.GetEmojiCount())
            {
                return sprites[emojiID];
            }
            Debug.LogWarning($"Invalid emoji ID: {emojiID}. Valid range is 0-{this.GetEmojiCount() - 1}");
            return null;
        }

        /// <summary>
        /// Get total emoji count
        /// </summary>
        public int GetEmojiCount()
        {
            return sprites.Count;
        }

        /// <summary>
        /// Get extracted texture by ID (with cache)
        /// </summary>
        public Texture2D GetExtractedTextureByID(int emojiID)
        {
            if (!m_ExtractedTextures.ContainsKey(emojiID))
            {
                Sprite sprite = GetSpriteByID(emojiID);
                if (sprite != null)
                {
                    m_ExtractedTextures[emojiID] = ExtractTextureFromSprite(sprite);
                }
            }
            return m_ExtractedTextures.TryGetValue(emojiID, out var texture) ? texture : null;
        }

        /// <summary>
        /// Apply texture to particle system
        /// </summary>
        public void ApplyTextureToParticleSystem(GameObject effectInstance, Texture2D texture)
        {
            ParticleSystemRenderer renderer = effectInstance.GetComponent<ParticleSystemRenderer>();
            if (renderer == null)
            {
                renderer = effectInstance.GetComponentInChildren<ParticleSystemRenderer>();
            }

            if (renderer != null)
            {
                Material materialInstance = new Material(renderer.material);
                materialInstance.SetTexture("_MainTexture", texture);
                renderer.material = materialInstance;
            }
            else
            {
                Debug.LogWarning("EmojiEffectController: ParticleSystemRenderer component not found!");
            }
        }

        /// <summary>
        /// Destroy effect when particle system completes
        /// </summary>
        public void DestroyEffectWhenComplete(GameObject effectInstance)
        {
            ParticleSystem ps = effectInstance.GetComponent<ParticleSystem>();
            if (ps == null)
            {
                ps = effectInstance.GetComponentInChildren<ParticleSystem>();
            }

            if (ps != null)
            {
                float lifetime = ps.main.duration + ps.main.startLifetime.constantMax;
                Destroy(effectInstance, lifetime);
            }
            else
            {
                Destroy(effectInstance, 5f);
            }
        }

        /// <summary>
        /// Extract standalone Texture2D from sprite atlas
        /// </summary>
        private Texture2D ExtractTextureFromSprite(Sprite sprite)
        {
            if (sprite == null) return null;

            Rect rect = sprite.textureRect;
            Texture2D newTexture = new Texture2D((int)rect.width, (int)rect.height, TextureFormat.RGBA32, false);

            try
            {
                Color[] pixels = sprite.texture.GetPixels(
                    (int)rect.x,
                    (int)rect.y,
                    (int)rect.width,
                    (int)rect.height
                );
                newTexture.SetPixels(pixels);
                newTexture.Apply();
            }
            catch (UnityException e)
            {
                Debug.LogError($"EmojiEffectController: Failed to read sprite pixels. Please enable Read/Write Enabled in the atlas Import Settings: {e.Message}");
                return null;
            }

            return newTexture;
        }

        private void OnDestroy()
        {
            // Clean up cached textures to avoid memory leaks
            foreach (var texture in m_ExtractedTextures.Values)
            {
                if (texture != null)
                {
                    Destroy(texture);
                }
            }
            m_ExtractedTextures.Clear();
        }
    }
}