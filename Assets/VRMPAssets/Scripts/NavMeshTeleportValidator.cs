using UnityEngine;
using UnityEngine.AI;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation;

public class NavMeshTeleportValidator : TeleportationProvider
{
    [SerializeField] private float m_MaxNavMeshDistance = 0.25f;
    [SerializeField] private bool m_DebugMode = true;

    protected override void Update()
    {
        if (!validRequest)
            return;
        if (m_DebugMode)
            Debug.Log($"Teleportation request detected. {currentRequest.destinationPosition}");

        if (this.TryTeleport(currentRequest.destinationPosition))
            base.Update();

        validRequest = false;
    }

    private bool TryTeleport(Vector3 targetPosition)
    {
        NavMeshHit hit;

        if (NavMesh.SamplePosition(targetPosition, out hit, m_MaxNavMeshDistance, NavMesh.AllAreas))
        {
            if (m_DebugMode)
            {
                Debug.Log($"NavMesh found at: {hit.position}, Distance: {Vector3.Distance(targetPosition, hit.position)}");
                Debug.DrawLine(targetPosition, hit.position, Color.green, 2.0f);
            }

            return true;
        }
        else
        {
            if (m_DebugMode)
            {
                Debug.LogWarning($"Invalid teleport target: Not on NavMesh. Position: {targetPosition}");
                Debug.DrawRay(targetPosition, Vector3.up * 2.0f, Color.red, 2.0f);
            }
        }
        return false;
    }
}