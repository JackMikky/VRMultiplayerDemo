
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using Unity.Netcode;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using System.Security.Cryptography;

public class NetworkSimpleXRSlider : NetworkBehaviour
{
    [Range(0.0f, 1.0f)]
    public float startValue = 1f;

    [Header("References")]
    public Transform handle;

    [Header("Slider Settings")]
    public float length = 0.7f;               // スライダー長さ（m）
    public Vector3 localAxis = Vector3.right; // ローカル軸

    [Header("Value (0-1)")]
    public NetworkVariable<float> value =
        new NetworkVariable<float>(
            1f,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Owner
        );

    XRGrabInteractable grab;
    float halfLength;

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            value.Value = startValue;
        }

        UpdateHandleFromValue();
    }

    void Awake()
    {
        halfLength = length * 0.5f;
        grab = handle.GetComponent<XRGrabInteractable>();

        grab.selectEntered.AddListener(OnGrabbed);
        grab.selectExited.AddListener(OnReleased);
    }

    void Update()
    {
        // Owner のみが位置 → 値を更新
        if (IsOwner && grab.isSelected)
        {
            UpdateValueFromHandle();
        }

        // 全員が 値 → 見た目 を反映
        UpdateHandleFromValue();
    }

    void UpdateValueFromHandle()
    {
        Vector3 localPos = transform.InverseTransformPoint(handle.position);
        float axisValue = Vector3.Dot(localPos, localAxis.normalized);
        axisValue = Mathf.Clamp(axisValue, -halfLength, halfLength);

        float newValue = Mathf.InverseLerp(-halfLength, halfLength, axisValue);
        value.Value = newValue;
    }

    void UpdateHandleFromValue()
    {
        float axisValue = Mathf.Lerp(-halfLength, halfLength, value.Value);
        Vector3 localPos = localAxis.normalized * axisValue;
        handle.position = transform.TransformPoint(localPos);
    }

    // -------- Ownership 制御 --------

    void OnGrabbed(SelectEnterEventArgs args)
    {
        RequestOwnershipServerRpc();
    }

    void OnReleased(SelectExitEventArgs args)
    {
        ReleaseOwnershipServerRpc();
    }

    [ServerRpc(RequireOwnership = false)]
    void RequestOwnershipServerRpc(ServerRpcParams rpcParams = default)
    {
        NetworkObject.ChangeOwnership(
            rpcParams.Receive.SenderClientId
        );
    }

    [ServerRpc(RequireOwnership = false)]
    void ReleaseOwnershipServerRpc()
    {
        NetworkObject.ChangeOwnership(
            NetworkManager.ServerClientId
        );
    }
}
