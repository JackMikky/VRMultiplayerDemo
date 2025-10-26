using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneSwitcher : MonoBehaviour
{
    [SerializeField] private string targetSceneName = "GameScene"; // 要切换的场景名

    public void SwitchScene()
    {
        if (!NetworkManager.Singleton.IsServer)
        {
            Debug.LogWarning("Only the server can initiate scene switching.");
            return;
        }

        if (string.IsNullOrEmpty(targetSceneName))
        {
            Debug.LogError("Target scene name is not set.");
            return;
        }

        Debug.Log($"Switching to scene: {targetSceneName}");
        NetworkManager.Singleton.SceneManager.LoadScene(targetSceneName, LoadSceneMode.Additive);
    }
}