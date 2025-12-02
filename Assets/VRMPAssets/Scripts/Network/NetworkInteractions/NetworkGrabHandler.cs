using Unity.Netcode;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class NetworkGrabHandler : NetworkBehaviour
{
    private XRGrabInteractable grabInteractable;

    private void Awake()
    {
        grabInteractable = GetComponent<XRGrabInteractable>();
        grabInteractable.selectEntered.AddListener(OnGrab);
        grabInteractable.selectExited.AddListener(OnRelease);
    }

    private void OnGrab(SelectEnterEventArgs args)
    {
        var rb = GetComponent<Rigidbody>();
        rb.isKinematic = false;
        RequestGrabServerRpc(NetworkManager.LocalClientId);
    }

    private void OnRelease(SelectExitEventArgs args)
    {
        if (IsOwner)
        {
            var rb = GetComponent<Rigidbody>();
            rb.isKinematic = true;
            ReleaseGrabServerRpc(NetworkManager.LocalClientId);
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void RequestGrabServerRpc(ulong clientId)
    {
        if (NetworkObject.OwnerClientId != clientId)
        {
            NetworkObject.ChangeOwnership(clientId);
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void ReleaseGrabServerRpc(ulong clientId)
    {
        if (NetworkObject.OwnerClientId == clientId)
        {
            NetworkObject.ChangeOwnership(NetworkManager.ServerClientId);
        }
    }
}