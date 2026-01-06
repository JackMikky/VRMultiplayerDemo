using TMPro;
using UnityEngine;

public class SceneProperty : MonoBehaviour
{
    [Tooltip("ó‹µ•\¦Text")]
    public TMP_Text situationText;

    /// <summary>
    /// ƒV[ƒ“‘®«
    /// </summary>
    public enum SceneAttribute
    {
        None,
        Fire, // ‰ÎĞ
        Escape // ”ğ“ï
    }

    [Tooltip("Scene‘®«")]
    public SceneAttribute sceneAttribute;

    private void Start()
    {
        situationText.text = sceneAttribute.ToString();
    }
}