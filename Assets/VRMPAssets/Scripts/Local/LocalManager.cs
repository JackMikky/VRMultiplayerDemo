using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using XRMultiplayer;

public class LocalManager : MonoBehaviour
{
    public static LocalManager Instance { get; private set; }

    [SerializeField] GameObject localAvatar;

    public UnityEvent onApplicationStarted;

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
        TrensportToLobby();
    }

    void TrensportToLobby()
    {
        SceneManager.LoadSceneAsync(lobbySceneName, LoadSceneMode.Additive);
        SceneManager.sceneLoaded+=OnLobbyLoaded;
        XRINetworkGameManager.Instance.networkSceneManager.currentSceneName = lobbySceneName;
    }

    void OnLobbyLoaded(Scene scene, LoadSceneMode loadSceneMode)
    {
        this.onApplicationStarted.Invoke();
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