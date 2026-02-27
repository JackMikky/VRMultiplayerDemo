
using Unity.Netcode;
using UnityEngine;
using System.Collections;

public class SceneEventLogger : MonoBehaviour
{
    private bool _subscribed;

    private void Awake()
    {
        DontDestroyOnLoad(gameObject); // シーンまたぎでログしたい場合
    }

    private void OnEnable()
    {
        StartCoroutine(SubscribeWhenReady());
    }

    private IEnumerator SubscribeWhenReady()
    {
        while (NetworkManager.Singleton == null)
        {
            yield return null;
        }
        while (NetworkManager.Singleton.SceneManager == null)
        {
            yield return null;
        }

        if (!_subscribed)
        {
            NetworkManager.Singleton.SceneManager.OnSceneEvent += OnSceneEvent;
            _subscribed = true;
            Debug.Log("[SceneEventLogger] Subscribed to OnSceneEvent.");
        }
    }

    private void OnDisable() => TryUnsubscribe();
    private void OnDestroy() => TryUnsubscribe();

    private void TryUnsubscribe()
    {
        if (_subscribed && NetworkManager.Singleton != null && NetworkManager.Singleton.SceneManager != null)
        {
            NetworkManager.Singleton.SceneManager.OnSceneEvent -= OnSceneEvent;
            _subscribed = false;
        }
    }

    private void OnSceneEvent(SceneEvent e)
    {
        // バージョン差を気にせず、列挙名をそのまま吐く
        Debug.Log($"[NGO] {e.SceneEventType} | SceneName='{e.SceneName}' | ClientId={e.ClientId} | Mode={e.LoadSceneMode}");
    }
}