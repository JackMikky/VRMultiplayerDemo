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

    [Tooltip("Where passersby wander around after watching.")]
    [SerializeField] private AreaZone roamingZone;

    [Header("Points")]
    [Tooltip("Destination passersby walk to before being removed.")]
    [SerializeField] private Transform exitPoint;

    public AreaZone GatheringZone => gatheringZone;
    public AreaZone RoamingZone => roamingZone;
    public Transform ExitPoint => exitPoint;

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

    public void SetGatheringZone(AreaZone zone) => gatheringZone = zone;

    public void SetRoamingZone(AreaZone zone) => roamingZone = zone;

    public void SetExitPoint(Transform point) => exitPoint = point;
}
