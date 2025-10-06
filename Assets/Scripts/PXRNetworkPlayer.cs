using Unity.Collections;
using Unity.Netcode;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.XR.Templates.VRMultiplayer;
using XRMultiplayer;


public class PXRNetworkPlayer : NetworkBehaviour
{
    /// <summary>
    /// Singleton Reference for the Local Player.
    /// </summary>
    public static PXRNetworkPlayer LocalPlayer;

    [Header("Avatar Transform References"), Tooltip("Assign to local avatar transform.")]
    public Transform head;

    public Transform leftHand;

    public Transform rightHand;

    [Header("Player Name Tag"), SerializeField, Tooltip("Player Name Tag.")]
    protected PlayerNameTag playerNameTag;

    public string playerName
    {
        get => _playerName.Value.ToString();
    }

    readonly NetworkVariable<FixedString128Bytes> _playerName = new("", NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Owner);

    [Header("Local XR")] public XROrigin xrOrigin;

    /// <summary>
    /// Internal references to the Local Player Transforms.
    /// </summary>
    protected Transform LeftHandOrigin, RightHandOrigin, HeadOrigin;

    [Header("Networked Hands"), SerializeField, Tooltip("Hand Objects to be disabled for the local player.")]
    protected GameObject[] handsObjects;

    void Start()
    {
        if (IsOwner)
        {
            head.gameObject.SetActive(false);
            leftHand.gameObject.SetActive(false);
            rightHand.gameObject.SetActive(false);

            // 绑定 XR Origin
            if (!this.xrOrigin)
                xrOrigin = FindObjectOfType<XROrigin>();
        }
        else
        {
            // 远程玩家：禁用 XR Origin
            if (xrOrigin != null) xrOrigin.gameObject.SetActive(false);
        }
    }

    void Update()
    {
    }

    protected virtual void LateUpdate()
    {
        if (!IsOwner) return;

        if (HeadOrigin != null)
            head.SetPositionAndRotation(HeadOrigin.position, HeadOrigin.rotation);

        if (LeftHandOrigin != null)
            leftHand.SetPositionAndRotation(LeftHandOrigin.position, LeftHandOrigin.rotation);

        if (RightHandOrigin != null)
            rightHand.SetPositionAndRotation(RightHandOrigin.position, RightHandOrigin.rotation);
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        if (IsLocalPlayer)
        {
            // Set Local Player.
            LocalPlayer = this;

            // Get Origin and set head.
            xrOrigin = FindFirstObjectByType<XROrigin>();
            if (xrOrigin != null)
            {
                HeadOrigin = xrOrigin.Camera.transform;
            }
            else
            {
                Utils.Log("No XR Rig Available", 1);
            }

            SetupLocalPlayer();
        }

        // CompleteSetup();
    }

    protected virtual void SetupLocalPlayer()
    {
        foreach (var hand in handsObjects)
        {
            hand.SetActive(false);
        }

        //m_PlayerColor.Value = XRINetworkGameManager.LocalPlayerColor.Value;
        //_playerName.Value = new FixedString128Bytes(XRINetworkGameManager.LocalPlayerName.Value);
        //XRINetworkGameManager.LocalPlayerColor.Subscribe(UpdateLocalPlayerColor);
        //XRINetworkGameManager.LocalPlayerName.Subscribe(UpdateLocalPlayerName);
        //m_VoiceChat.selfMuted.Subscribe(SelfMutedChanged);
        //m_VoiceChat.ToggleSelfMute(true, true);

        //onSpawnedLocal?.Invoke();
    }

    public void SetHandOrigins(Transform left, Transform right)
    {
        LeftHandOrigin = left;
        RightHandOrigin = right;
    }
}