using System;
using System.Collections;
using UnityEngine;

namespace XRMultiplayer
{
    /// <summary>
    /// Represents a projectile in the game.
    /// </summary>
    public class CustomProjectile : MonoBehaviour
    {
        /// <summary>
        /// The trail renderer for the projectile.
        /// </summary>
        [SerializeField] protected TrailRenderer m_TrailRenderer;

        [SerializeField] protected float m_Lifetime = 10.0f;

        /// <summary>
        /// Indicates whether the projectile belongs to the local player.
        /// </summary>
        private bool m_LocalPlayerProjectile;

        public bool isLocalPlayerProjectile
        {
            get { return m_LocalPlayerProjectile; }
        }

        private Action<CustomProjectile> m_OnReturnToPool;

        private Action<int, bool> hitAction;

        private Action<int> onHitReturnEvent;

        private Rigidbody m_Rigidybody;

        [SerializeField] private string m_ColliderTag = "Piggy";

        public int UID { get; set; }

        /// <summary>
        /// Sets up the projectile with the specified parameters.
        /// </summary>
        /// <param name="localPlayer">Indicates whether the projectile belongs to the local player.</param>
        /// <param name="playerColor">The color of the player.</param>
        public void Setup(bool localPlayer, Color playerColor, Action<CustomProjectile> returnToPoolAction = null, Action<int, bool> hitTargetAction = null, Action<int> onHitReturnEvent = null)
        {
            if (m_Rigidybody == null)
            {
                TryGetComponent(out m_Rigidybody);
            }

            m_LocalPlayerProjectile = localPlayer;
            m_TrailRenderer.startColor = playerColor;
            m_TrailRenderer.endColor = playerColor;
            m_TrailRenderer.Clear();
            if (returnToPoolAction != null)
            {
                m_OnReturnToPool = returnToPoolAction;
                StartCoroutine(ResetProjectileAfterTime());
            }

            if (onHitReturnEvent != null)
            {
                this.onHitReturnEvent = onHitReturnEvent;
            }

            if (hitTargetAction != null)
            {
                hitAction = hitTargetAction;
            }
        }

        private IEnumerator ResetProjectileAfterTime()
        {
            yield return new WaitForSeconds(m_Lifetime);
            ResetProjectile();
        }

        /// <summary>
        /// Called when the projectile hits a target.
        /// </summary>
        /// <param name="target">The target that was hit.</param>
        protected virtual void HitTarget(Target target)
        {
            target.TargetHitLocal();
        }

        public void HitTarget(int score, bool isLocalPlayer)
        {
            hitAction?.Invoke(score, isLocalPlayer);
            onHitReturnEvent?.Invoke(UID);
            ResetProjectile();
        }

        public void ResetProjectile()
        {
            StopAllCoroutines();
            m_OnReturnToPool?.Invoke(this);
        }
    }
}