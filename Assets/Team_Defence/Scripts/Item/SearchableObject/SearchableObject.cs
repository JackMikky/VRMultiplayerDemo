using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class SearchableObject : MonoBehaviour
{
    [SerializeField] private List<ItemObject> itemPrefabs;

    public int InstantiatableCount = 3;

    [SerializeField] private List<ItemObject> searchableItems;

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

        for (int i = 0; i < InstantiatableCount; i++)
        {
            var randomIndex = Random.Range(0, itemPrefabs.Count);
            var item = GameObject.Instantiate(itemPrefabs[randomIndex], transform.position, Quaternion.identity, transform);
            AddItem(item);
            item.SetEnabled(false); // Deactivate the item initially
        }

        SceneItemManager.Instance?.RegisterSearchableObject(this);

        //TODO: Remove this after testing, this is just for testing the bomb planting functionality
        var bombItem = searchableItems.Find(item => item != null && item.ItemCategory == EquipmentCategory.Bomb);
        PlantTimedBomb(bombItem?.Equipment as TimedBomb, this.transform, bombItem);
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

    public void AddItem(ItemObject item)
    {
        if (searchableItems == null)
        {
            searchableItems = new List<ItemObject>();
        }

        if (item != null && !searchableItems.Contains(item))
        {
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
            .Where(item => item != null && (item.ItemCategory == EquipmentCategory.Bomb || item.ItemCategory == EquipmentCategory.Gun || item.ItemCategory == EquipmentCategory.Melee))
            .OrderByDescending(item => GetWeaponScore(item.ItemCategory))
            .FirstOrDefault();
        RemoveItem(item);
        return item;
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
}