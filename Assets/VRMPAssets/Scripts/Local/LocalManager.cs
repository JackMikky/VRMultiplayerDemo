using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using XRMultiplayer;

public class LocalManager : MonoBehaviour
{
    public static LocalManager Instance { get; private set; }

    [SerializeField] GameObject localAvatar;

    public UnityEvent onLobbyLoadStart;

    public UnityEvent onLobbyLoaded;

    const string lobbySceneName = "Lobby";
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

    void Start()
    {
        LoadLocalSceneByName(lobbySceneName);
        SceneManager.sceneLoaded += HandleLobbyLoaded;
    }

   public void LoadLocalSceneByName(string sceneName)
    {
        SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
        XRINetworkGameManager.Instance.networkSceneManager.currentSceneName = sceneName;
        onLobbyLoadStart.Invoke();
    }

    void HandleLobbyLoaded(Scene scene, LoadSceneMode loadSceneMode)
    {
        this.onLobbyLoaded.Invoke();
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