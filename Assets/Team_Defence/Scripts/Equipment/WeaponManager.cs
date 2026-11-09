using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class WeaponManager : MonoBehaviour
{
    public static WeaponManager Instance { get; private set; }

    [Header("Scene Weapons")]
    [SerializeField] private List<WeaponObject> sceneWeapons = new List<WeaponObject>();

    private readonly Dictionary<WeaponObject, NPCBase> reservedBy = new Dictionary<WeaponObject, NPCBase>();
    private readonly Dictionary<NPCBase, WeaponObject> weaponByNPC = new Dictionary<NPCBase, WeaponObject>();

    public IReadOnlyList<WeaponObject> SceneWeapons => sceneWeapons;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogError("Only one WeaponManager should exist in the scene.", this);
            enabled = false;
            return;
        }

        Instance = this;
        RefreshSceneWeapons();
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    public void RefreshSceneWeapons()
    {
        sceneWeapons.RemoveAll(weapon => weapon == null);

        WeaponObject[] foundWeapons = GameObject.FindGameObjectsWithTag("Weapon").Select(go => go.GetComponent<WeaponObject>()).Where(wo => wo != null).ToArray();
        foreach (WeaponObject weapon in foundWeapons)
        {
            RegisterWeapon(weapon);
        }

        RemoveInvalidReservations();
    }

    public void RegisterWeapon(WeaponObject weapon)
    {
        if (weapon != null && !sceneWeapons.Contains(weapon))
        {
            sceneWeapons.Add(weapon);
        }
    }

    public void UnregisterWeapon(WeaponObject weapon)
    {
        if (weapon == null)
        {
            return;
        }

        if (reservedBy.TryGetValue(weapon, out NPCBase reserver))
        {
            reservedBy.Remove(weapon);
            weaponByNPC.Remove(reserver);
        }

        sceneWeapons.Remove(weapon);
    }

    public bool TryReserveWeapon(NPCBase npc, WeaponObject weapon)
    {
        if (npc == null || weapon == null || weapon.IsPickedUp || !sceneWeapons.Contains(weapon))
        {
            return false;
        }

        if (reservedBy.TryGetValue(weapon, out NPCBase currentReserver))
        {
            return currentReserver == npc;
        }

        if (weaponByNPC.TryGetValue(npc, out WeaponObject currentWeapon))
        {
            return currentWeapon == weapon;
        }

        reservedBy.Add(weapon, npc);
        weaponByNPC.Add(npc, weapon);
        return true;
    }

    public bool TryReserveNearestWeapon(NPCBase npc, out WeaponObject weapon)
    {
        weapon = null;
        if (npc == null)
        {
            return false;
        }

        float nearestDistance = float.MaxValue;

        foreach (WeaponObject candidate in sceneWeapons)
        {
            if (!IsAvailable(candidate))
            {
                continue;
            }

            float distance = (candidate.transform.position - npc.transform.position).sqrMagnitude;
            if (distance < nearestDistance)
            {
                nearestDistance = distance;
                weapon = candidate;
            }
        }

        return weapon != null && TryReserveWeapon(npc, weapon);
    }

    public bool TryReserveRandomWeapon(NPCBase npc, out WeaponObject weapon)
    {
        weapon = null;
        if (npc == null)
        {
            return false;
        }

        List<WeaponObject> availableWeapons = new List<WeaponObject>();
        foreach (WeaponObject candidate in sceneWeapons)
        {
            if (IsAvailable(candidate))
            {
                availableWeapons.Add(candidate);
            }
        }

        if (availableWeapons.Count == 0)
        {
            return false;
        }

        weapon = availableWeapons[Random.Range(0, availableWeapons.Count)];
        return TryReserveWeapon(npc, weapon);
    }

    public void ReleaseReservation(NPCBase npc, WeaponObject weapon)
    {
        if (npc == null || weapon == null)
        {
            return;
        }

        if (reservedBy.TryGetValue(weapon, out NPCBase reserver) && reserver == npc)
        {
            reservedBy.Remove(weapon);
            weaponByNPC.Remove(npc);
        }
    }

    public void ReleaseAllReservations(NPCBase npc)
    {
        if (npc == null || !weaponByNPC.TryGetValue(npc, out WeaponObject weapon))
        {
            return;
        }

        weaponByNPC.Remove(npc);
        reservedBy.Remove(weapon);
    }

    private bool IsAvailable(WeaponObject weapon)
    {
        return weapon != null
            && !weapon.IsPickedUp
            && !reservedBy.ContainsKey(weapon);
    }

    private void RemoveInvalidReservations()
    {
        List<WeaponObject> invalidWeapons = new List<WeaponObject>();
        foreach (KeyValuePair<WeaponObject, NPCBase> reservation in reservedBy)
        {
            if (reservation.Key == null || reservation.Value == null)
            {
                invalidWeapons.Add(reservation.Key);
            }
        }

        foreach (WeaponObject weapon in invalidWeapons)
        {
            if (weapon != null && reservedBy.TryGetValue(weapon, out NPCBase npc))
            {
                weaponByNPC.Remove(npc);
            }

            reservedBy.Remove(weapon);
        }
    }
}