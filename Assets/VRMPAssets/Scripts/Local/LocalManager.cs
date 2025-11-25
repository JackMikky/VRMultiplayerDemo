using Unity.Netcode;
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

        XRINetworkGameManager.Instance.networkSceneManager.onSceneLoaded.AddOnceListener((sceneName) =>
        {
            if (sceneName == "Lobby")
            {
                warp.StartFadeIn(sceneName);
                Debug.Log("Fade in start");
            }
        });
    }

    public void LoadLocalSceneByName(string sceneName)
    {
        SceneManager.LoadSceneAsync(sceneName, XRINetworkGameManager.Instance.networkSceneManager.LoadSceneMode);
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