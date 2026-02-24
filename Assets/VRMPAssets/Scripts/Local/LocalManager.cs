using UnityEngine;
using UnityEngine.SceneManagement;
using XRMultiplayer;

public class LocalManager : MonoBehaviour
{
    public static LocalManager Instance { get; private set; }

    [SerializeField] private SceneAnnouncerController sceneAnnouncerController;

    public SceneAnnouncerController _SceneAnnouncerController
    { get { return sceneAnnouncerController; } }

    [Header("Prevention Scene UI Elements")]
    [SerializeField] private GameObject itemBox;

    [SerializeField] private GameObject itemSelectButton;
    [SerializeField] private GameObject itemActionButton;

    public GameObject ItemBoxObject => itemBox;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void Start()
    {
        SetLocalPreventionInvisibility(false);
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

    public void SetLocalPreventionInvisibility(bool value)
    {
        this.itemBox.SetActive(value);
        this.itemSelectButton.SetActive(value);
        this.itemActionButton.SetActive(value);
    }
}