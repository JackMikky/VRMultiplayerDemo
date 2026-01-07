using UnityEngine;

public class EditorComponent : MonoBehaviour
{
    public bool deviceTestMode = false;

    private void Awake()
    {
#if UNITY_EDITOR
        this.gameObject.SetActive(!deviceTestMode);
#else
        this.gameObject.SetActive(false);
#endif
    }
}