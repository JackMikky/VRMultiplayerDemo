using System.Collections.Generic;
using UnityEngine;

namespace XRMultiplayer
{
    public class PoolerProjectiles : Pooler
    {
        private readonly Dictionary<int, CustomProjectile> m_ProjectilesByUID = new();

        private int m_NextUID = 0;

        protected override GameObject CreateNewObject()
        {
            GameObject spawnedObject = base.CreateNewObject();

            if (spawnedObject.TryGetComponent(out CustomProjectile projectile))
            {
                projectile.UID = m_NextUID++;
                m_ProjectilesByUID[projectile.UID] = projectile;
            }

            return spawnedObject;
        }

        public void ReturnItemByUID(int UID)
        {
            if (m_ProjectilesByUID.TryGetValue(UID, out CustomProjectile projectile))
            {
                ReturnItem(projectile.gameObject);
            }
        }
    }
}