using UnityEngine;
using UnityEngine.SceneManagement;
using XRMultiplayer;
using UnityEngine.InputSystem;

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

    public bool cursorEnable = true;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
#if !UNITY_ANDROID
        Cursor.visible = false;
        cursorEnable = false;
        Cursor.lockState = CursorLockMode.Locked;
#endif
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

    private void Update()
    {
#if !UNITY_ANDROID

        if (Keyboard.current?.escapeKey.wasPressedThisFrame == true)
        {
            cursorEnable = !cursorEnable;

            Cursor.visible = cursorEnable;
            Cursor.lockState = cursorEnable
                ? CursorLockMode.None
                : CursorLockMode.Locked;
        }
#endif
    }

    public void SetLocalPreventionInvisibility(bool value)
    {
        this.itemBox.SetActive(value);
        this.itemSelectButton.SetActive(value);
        this.itemActionButton.SetActive(value);
    }
}