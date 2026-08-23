using UnityEngine;

public class EditorComponent : MonoBehaviour
{
    public bool deviceTestMode = false;

    private void Awake()
    {
#if UNITY_EDITOR
        this.gameObject.SetActive(!deviceTestMode);
#elif PLATFORM_ANDROID
        this.gameObject.SetActive(false);
#else
        this.gameObject.SetActive(true);
#endif
    }
}