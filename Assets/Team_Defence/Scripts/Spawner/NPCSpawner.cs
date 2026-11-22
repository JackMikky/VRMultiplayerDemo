using System.Collections.Generic;
using UnityEngine;

public class NPCSpawner : MonoBehaviour
{
    [Header("NPC Prefabs")]
    [SerializeField] private GameObject citizenPrefab;

    [SerializeField] private GameObject assassinPrefab;

    [Header("Spawn Settings")]
    [SerializeField, Min(0.1f)] private float minSpawnInterval = 5f;

    [SerializeField, Min(0.1f)] private float maxSpawnInterval = 10f;
    [SerializeField, Min(1)] private int maxNpcCount = 30;
    [SerializeField, Range(0f, 100f)] private float assassinSpawnPercentage = 10f;
    private float nextSpawnTime;
    private readonly Queue<CitizenNPC> citizens = new Queue<CitizenNPC>();

    private void Start()
    {
        ScheduleNextSpawn();
    }

    private void Update()
    {
        if (Time.time < nextSpawnTime) return;

        ScheduleNextSpawn();
        SpawnNpc();
    }

    private void SpawnNpc()
    {
        VenueManager venue = VenueManager.Instance;
        AreaZone spawnZone = venue != null ? venue.CitizenSpawnZone : null;
        if (spawnZone == null) return;

        if (IsFull() && !TryRemoveOldestCitizen()) return;

        bool spawnAssassin = Random.Range(0f, 100f) < assassinSpawnPercentage;
        GameObject prefab = spawnAssassin ? assassinPrefab : citizenPrefab;
        if (prefab == null) return;

        Transform vipTarget = NPCManager.Instance != null ? NPCManager.Instance.vipTransform : null;

        Vector3 position = spawnZone.TryGetRandomNavMeshPoint(out Vector3 navPoint) ? navPoint : spawnZone.GetRandomPoint();
        GameObject spawned = Instantiate(prefab, position, Quaternion.identity);

        if (spawned.TryGetComponent(out CitizenNPC citizen))
        {
            citizens.Enqueue(citizen);

            AreaZone gatheringZone = venue.GatheringZone;
            if (gatheringZone != null && gatheringZone.TryGetRandomNavMeshPoint(out Vector3 gatheringPosition))
            {
                citizen.ConfigureAudienceGathering(gatheringPosition, vipTarget);
            }
        }
        else if (spawned.TryGetComponent(out AssassinNPC assassin))
        {
            assassin.SetNavMeshTarget(vipTarget);
        }
    }

    private bool IsFull()
    {
        PruneCitizens();
        return citizens.Count >= maxNpcCount;
    }

    private void PruneCitizens()
    {
        while (citizens.Count > 0 && citizens.Peek() == null) citizens.Dequeue();
    }

    private bool TryRemoveOldestCitizen()
    {
        PruneCitizens();
        if (citizens.Count == 0) return false;

        citizens.Dequeue().Leave();
        return true;
    }

    private void ScheduleNextSpawn()
    {
        float min = Mathf.Min(minSpawnInterval, maxSpawnInterval);
        float max = Mathf.Max(minSpawnInterval, maxSpawnInterval);
        nextSpawnTime = Time.time + Random.Range(min, max);
    }
}