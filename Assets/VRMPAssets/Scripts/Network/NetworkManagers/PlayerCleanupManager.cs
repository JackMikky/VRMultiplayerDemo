using Unity.Netcode;
using UnityEngine;

namespace XRMultiplayer
{
    public class PlayerCleanupManager : MonoBehaviour
    {
        void OnEnable()
        {
            NetworkManager.Singleton.OnClientDisconnectCallback += HandleDisconnect;
        }

        void OnDisable()
        {
            if (NetworkManager.Singleton)
            {
                NetworkManager.Singleton.OnClientDisconnectCallback -= HandleDisconnect;
            }
        }

        void HandleDisconnect(ulong clientId)
        {
            Debug.Log($"Client {clientId} disconnected.");

            if (NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out var client))
            {
                var playerObject = client.PlayerObject;

                if (playerObject != null && playerObject.IsSpawned)
                {
                    playerObject.Despawn();
                    Debug.Log($"Player object for client {clientId} despawned.");
                }
            }
        }
    }
}