using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER

using UnityEngine.InputSystem;

#endif

public class NPCManager : MonoBehaviour
{
    public static NPCManager Instance { get; private set; }

    private const float PanicMaxReactionDelay = 0.6f;
    private const float PanicRandomDelay = 0.3f;

    [Header("Prefabs Settings")]
    [Tooltip("A civilian prefab with the CitizenNPC script attached.")]
    public GameObject citizenPrefab;

    [Tooltip("Assassin prefab with the AssassinNPC script attached")]
    public GameObject assassinPrefab;

    [Header("Game Settings")]
    public Transform vipTransform;

    [Header("NPC Leave Point")]
    [Tooltip("Obsolete. Set the exit point on VenueManager instead.")]
    [SerializeField, HideInInspector] private Transform npcLeavePoint;

    public Transform NPCLeavePoint => VenueManager.Instance != null ? VenueManager.Instance.ExitPoint : null;

    public int npcCount = 10;

    public bool enableClickToSpawnSuspect = true;
    public bool enableClickToInteractSuspect = true;
    public LayerMask groundLayers = ~0;
    public float maxSampleDistance = 1.5f;

    private NPCBase cachedSuspect;

    [Header("NPC Lists")]
    [SerializeField] private List<NPCBase> citizenNPCs = new List<NPCBase>();

    [SerializeField] private List<NPCBase> assassinNPCs = new List<NPCBase>();
    [SerializeField] private List<NPCBase> policeNPCs = new List<NPCBase>();
    [SerializeField] private List<NPCBase> vipNPCs = new List<NPCBase>();

    [Header("Debug Settings")]
    [SerializeField] private bool useMouseClickToSpawnSuspect = true;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        AreaZone assassinZone = VenueManager.Instance != null ? VenueManager.Instance.AssassinSpawnZone : null;
        if (assassinZone != null)
        {
            Vector3 spawnPosition = assassinZone.TryGetRandomNavMeshPoint(out Vector3 navPoint) ? navPoint : assassinZone.Center;
            SpawnSuspectAt(spawnPosition);
        }
    }

    private void Update()
    {
        if (!GameManager.Instance.IsGameRunning) return;
        SpawnSuspectByClick();
    }

    private void SpawnSuspectByClick()
    {
        if (!useMouseClickToSpawnSuspect) return;
        if (!Application.isPlaying) return;

        if (GameManager.Instance == null || !GameManager.Instance.IsGameRunning) return;

        bool click = false;
        Vector2 screenPos = default;

#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            click = true;
            screenPos = Mouse.current.position.ReadValue();
        }
#else
        if ((enableClickToSpawnSuspect || enableClickToInteractSuspect) && Input.GetMouseButtonDown(0))
        {
            click = true;
            screenPos = Input.mousePosition;
        }
#endif

        if (click)
        {
            Camera cam = Camera.main;
            if (cam == null) return;

            Ray ray = cam.ScreenPointToRay(screenPos);

            if (enableClickToInteractSuspect)
            {
                if (Physics.Raycast(ray, out RaycastHit npcHit, 100f))
                {
                    NPCBase hitNpc = npcHit.collider.GetComponentInParent<NPCBase>();
                    if (hitNpc != null)
                    {
                        hitNpc.OnInteracted();
                        Debug.Log($"[Interaction] Clicked on an NPC; is this person a suspect?: {hitNpc.NpcType.Equals(NPCType.Assassin)}");
                        return;
                    }
                }
            }

            if (enableClickToSpawnSuspect)
            {
                if (Physics.Raycast(ray, out RaycastHit hit, 100f, groundLayers))
                {
                    Vector3 clickPoint = hit.point;
                    if (NavMesh.SamplePosition(clickPoint, out NavMeshHit navHit, maxSampleDistance, NavMesh.AllAreas))
                    {
                        SpawnSuspectAt(navHit.position);
                    }
                }
            }
        }
    }

    public void SpawnSuspectAt(Vector3 position)
    {
        if (assassinPrefab == null || vipTransform == null) return;

        if (cachedSuspect != null)
        {
            Debug.Log($"[Cleanup] Removed the previously manually generated suspect: {cachedSuspect.name}");
            Destroy(cachedSuspect.gameObject);
        }

        GameObject npcGo = Instantiate(assassinPrefab, position, Quaternion.identity);
        NPCBase npcScript = npcGo.GetComponent<NPCBase>();
        if (npcScript == null) return;

        if (npcScript is AssassinNPC assassin)
            assassin.SetNavMeshTarget(vipTransform);

        npcScript.Initialize();
        cachedSuspect = npcScript;

        Debug.Log($"[Manually Generated] New Assassin successfully created, location: {position}");
    }

    public void CatchSuspectInEditor()
    {
        if (cachedSuspect != null)
        {
            cachedSuspect.OnInteracted();
        }
        else
        {
            Debug.LogWarning("The suspect has not yet been generated in the scene, or the game is not running!");
        }
    }

    public void RegisterCitizen(NPCBase npc)
    {
        if (npc != null && !citizenNPCs.Contains(npc))
        {
            citizenNPCs.Add(npc);
        }
    }

    public void UnregisterCitizen(NPCBase npc)
    {
        if (npc != null)
        {
            citizenNPCs.Remove(npc);
        }
    }

    public void RegisterAssassin(NPCBase npc)
    {
        if (npc != null && !assassinNPCs.Contains(npc))
        {
            assassinNPCs.Add(npc);
            cachedSuspect = npc;
        }
    }

    public void UnregisterAssassin(NPCBase npc, bool wasCaptured = true)
    {
        if (npc != null && assassinNPCs.Contains(npc))
        {
            assassinNPCs.Remove(npc);
            if (wasCaptured && assassinNPCs.Count == 0)
            {
                Debug.Log("[System] All Assassin NPCs have been removed from the scene.");
                GameManager.Instance.PlayerWin();
            }
        }
    }

    public void RegisterPolice(NPCBase npc)
    {
        if (npc != null && !policeNPCs.Contains(npc))
        {
            policeNPCs.Add(npc);
        }
    }

    /// <summary>
    /// Makes every registered citizen within radius panic. Each citizen reacts after a small random delay scaled by distance.
    /// </summary>
    public void BroadcastPanic(Vector3 position, float radius)
    {
        float radiusSqr = radius * radius;

        for (int i = citizenNPCs.Count - 1; i >= 0; i--)
        {
            if (!(citizenNPCs[i] is CitizenNPC citizen) || citizen == null)
            {
                citizenNPCs.RemoveAt(i);
                continue;
            }

            float distanceSqr = (citizen.transform.position - position).sqrMagnitude;
            if (distanceSqr > radiusSqr) continue;

            float delay = Mathf.Sqrt(distanceSqr) / Mathf.Max(0.01f, radius) * PanicMaxReactionDelay
                + Random.Range(0f, PanicRandomDelay);
            citizen.TriggerPanic(delay);
        }
    }

    public void RegisterVIP(NPCBase npc)
    {
        if (npc != null && !vipNPCs.Contains(npc))
        {
            vipNPCs.Add(npc);
        }
    }
}