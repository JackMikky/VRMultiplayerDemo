using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class SceneItemManager : MonoBehaviour
{
    public static SceneItemManager Instance { get; private set; }

    [Header("Scene Weapons")]
    [SerializeField] private List<ItemObject> sceneWeapons = new List<ItemObject>();

    [Header("Searchable Objects")]
    [SerializeField] private List<SearchableObject> searchableObjects = new List<SearchableObject>();

    private readonly Dictionary<ItemObject, NPCBase> reservedBy = new Dictionary<ItemObject, NPCBase>();
    private readonly Dictionary<NPCBase, ItemObject> weaponByNPC = new Dictionary<NPCBase, ItemObject>();
    private readonly Dictionary<SearchableObject, NPCBase> searchableReservedBy = new Dictionary<SearchableObject, NPCBase>();
    private readonly Dictionary<NPCBase, SearchableObject> searchableByNPC = new Dictionary<NPCBase, SearchableObject>();

    public IReadOnlyList<ItemObject> SceneWeapons => sceneWeapons;
    public IReadOnlyList<SearchableObject> SearchableObjects => searchableObjects;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogError("Only one SceneItemManager should exist in the scene.", this);
            enabled = false;
            return;
        }

        Instance = this;
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

        ItemObject[] foundWeapons = GameObject.FindGameObjectsWithTag("Weapon").Select(go => go.GetComponent<ItemObject>()).Where(wo => wo != null).ToArray();
        foreach (ItemObject weapon in foundWeapons)
        {
            RegisterWeapon(weapon);
        }

        RemoveInvalidReservations();
    }

    public void RefreshSearchableObjects()
    {
        searchableObjects.RemoveAll(searchable => searchable == null);

        SearchableObject[] foundSearchableObjects = FindObjectsOfType<SearchableObject>();
        foreach (SearchableObject searchableObject in foundSearchableObjects)
        {
            RegisterSearchableObject(searchableObject);
        }

        RemoveInvalidReservations();
    }

    public void RegisterWeapon(ItemObject weapon)
    {
        if (weapon != null && !sceneWeapons.Contains(weapon))
        {
            sceneWeapons.Add(weapon);
        }
    }

    public void RegisterSearchableObject(SearchableObject searchableObject)
    {
        if (searchableObject != null && !searchableObjects.Contains(searchableObject))
        {
            searchableObjects.Add(searchableObject);
        }
    }

    public void UnregisterWeapon(ItemObject weapon)
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

    public void UnregisterSearchableObject(SearchableObject searchableObject)
    {
        if (searchableObject == null)
        {
            return;
        }

        if (searchableReservedBy.TryGetValue(searchableObject, out NPCBase reserver))
        {
            searchableReservedBy.Remove(searchableObject);
            searchableByNPC.Remove(reserver);
        }

        searchableObjects.Remove(searchableObject);
    }

    public bool TryReserveWeapon(NPCBase npc, ItemObject weapon)
    {
        if (npc == null || weapon == null || weapon.IsPickedUp || !sceneWeapons.Contains(weapon))
        {
            return false;
        }

        if (reservedBy.TryGetValue(weapon, out NPCBase currentReserver))
        {
            return currentReserver == npc;
        }

        if (weaponByNPC.TryGetValue(npc, out ItemObject currentWeapon))
        {
            return currentWeapon == weapon;
        }

        reservedBy.Add(weapon, npc);
        weaponByNPC.Add(npc, weapon);
        return true;
    }

    public bool TryReserveNearestWeapon(NPCBase npc, out ItemObject weapon)
    {
        weapon = null;
        if (npc == null)
        {
            return false;
        }

        float nearestDistance = float.MaxValue;

        foreach (ItemObject candidate in sceneWeapons)
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

    public bool TryReserveRandomWeapon(NPCBase npc, out ItemObject weapon)
    {
        weapon = null;
        if (npc == null)
        {
            return false;
        }

        List<ItemObject> availableWeapons = new List<ItemObject>();
        foreach (ItemObject candidate in sceneWeapons)
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

    public bool TryReserveRandomSearchableObjectWithWeapon(NPCBase npc, out SearchableObject searchableObject)
    {
        searchableObject = null;
        if (npc == null)
        {
            return false;
        }

        List<SearchableObject> availableSearchables = new List<SearchableObject>();
        foreach (SearchableObject candidate in searchableObjects)
        {
            if (IsSearchableAvailable(candidate))
            {
                availableSearchables.Add(candidate);
            }
        }

        if (availableSearchables.Count == 0)
        {
            return false;
        }

        searchableObject = availableSearchables[Random.Range(0, availableSearchables.Count)];
        return TryReserveSearchableObject(npc, searchableObject);
    }

    public bool TryReserveSearchableObject(NPCBase npc, SearchableObject searchableObject)
    {
        if (npc == null || searchableObject == null || !searchableObjects.Contains(searchableObject) || !searchableObject.HasWeaponItems())
        {
            return false;
        }

        if (searchableReservedBy.TryGetValue(searchableObject, out NPCBase currentReserver))
        {
            return currentReserver == npc;
        }

        if (searchableByNPC.TryGetValue(npc, out SearchableObject currentSearchable))
        {
            return currentSearchable == searchableObject;
        }

        searchableReservedBy.Add(searchableObject, npc);
        searchableByNPC.Add(npc, searchableObject);
        return true;
    }

    public void ReleaseReservation(NPCBase npc, ItemObject weapon)
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

    public void ReleaseSearchableReservation(NPCBase npc, SearchableObject searchableObject)
    {
        if (npc == null || searchableObject == null)
        {
            return;
        }

        if (searchableReservedBy.TryGetValue(searchableObject, out NPCBase reserver) && reserver == npc)
        {
            searchableReservedBy.Remove(searchableObject);
            searchableByNPC.Remove(npc);
        }
    }

    public void ReleaseAllReservations(NPCBase npc)
    {
        if (npc == null)
        {
            return;
        }

        if (weaponByNPC.TryGetValue(npc, out ItemObject weapon))
        {
            weaponByNPC.Remove(npc);
            reservedBy.Remove(weapon);
        }

        if (searchableByNPC.TryGetValue(npc, out SearchableObject searchableObject))
        {
            searchableByNPC.Remove(npc);
            searchableReservedBy.Remove(searchableObject);
        }
    }

    private bool IsAvailable(ItemObject weapon)
    {
        return weapon != null
            && !weapon.IsPickedUp
            && !reservedBy.ContainsKey(weapon);
    }

    private bool IsSearchableAvailable(SearchableObject searchableObject)
    {
        return searchableObject != null
            && searchableObject.HasWeaponItems()
            && !searchableReservedBy.ContainsKey(searchableObject);
    }

    private void RemoveInvalidReservations()
    {
        List<ItemObject> invalidWeapons = new List<ItemObject>();
        foreach (KeyValuePair<ItemObject, NPCBase> reservation in reservedBy)
        {
            if (reservation.Key == null || reservation.Value == null)
            {
                invalidWeapons.Add(reservation.Key);
            }
        }

        foreach (ItemObject weapon in invalidWeapons)
        {
            if (weapon != null && reservedBy.TryGetValue(weapon, out NPCBase npc))
            {
                weaponByNPC.Remove(npc);
            }

            reservedBy.Remove(weapon);
        }

        List<SearchableObject> invalidSearchables = new List<SearchableObject>();
        foreach (KeyValuePair<SearchableObject, NPCBase> reservation in searchableReservedBy)
        {
            if (reservation.Key == null || reservation.Value == null || !reservation.Key.HasWeaponItems())
            {
                invalidSearchables.Add(reservation.Key);
            }
        }

        foreach (SearchableObject searchableObject in invalidSearchables)
        {
            if (searchableObject != null && searchableReservedBy.TryGetValue(searchableObject, out NPCBase npc))
            {
                searchableByNPC.Remove(npc);
            }

            searchableReservedBy.Remove(searchableObject);
        }
    }
}