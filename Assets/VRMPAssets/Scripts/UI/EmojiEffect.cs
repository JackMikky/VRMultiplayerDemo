using UnityEngine;
using UnityEngine.UI;

public class EmojiEffect : MonoBehaviour
{
    [SerializeField] private Image emojiImage;
    [SerializeField] private GameObject emojiEffectPrefab;

    private float effectHeightOffset = 1f;
    private Button button;
    private Texture2D m_CachedTexture;

    private void Start()
    {
        button = GetComponent<Button>();
        if (button != null)
        {
            button.onClick.AddListener(CreateEmojiEffect);
        }
    }

    private void CreateEmojiEffect()
    {
        var position = Camera.main.transform.position + Camera.main.transform.forward + new Vector3(0, -effectHeightOffset, 0);
        GameObject effectInstance = Instantiate(emojiEffectPrefab, position, emojiEffectPrefab.transform.rotation);

        if (effectInstance.TryGetComponent<ParticleSystemRenderer>(out var renderer))
        {
            SetParticleTexture(renderer);
        }
        else
        {
            var childRenderer = effectInstance.GetComponentInChildren<ParticleSystemRenderer>();
            if (childRenderer != null)
            {
                SetParticleTexture(childRenderer);
            }
        }

        // Destroy effect after particle system finishes
        DestroyEffectWhenComplete(effectInstance);
    }

    private void DestroyEffectWhenComplete(GameObject effectInstance)
    {
        if (effectInstance.TryGetComponent<ParticleSystem>(out var ps))
        {
            float lifetime = ps.main.duration + ps.main.startLifetime.constantMax;
            Destroy(effectInstance, lifetime);
        }
        else
        {
            var childPs = effectInstance.GetComponentInChildren<ParticleSystem>();
            if (childPs != null)
            {
                float lifetime = childPs.main.duration + childPs.main.startLifetime.constantMax;
                Destroy(effectInstance, lifetime);
            }
            else
            {
                // Fallback: destroy after 5 seconds
                Destroy(effectInstance, 5f);
            }
        }
    }

    private void SetParticleTexture(ParticleSystemRenderer renderer)
    {
        if (emojiImage == null || emojiImage.sprite == null) return;

        if (m_CachedTexture == null)
        {
            m_CachedTexture = ExtractTextureFromSprite(emojiImage.sprite);
        }

        if (m_CachedTexture == null) return;

        Material materialInstance = new Material(renderer.material);
        materialInstance.SetTexture("_MainTexture", m_CachedTexture);
        renderer.material = materialInstance;
    }

    /// <summary>
    /// Extracts a standalone Texture2D from an atlas Sprite.
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
            Debug.LogError($"Failed to read Sprite pixels. Please enable Read/Write Enabled in the atlas Import Settings: {e.Message}");
            return null;
        }

        return newTexture;
    }
}