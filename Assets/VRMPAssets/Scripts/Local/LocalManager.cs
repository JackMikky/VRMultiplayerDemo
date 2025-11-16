using Unity.Services.Lobbies.Models;
using UnityEditor.SearchService;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using XRMultiplayer;

public class LocalManager : MonoBehaviour
{
    public static LocalManager Instance { get; private set; }

    [SerializeField] private GameObject localAvatar;

    [SerializeField] private GameObject lobbyObject;

    [SerializeField] private GameObject entranceObject;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        XRINetworkGameManager.Connected.Subscribe(HideLocalAvatar);
    }

    private void Start()
    {
        var warp = XRINetworkGameManager.Instance.networkSceneManager.WarpController;
        warp.onWarpFadeOutComplete.AddOnceListener((sceneName) =>
        {
            if (sceneName == "Lobby")
            {
                HideEntranceObject();
                warp.StartFadeIn(sceneName);
                XRINetworkGameManager.Instance.networkSceneManager.onSceneLoaded.AddListener((sceneName) =>
                {
                    warp.StartFadeIn(sceneName);
                });
            }
        });
    }

    public void LoadLocalSceneByName(string sceneName)
    {
        SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
        XRINetworkGameManager.Instance.networkSceneManager.currentSceneName = sceneName;
    }

    private void HideEntranceObject()
    {
        lobbyObject.SetActive(true);
        entranceObject.SetActive(false);
    }

    private void HideLocalAvatar(bool connected)
    {
        this.localAvatar.SetActive(!connected);
    }

    public void SetLocalAvatarInvisibility(bool value)
    {
        this.localAvatar.SetActive(value);
    }
}