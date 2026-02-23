using UnityEngine;
using UnityEngine.SceneManagement;
using XRMultiplayer;

public class LocalManager : MonoBehaviour
{
    public static LocalManager Instance { get; private set; }

    [SerializeField] private SceneAnnouncerController sceneAnnouncerController;

    public SceneAnnouncerController _SceneAnnouncerController
    { get { return sceneAnnouncerController; } }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
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
}