using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation;

namespace XRMultiplayer
{
    public class PlayerResetter : MonoBehaviour
    {
        [SerializeField] private Vector3 resetPosition = new Vector3(0, 0.15f, 0);
        [SerializeField] private Quaternion resetRotation = Quaternion.identity;

        private TeleportationProvider m_TeleportationProvider;

        private void Start()
        {
            m_TeleportationProvider = FindFirstObjectByType<TeleportationProvider>();

            if (m_TeleportationProvider == null)
            {
                Utils.LogWarning("[PlayerResetter] TeleportationProvider not found in scene.");
                enabled = false;
                return;
            }

            ResetPlayerPosition();
        }

        private void ResetPlayerPosition()
        {
            TeleportRequest teleportRequest = new TeleportRequest
            {
                destinationPosition = resetPosition,
                destinationRotation = resetRotation
            };

            if (!m_TeleportationProvider.QueueTeleportRequest(teleportRequest))
            {
                Utils.LogWarning("[PlayerResetter] Failed to queue teleport request for position reset.");
            }
        }

        // テスト用のコンテキストメニュー
        [ContextMenu("Reset Player Position Now")]
        private void ResetPlayerPositionNow()
        {
            ResetPlayerPosition();
        }
    }
}