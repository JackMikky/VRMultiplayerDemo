using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using XRMultiplayer;

/// <summary>
/// Networked Projectile Launcher.
/// </summary>
public class NetworkProjectileLauncher : NetworkBehaviour
{
    [SerializeField]
    [Tooltip("The point that the project is created")]
    private Transform m_StartPoint = null;

    [SerializeField]
    [Tooltip("The speed at which the projectile is launched")]
    private float m_LaunchSpeed = 1000f;

    [SerializeField]
    [Tooltip("The speed at which the projectile is launched")]
    private int m_MaxProjectilesAllowed = 15;

    [SerializeField]
    [Range(0, 1.5f)]
    [Tooltip("Cooldown time between shots in seconds")]
    private float m_FireCooldown = 0.25f;

    private float m_LastFireTime = 0f;

    private readonly List<CustomProjectile> m_ProjectileQueue = new();

    [Header("Audio")]
    [SerializeField] private AudioSource m_AudioSource;

    [SerializeField] private AudioClip m_AudioClip;

    /// <summary>
    /// Networked Color. This value gets set when ownership is gained.
    /// </summary>
    private readonly NetworkVariable<Color> m_ProjectileColor = new(
        default,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    /// <summary>
    /// Backup color to use for the local player if ownership has not been established when firing the launcher.
    /// </summary>
    /// <remarks>
    /// This will only be used if the player picks up the launcher and fires immediately.
    /// This Color is not synchronized over the network and will result in inconsistency between players when used.
    /// </remarks>
    private Color m_BackupColor;

    private PoolerProjectiles m_ProjectilePooler;

    private Action<int, bool> hitTargetAction;

    public Action<int, bool> HitTargetAction
    {
        set => hitTargetAction = value;
    }

    private void Awake()
    {
        m_ProjectilePooler = FindFirstObjectByType<PoolerProjectiles>();
    }

    /// <inheritdoc/>
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        if (IsServer && IsOwner)
        {
            m_ProjectileColor.Value = XRINetworkGameManager.LocalPlayerColor.Value;
        }
        else if (IsOwner)
        {
            SetProjectileColorServerRpc(XRINetworkGameManager.LocalPlayerColor.Value);
        }
    }

    /// <summary>
    /// Check if the launcher can fire based on cooldown.
    /// </summary>
    private bool CanFire()
    {
        return Time.time >= m_LastFireTime + m_FireCooldown;
    }

    /// <summary>
    /// Synchronize the firing of the projectile.
    /// </summary>
    /// <param name="activate"></param>
    public void FireLauncher(bool activate)
    {
        if (activate)
        {
            if (!CanFire())
            {
                return;
            }

            m_LastFireTime = Time.time;

            Color fireColor = m_BackupColor;
            if (m_ProjectileColor.Value != default)
            {
                fireColor = m_ProjectileColor.Value;
            }

            GameObject newObject = m_ProjectilePooler.GetItem();
            if (!newObject.TryGetComponent(out CustomProjectile projectile))
            {
                Utils.Log("Projectile component not found on projectile object.", 1);
                return;
            }

            projectile.transform.SetPositionAndRotation(m_StartPoint.position, m_StartPoint.rotation);
            if (hitTargetAction != null)
            {
                projectile.Setup(IsOwner, this.NetworkObject.OwnerClientId, fireColor, OnProjectileDestroy, hitTargetAction, OnHitTargetRpc);
            }

            m_AudioSource.PlayOneShot(m_AudioClip);

            if (newObject.TryGetComponent(out Rigidbody rigidBody))
            {
                rigidBody.isKinematic = true;
                rigidBody.isKinematic = false;
                Vector3 force = m_StartPoint.forward * m_LaunchSpeed;
                rigidBody.AddForce(force);
            }

            m_ProjectileQueue.Add(projectile);
            if (m_ProjectileQueue.Count > m_MaxProjectilesAllowed)
            {
                m_ProjectileQueue[0].ResetProjectile();
            }
        }
    }

    [Rpc(SendTo.NotMe)]
    public void OnHitTargetRpc(int UID)
    {
        for (int i = m_ProjectileQueue.Count - 1; i >= 0; i--)
        {
            if (m_ProjectileQueue[i].UID == UID)
            {
                CustomProjectile projectile = m_ProjectileQueue[i];
                m_ProjectileQueue.RemoveAt(i);
                m_ProjectilePooler.ReturnItem(projectile.gameObject);
                return;
            }
        }
        m_ProjectilePooler.ReturnItemByUID(UID);
    }

    private void OnProjectileDestroy(CustomProjectile projectile)
    {
        if (m_ProjectileQueue.Contains(projectile))
        {
            m_ProjectileQueue.Remove(projectile);
        }
        m_ProjectilePooler.ReturnItem(projectile.gameObject);
    }

    /// <inheritdoc/>
    public override void OnGainedOwnership()
    {
        base.OnGainedOwnership();
        if (IsOwner)
        {
            SetProjectileColorServerRpc(XRINetworkGameManager.LocalPlayerColor.Value);
        }
    }

    [Rpc(SendTo.Server)]
    private void SetProjectileColorServerRpc(Color color)
    {
        m_ProjectileColor.Value = color;
    }
}