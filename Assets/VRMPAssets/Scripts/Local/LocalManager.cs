using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using XRMultiplayer;

public class LocalManager : MonoBehaviour
{
    public static LocalManager Instance { get; private set; }

    [SerializeField] private GameObject localAvatar;

    public CustomEvent onLobbyLoadStart;

    public CustomEvent onLobbyLoaded;

    private const string lobbySceneName = "Lobby";

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
        // todo 每次加载场景都会调用，需要优化
        SceneManager.sceneLoaded += HandleLobbyLoaded;
    }

    private void Start()
    {
        LoadLocalSceneByName(lobbySceneName);
        onLobbyLoadStart.Invoke();
    }

    public void LoadLocalSceneByName(string sceneName)
    {
        SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
        XRINetworkGameManager.Instance.networkSceneManager.currentSceneName = sceneName;
    }

    private void HandleLobbyLoaded(Scene scene, LoadSceneMode loadSceneMode)
    {
        if (scene.name == lobbySceneName)
            this.onLobbyLoaded.Invoke();
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