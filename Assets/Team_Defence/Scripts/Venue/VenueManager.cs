using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Holds references to the venue's areas and points. Assign everything from the Inspector or from other scripts.
/// </summary>
public class VenueManager : MonoBehaviour
{
    public static VenueManager Instance { get; private set; }

    [Header("Areas")]
    [Tooltip("Where audience and passersby gather to watch the speech.")]
    [SerializeField] private AreaZone gatheringZone;

    [Tooltip("Where passersby wander around after watching. Several zones can be combined; larger zones are picked more often.")]
    [SerializeField] private List<AreaZone> roamingZones = new List<AreaZone>();

    [Header("Points")]
    [Tooltip("Destination passersby walk to before being removed.")]
    [SerializeField] private Transform exitPoint;

    [Header("Spawn Areas")]
    [Tooltip("Where NPCSpawner spawns citizens.")]
    [SerializeField] private AreaZone citizenSpawnZone;

    [Tooltip("Where the initial assassin is spawned.")]
    [SerializeField] private AreaZone assassinSpawnZone;

    public AreaZone CitizenSpawnZone => citizenSpawnZone;
    public AreaZone AssassinSpawnZone => assassinSpawnZone;
    public AreaZone GatheringZone => gatheringZone;
    public IReadOnlyList<AreaZone> RoamingZones => roamingZones;
    public Transform ExitPoint => exitPoint;

    private readonly List<AreaZone> candidates = new List<AreaZone>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    public void SetRoamingZones(IEnumerable<AreaZone> zones)
    {
        roamingZones.Clear();
        if (zones != null) roamingZones.AddRange(zones);
    }

    public void AddRoamingZone(AreaZone zone)
    {
        if (zone != null && !roamingZones.Contains(zone)) roamingZones.Add(zone);
    }

    public void RemoveRoamingZone(AreaZone zone) => roamingZones.Remove(zone);

    /// <summary>Picks a roaming zone weighted by area x weight and returns a NavMesh point inside it. Falls back to the other zones if one fails.</summary>
    public bool TryGetRandomRoamingPoint(out Vector3 point)
    {
        candidates.Clear();
        foreach (AreaZone zone in roamingZones)
        {
            if (zone != null && zone.isActiveAndEnabled && zone.Weight > 0f) candidates.Add(zone);
        }

        while (candidates.Count > 0)
        {
            int index = PickWeightedIndex(candidates);
            if (candidates[index].TryGetRandomNavMeshPoint(out point)) return true;
            candidates.RemoveAt(index);
        }

        point = default;
        return false;
    }

    private static int PickWeightedIndex(List<AreaZone> zones)
    {
        float total = 0f;
        foreach (AreaZone zone in zones) total += zone.SelectionWeight;

        float roll = Random.value * total;
        for (int i = 0; i < zones.Count; i++)
        {
            roll -= zones[i].SelectionWeight;
            if (roll <= 0f) return i;
        }

        return zones.Count - 1;
    }

    public void SetGatheringZone(AreaZone zone) => gatheringZone = zone;

    public void SetExitPoint(Transform point) => exitPoint = point;
}
