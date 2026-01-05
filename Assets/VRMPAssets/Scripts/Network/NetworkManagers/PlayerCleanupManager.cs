using Unity.Netcode;
using UnityEngine;

namespace XRMultiplayer
{
    public class PlayerCleanupManager : MonoBehaviour
    {
        private void Start()
        {
            NetworkManager.Singleton.OnClientDisconnectCallback += HandleDisconnect;
        }

        private void OnDisable()
        {
            if (NetworkManager.Singleton)
            {
                NetworkManager.Singleton.OnClientDisconnectCallback -= HandleDisconnect;
            }
        }

        private void HandleDisconnect(ulong clientId)
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