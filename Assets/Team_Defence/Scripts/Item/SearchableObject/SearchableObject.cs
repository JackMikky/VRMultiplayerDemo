using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class SearchableObject : MonoBehaviour
{
    [SerializeField] private List<ItemObject> itemPrefabs;

    public int InstantiatableCount = 3;

    [SerializeField] private List<ItemObject> searchableItems;
    [SerializeField] private bool allowDuplicateWeaponPrefabs;
    [SerializeField] private bool limitOneWeaponPerCategory;

    private void Awake()
    {
        if (searchableItems == null)
        {
            searchableItems = new List<ItemObject>();
        }
    }

    private void Start()
    {
        if (itemPrefabs == null || itemPrefabs.Count == 0)
        {
            SceneItemManager.Instance?.RegisterSearchableObject(this);
            return;
        }

        HashSet<ItemObject> generatedWeaponPrefabs = new HashSet<ItemObject>();
        HashSet<EquipmentCategory> generatedWeaponCategories = new HashSet<EquipmentCategory>();

        for (int i = 0; i < InstantiatableCount; i++)
        {
            List<ItemObject> candidates = itemPrefabs
                .Where(prefab => prefab != null
                    && IsGenerationCandidateAllowed(prefab, generatedWeaponPrefabs, generatedWeaponCategories))
                .ToList();

            if (candidates.Count == 0)
            {
                break;
            }

            ItemObject selectedPrefab = candidates[Random.Range(0, candidates.Count)];
            var item = GameObject.Instantiate(selectedPrefab, transform.position, Quaternion.identity, transform);
            AddItem(item);
            item.SetEnabled(false); // Deactivate the item initially

            if (IsWeaponCategory(selectedPrefab.ItemCategory))
            {
                generatedWeaponPrefabs.Add(selectedPrefab);
                generatedWeaponCategories.Add(selectedPrefab.ItemCategory);
            }
        }

        SceneItemManager.Instance?.RegisterSearchableObject(this);
    }

    private void OnDestroy()
    {
        SceneItemManager.Instance?.UnregisterSearchableObject(this);
    }

    public ItemObject GetItemRandomly()
    {
        if (searchableItems == null || searchableItems.Count == 0)
        {
            Debug.LogWarning("No items available in the searchable object.");
            return null;
        }
        int randomIndex = Random.Range(0, searchableItems.Count);
        ItemObject selectedItem = searchableItems[randomIndex];
        if (selectedItem != null)
        {
            RemoveItem(selectedItem);
            return selectedItem;
        }
        else
        {
            Debug.LogWarning("Selected item is null.");
            return null;
        }
    }

    public void PlantTimedBomb(TimedBomb bomb, Transform user, ItemObject bombItem)
    {
        if (bomb == null) return;

        bomb.Plant(this, user, bombItem);
    }

    public void AddItem(ItemObject item)
    {
        if (searchableItems == null)
        {
            searchableItems = new List<ItemObject>();
        }

        if (item != null && !searchableItems.Contains(item))
        {
            // Set the item as a child of this SearchableObject
            item.SetEnabled(false);
            item.transform.SetParent(transform);
            item.transform.localPosition = Vector3.zero;
            item.transform.localRotation = Quaternion.identity;

            searchableItems.Add(item);
        }
    }

    public void RemoveItem(ItemObject item)
    {
        if (searchableItems != null && item != null && searchableItems.Contains(item))
        {
            searchableItems.Remove(item);
        }
    }

    public bool HasWeaponItems()
    {
        if (searchableItems == null)
        {
            return false;
        }

        return searchableItems.Any(item => item != null && (item.ItemCategory == EquipmentCategory.Bomb || item.ItemCategory == EquipmentCategory.Gun || item.ItemCategory == EquipmentCategory.Melee));
    }

    public IReadOnlyList<ItemObject> GetAllItems()
    {
        if (searchableItems == null)
        {
            Debug.LogWarning("Searchable items list is null.");
            return new List<ItemObject>();
        }
        var items = searchableItems;
        searchableItems = new List<ItemObject>();
        return items;
    }

    public ItemObject GetItemByType(EquipmentCategory category)
    {
        foreach (var item in searchableItems)
        {
            if (item != null && item.ItemCategory == category)
            {
                RemoveItem(item);
                return item;
            }
        }
        Debug.LogWarning($"No item found of category: {category}");
        return null;
    }

    public List<ItemObject> GetItemsByType(EquipmentCategory category)
    {
        var items = searchableItems.Where(item => item != null && item.ItemCategory == category).ToList();
        foreach (var item in items)
        {
            RemoveItem(item);
        }
        return items;
    }

    public ItemObject GetNormalItem()
    {
        return GetItemByType(EquipmentCategory.Normal);
    }

    public ItemObject GetBestWeapon()
    {
        var item = searchableItems
            .Where(item => item != null && IsWeaponCategory(item.ItemCategory))
            .OrderByDescending(item => GetWeaponScore(item.ItemCategory))
            .FirstOrDefault();
        RemoveItem(item);
        return item;
    }

    public IReadOnlyList<ItemObject> GetAvailableWeapons()
    {
        return searchableItems
            .Where(item => item != null && IsWeaponCategory(item.ItemCategory))
            .ToList();
    }

    public ItemObject GetBombItem()
    {
        return GetItemByType(EquipmentCategory.Bomb);
    }

    public ItemObject GetMeleeItem()
    {
        return GetItemByType(EquipmentCategory.Melee);
    }

    public ItemObject GetGunItem()
    {
        return GetItemByType(EquipmentCategory.Gun);
    }

    private int GetWeaponScore(EquipmentCategory type)
    {
        switch (type)
        {
            case EquipmentCategory.Bomb:
                return 100;

            case EquipmentCategory.Gun:
                return 80;

            case EquipmentCategory.Melee:
                return 50;

            default:
                return 0;
        }
    }

    private bool IsWeaponCategory(EquipmentCategory category)
    {
        return category == EquipmentCategory.Bomb
            || category == EquipmentCategory.Gun
            || category == EquipmentCategory.Melee;
    }

    private bool IsGenerationCandidateAllowed(ItemObject prefab, HashSet<ItemObject> generatedWeaponPrefabs, HashSet<EquipmentCategory> generatedWeaponCategories)
    {
        if (!IsWeaponCategory(prefab.ItemCategory))
        {
            return true;
        }

        if (allowDuplicateWeaponPrefabs)
        {
            return true;
        }

        if (limitOneWeaponPerCategory)
        {
            return !generatedWeaponCategories.Contains(prefab.ItemCategory);
        }

        return !generatedWeaponPrefabs.Contains(prefab);
    }
}