using UnityEngine;
using UnityEngine.UI;

public class EmojiEffect : MonoBehaviour
{
    [SerializeField] private Image emojiImage;
    [SerializeField] private GameObject emojiEffectPrefab;

    public Vector3 effectOffset;
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
    }

    private void SetParticleTexture(ParticleSystemRenderer renderer)
    {
        if (emojiImage == null || emojiImage.sprite == null) return;

        // 使用缓存的 Texture，避免重复提取
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
    /// 从图集的 Sprite 中提取单独的 Texture2D
    /// </summary>
    private Texture2D ExtractTextureFromSprite(Sprite sprite)
    {
        if (sprite == null) return null;

        // 获取 Sprite 在图集中的区域
        Rect rect = sprite.textureRect;

        // 创建新的 Texture2D
        Texture2D newTexture = new Texture2D((int)rect.width, (int)rect.height, TextureFormat.RGBA32, false);

        // 从原始图集中读取像素
        // 注意：需要在图集的 Import Settings 中启用 Read/Write Enabled
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
            Debug.LogError($"无法读取 Sprite 像素，请在图集的 Import Settings 中启用 Read/Write Enabled: {e.Message}");
            return null;
        }

        return newTexture;
    }
}