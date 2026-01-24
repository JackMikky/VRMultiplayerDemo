using Unity.Netcode;
using UnityEngine;

namespace XRMultiplayer
{
    /// <summary>
    /// Manages network-synchronized emoji effects
    /// </summary>
    [RequireComponent(typeof(NetworkObject))]
    [RequireComponent(typeof(EmojiEffectController))]
    public class NetworkEmojiManager : NetworkBehaviour
    {
        [SerializeField]
        [Tooltip("Emoji effect prefab")]
        private GameObject emojiEffectPrefab;

        [SerializeField]
        [Tooltip("Emoji effect controller reference")]
        private EmojiEffectController emojiEffectController;

        /// <summary>
        /// Spawn emoji effect at specified position and sync to all clients
        /// </summary>
        /// <param name="position">Spawn position</param>
        /// <param name="emojiID">Emoji ID</param>
        public void SpawnEmojiEffect(Vector3 position, int emojiID)
        {
            CreateLocalEmojiEffect(position, emojiID);

            if (IsServer)
            {
                SpawnEmojiEffectClientRpc(position, emojiID);
            }
            else
            {
                SpawnEmojiEffectServerRpc(position, emojiID);
            }
        }

        /// <summary>
        /// Server RPC - Receive client request and broadcast to other clients
        /// </summary>
        [Rpc(SendTo.Server)]
        private void SpawnEmojiEffectServerRpc(Vector3 position, int emojiID)
        {
            CreateLocalEmojiEffect(position, emojiID);

            SpawnEmojiEffectClientRpc(position, emojiID);
        }

        /// <summary>
        /// Client RPC - Create effect on all clients (excluding sender)
        /// </summary>
        [Rpc(SendTo.NotMe)]
        private void SpawnEmojiEffectClientRpc(Vector3 position, int emojiID)
        {
            CreateLocalEmojiEffect(position, emojiID);
        }

        /// <summary>
        /// Create emoji effect locally
        /// </summary>
        private void CreateLocalEmojiEffect(Vector3 position, int emojiID)
        {
            if (emojiEffectPrefab == null)
            {
                Debug.LogError("NetworkEmojiManager: emojiEffectPrefab is not set!");
                return;
            }

            if (emojiEffectController == null)
            {
                Debug.LogError("NetworkEmojiManager: emojiEffectController is not set!");
                return;
            }

            Texture2D emojiTexture = emojiEffectController.GetExtractedTextureByID(emojiID);
            if (emojiTexture == null)
            {
                Debug.LogError($"NetworkEmojiManager: Failed to get emoji texture for ID {emojiID}!");
                return;
            }

            GameObject effectInstance = Instantiate(emojiEffectPrefab, position, emojiEffectPrefab.transform.rotation);

            emojiEffectController.ApplyTextureToParticleSystem(effectInstance, emojiTexture);
            emojiEffectController.DestroyEffectWhenComplete(effectInstance);
        }
    }
}