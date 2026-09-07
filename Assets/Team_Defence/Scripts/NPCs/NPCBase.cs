using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;

#if UNITY_EDITOR

using UnityEditor;

#endif

public enum NPCType
{
    Assassin,
    Citizen,
    Police,
    VIP
}

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(NavMeshObstacle))]
public abstract class NPCBase : MonoBehaviour, IDamageable
{
    protected NPCType npcType = NPCType.Citizen;
    [HideInInspector] public NPCType NpcType => npcType;

    protected StateMachine StateMachine { get; private set; }

    [Header("NavMesh Settings")]
    protected Transform target { get; private set; }

    public Transform Target => target;

    protected NavMeshAgent agent;
    public NavMeshAgent Agent => agent;

    protected NavMeshObstacle obstacle;
    public NavMeshObstacle Obstacle => obstacle;

    [Space(10)]
    protected Renderer myRenderer;

    protected Animator anim;
    public Animator Anim => anim;

    [HideInInspector] public float nextIdleActionTime;
    [HideInInspector] public int[] cachedIdleAnimationHashes;
    [HideInInspector] public float currentWaypointStayDuration;

    [Space(10)]
    [Header("Visual Feedback")]
    [SerializeField] private GameObject mark;

    [Space(10)]
    [Header("Equipment")]
    [SerializeField] protected WeaponObject currentEquipment;

    public WeaponObject CurrentEquipment => currentEquipment;

    [Space(10)]
    [Header("Health Settings")]
    [SerializeField] protected float maxHealth = 100f;

    [Space(10)]
    [Header("Events")]
    public UnityEvent onDead;

    public UnityEvent onDamaged;

    protected float currentHealth;

    public float MaxHealth => maxHealth;
    public float CurrentHealth => currentHealth;
    public bool IsDead { get; private set; }

    #region Debug Settings

    [Space(10)]
    [Header("Debug Settings")]
    [Tooltip("Show current state, target and health info above the NPC in the Scene view")]
    [SerializeField] private bool showDebugStateLabel = false;

    [Tooltip("Vertical offset for the debug label above the NPC's head")]
    [SerializeField] private float debugLabelHeightOffset = 2.2f;

    #endregion Debug Settings

    /// <summary>
    /// Exposes the current state's type name for debugging/inspection purposes
    /// </summary>
    public string CurrentStateName => StateMachine?.CurrentState != null
        ? StateMachine.CurrentState.GetType().Name
        : "None";

    protected virtual void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        obstacle = GetComponent<NavMeshObstacle>();
        myRenderer = GetComponentInChildren<Renderer>();
        anim = GetComponentInChildren<Animator>();
        StateMachine = new StateMachine();

        currentHealth = maxHealth;
    }

    private void Start()
    {
        Initialize();
    }

    private void Update()
    {
        StateMachine.Update();

        OnUpdate();
    }

    protected virtual void OnUpdate()
    {
    }

    public void LookAtPlayer(Transform player)
    {
        if (player == null) return;

        Vector3 direction = player.position - transform.position;
        direction.y = 0;

        if (direction != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 5f);
        }
    }

    public virtual void Initialize()
    {
        OnSetupBehavior();
    }

    public void SetNavMeshTarget(Transform target)
    {
        this.target = target;
    }

    public void SetNavigationMode(bool useAgent)
    {
        if (useAgent)
        {
            if (obstacle != null) obstacle.enabled = false;
            if (agent != null) agent.enabled = true;
        }
        else
        {
            if (agent != null) agent.enabled = false;
            if (obstacle != null) obstacle.enabled = true;
        }
    }

    /// <summary>
    /// Safely reset all base locomotion animation state flags.
    /// Prevent blending errors where walking or idle animations play while running
    /// </summary>
    public void ResetMovementAnimationFlags()
    {
        if (anim == null) return;

        anim.SetBool(AnimationConstants.IsWalking, false);
        anim.SetBool(AnimationConstants.IsRunning, false);
        anim.SetBool(AnimationConstants.IsIdling, false);
    }

    /// <summary>
    /// Safely modify the movement speed and halt state of pathfinding entities
    /// Includes built-in safety checks for component activation status and mesh baking bounds
    /// </summary>
    public void SetAgentVelocity(float speed, bool isStopped)
    {
        if (agent != null && agent.enabled && agent.isOnNavMesh)
        {
            agent.speed = speed;
            agent.isStopped = isStopped;
        }
    }

    // ==========================================

    protected abstract void OnSetupBehavior();

    public virtual void OnInteracted()
    {
    }

    public void ShowMark(bool show)
    {
        if (mark != null) mark.SetActive(show);
    }

    public virtual void TakeDamage(float amount, GameObject attacker)
    {
        if (IsDead) return;

        currentHealth = Mathf.Max(0f, currentHealth - amount);
        onDamaged?.Invoke();

        if (currentHealth <= 0f)
        {
            Die(attacker);
        }
    }

    protected virtual void Die(GameObject attacker)
    {
        IsDead = true;
        onDead?.Invoke();
        Debug.Log($"[{name}] has died, killed by {attacker?.name ?? "Unknown"}");

        SetAgentVelocity(0f, isStopped: true);
    }

#if UNITY_EDITOR

    private void OnDrawGizmos()
    {
        if (!showDebugStateLabel) return;

        Vector3 labelPosition = transform.position + Vector3.up * debugLabelHeightOffset;

        string stateName = Application.isPlaying ? CurrentStateName : "N/A";
        string targetName = target != null ? target.name : "None";
        string healthInfo = Application.isPlaying ? $"{currentHealth:0}/{maxHealth:0}" : $"{maxHealth:0}/{maxHealth:0}";

        string label = $"[{npcType}] {name}\nState: {stateName}\nTarget: {targetName}\nHP: {healthInfo}";

        DrawDebugLabelWithBackground(labelPosition, label, IsDead ? Color.red : Color.white);
    }

    private void DrawDebugLabelWithBackground(Vector3 worldPosition, string text, Color textColor)
    {
        Handles.zTest = UnityEngine.Rendering.CompareFunction.Always;

        GUIStyle style = new GUIStyle(EditorStyles.boldLabel)
        {
            alignment = TextAnchor.MiddleCenter,
            fontStyle = FontStyle.Bold,
            normal = { textColor = textColor },
            fontSize = 12
        };

        Handles.BeginGUI();

        Vector2 screenPoint = HandleUtility.WorldToGUIPoint(worldPosition);
        Vector2 size = style.CalcSize(new GUIContent(text));
        Rect backgroundRect = new Rect(screenPoint.x - size.x / 2f - 4f, screenPoint.y - size.y / 2f - 2f, size.x + 8f, size.y + 4f);

        Color previousColor = GUI.color;
        GUI.color = new Color(0f, 0f, 0f, 0.55f);
        GUI.DrawTexture(backgroundRect, EditorGUIUtility.whiteTexture);
        GUI.color = previousColor;

        GUI.Label(backgroundRect, text, style);

        Handles.EndGUI();

        Handles.zTest = UnityEngine.Rendering.CompareFunction.LessEqual;
    }

#endif
}