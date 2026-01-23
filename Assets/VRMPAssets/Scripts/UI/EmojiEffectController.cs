using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class EmojiEffectController : MonoBehaviour
{
    [SerializeField] private GameObject emojiIconPrefab;
    [SerializeField] private List<Sprite> sprites;

    private void Awake()
    {
        Initialize();
    }

    private void Initialize()
    {
        for (int i = 0; i < sprites.Count; i++)
        {
            GameObject emoji = Instantiate(emojiIconPrefab, transform);
            Image image = emoji.GetComponent<Image>();
            if (image != null)
            {
                image.sprite = sprites[i];
            }
        }
    }
}