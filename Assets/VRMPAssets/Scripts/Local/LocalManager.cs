using UnityEngine;
using UnityEngine.SceneManagement;
using XRMultiplayer;

public class LocalManager : MonoBehaviour
{
    public static LocalManager Instance { get; private set; }

    [SerializeField] private GameObject localAvatar;

    [SerializeField] private GameObject itemBox;
    [SerializeField] private GameObject itemSelectButton;
    [SerializeField] private GameObject itemActionButton;

    public GameObject ItemBoxObject => itemBox;

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
        localAvatar.SetActive(!connected);

        // ���݂̃V�[�������擾
        string currentSceneName = SceneManager.GetActiveScene().name;
        bool isPreventionBasic = currentSceneName != "Prevention_Basic";


        itemBox.SetActive(!isPreventionBasic);

        itemSelectButton.SetActive(!isPreventionBasic);

        itemActionButton.SetActive(!isPreventionBasic);
    }

    public void SetLocalAvatarInvisibility(bool value)
    {
        this.localAvatar.SetActive(value);
    }

    public void SetLocalPreventionInvisibility(bool value)
    {
        this.itemBox.SetActive(value);
        this.itemSelectButton.SetActive(value);
        this.itemActionButton.SetActive(value);
    }
}