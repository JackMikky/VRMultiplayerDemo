using UnityEngine;
using UnityEngine.UI;
using XRMultiplayer;

[RequireComponent(typeof(Button))]
public class SceneButtonInitializer : MonoBehaviour, IUIInitializer
{
    [SerializeField] private string changeToSceneName;

    public void InitializeUI()
    {
        var button = GetComponent<Button>();
        button.onClick.AddListener(() => XRINetworkGameManager.Instance.networkSceneManager.LoadSceneByNameWithWarpFadeOut(changeToSceneName));
    }

    private void OnEnable()
    {
        InitializeUI();
    }

    private void OnDisable()
    {
        var button = GetComponent<Button>();
        button.onClick.RemoveAllListeners();
    }

    private void OnDestroy()
    {
        var button = GetComponent<Button>();
        button.onClick.RemoveAllListeners();
    }
}