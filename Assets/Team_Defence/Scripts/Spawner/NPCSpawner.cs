using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class NPCSpawner : MonoBehaviour
{
    [Header("NPC Prefabs")]
    [SerializeField] private GameObject citizenPrefab;

    [SerializeField] private GameObject assassinPrefab;

    [Header("Target Settings")]
    [SerializeField] private Transform vipTarget;
    [SerializeField, Min(0f)] private float maxSampleDistance = 1.5f;

    [Header("Spawn Settings")]
    [SerializeField, Min(0.1f)] private float minSpawnInterval = 5f;

    [SerializeField, Min(0.1f)] private float maxSpawnInterval = 10f;
    [SerializeField, Min(1)] private int maxNpcCount = 30;
    [SerializeField, Range(0f, 100f)] private float assassinSpawnPercentage = 10f;
    private float spawnTimer;
    private float nextSpawnInterval;
    private readonly List<GameObject> activeNpcs = new List<GameObject>();
    private readonly Queue<CitizenNPC> citizens = new Queue<CitizenNPC>();

    private void Start()
    {
        ScheduleNextSpawn();
    }

    private void Update()
    {
        spawnTimer += Time.deltaTime;
        if (spawnTimer < nextSpawnInterval) return;

        spawnTimer = 0f;
        ScheduleNextSpawn();
        SpawnNpc();
    }

    private void SpawnNpc()
    {
        activeNpcs.RemoveAll(npc => npc == null);
        if (activeNpcs.Count >= maxNpcCount && !TryRemoveOldestCitizen()) return;

        AreaZone spawnZone = VenueManager.Instance != null ? VenueManager.Instance.CitizenSpawnZone : null;
        if (spawnZone == null) return;

        bool spawnAssassin = Random.Range(0f, 100f) < assassinSpawnPercentage;
        GameObject prefab = spawnAssassin ? assassinPrefab : citizenPrefab;
        if (prefab == null) return;

        Vector3 spawnPosition = spawnZone.TryGetRandomNavMeshPoint(out Vector3 navPoint) ? navPoint : spawnZone.GetRandomPoint();
        GameObject spawnedNpc = Instantiate(prefab, spawnPosition, Quaternion.identity);
        activeNpcs.Add(spawnedNpc);
        ConfigureSpawnedNpc(spawnedNpc);
    }

    private void ConfigureSpawnedNpc(GameObject spawnedNpc)
    {
        if (spawnedNpc == null) return;

        bool isCitizen = spawnedNpc.TryGetComponent(out CitizenNPC citizen);
        if (isCitizen) citizens.Enqueue(citizen);

        if (isCitizen)
        {
            AreaZone gatheringZone = VenueManager.Instance != null ? VenueManager.Instance.GatheringZone : null;
            if (gatheringZone != null && gatheringZone.TryGetRandomNavMeshPoint(out Vector3 gatheringPosition))
            {
                citizen.ConfigureAudienceGathering(gatheringPosition, vipTarget);
            }
        }
        else if (spawnedNpc.TryGetComponent(out AssassinNPC assassin))
        {
            assassin.SetNavMeshTarget(vipTarget);
        }
    }

    private bool TryRemoveOldestCitizen()
    {
        while (citizens.Count > 0)
        {
            CitizenNPC citizen = citizens.Dequeue();
            if (citizen == null) continue;

            activeNpcs.Remove(citizen.gameObject);
            StartCoroutine(ExitAndDestroy(citizen));
            return true;
        }

        return false;
    }

    private IEnumerator ExitAndDestroy(CitizenNPC citizen)
    {
        if (citizen.Agent == null)
        {
            DestroyCitizen(citizen);
            yield break;
        }

        Transform exitPoint = NPCManager.Instance != null ? NPCManager.Instance.NPCLeavePoint : null;
        if (exitPoint == null
            || !NavMesh.SamplePosition(exitPoint.position, out NavMeshHit hit, maxSampleDistance, NavMesh.AllAreas))
        {
            DestroyCitizen(citizen);
            yield break;
        }

        citizen.enabled = false;
        citizen.SetNavigationMode(useAgent: true);
        citizen.SetAgentVelocity(citizen.Agent.speed, isStopped: false);
        citizen.ResetMovementAnimationFlags();
        if (citizen.Anim != null) citizen.Anim.SetBool(AnimationConstants.IsWalking, true);
        if (!citizen.Agent.SetDestination(hit.position))
        {
            DestroyCitizen(citizen);
            yield break;
        }

        while (citizen != null
            && citizen.Agent.enabled
            && citizen.Agent.pathStatus != NavMeshPathStatus.PathInvalid
            && (citizen.Agent.pathPending
                || citizen.Agent.remainingDistance > citizen.Agent.stoppingDistance))
        {
            yield return null;
        }

        DestroyCitizen(citizen);
    }

    private void DestroyCitizen(CitizenNPC citizen)
    {
        if (citizen == null) return;

        if (NPCManager.Instance != null) NPCManager.Instance.UnregisterCitizen(citizen);
        Destroy(citizen.gameObject);
    }

    private void ScheduleNextSpawn()
    {
        float minInterval = Mathf.Min(minSpawnInterval, maxSpawnInterval);
        float maxInterval = Mathf.Max(minSpawnInterval, maxSpawnInterval);
        nextSpawnInterval = Random.Range(minInterval, maxInterval);
    }
}