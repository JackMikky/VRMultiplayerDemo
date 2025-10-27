using UnityEngine;
using XRMultiplayer;

public class LocalManager : MonoBehaviour
{
    public static LocalManager Instance { get; private set; }

    [SerializeField] GameObject localAvatar;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject); // ·ÀÖ¹ÖØ¸´ÊµÀý
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        XRINetworkGameManager.Connected.Subscribe(HideLocalAvatar);
    }

    void HideLocalAvatar(bool connected)
    {
        this.localAvatar.SetActive(!connected);
    }

    public void SetLocalAvatarInvisibility(bool value)
    {
        this.localAvatar.SetActive(value);
    }
}