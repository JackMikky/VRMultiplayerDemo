using UnityEngine;

public class PoliceNPC : NPCBase
{
    #region StateMachine

    public PolicePatrolingState PatrolingState { get; private set; }
    public PoliceStayingState StayingState { get; private set; }

    public PoliceChasingState ChasingState { get; private set; }

    public PoliceKeepingWatchState KeepingWatchState { get; private set; }

    public PoliceInteractedState InteractedState
    { get; private set; }

    [HideInInspector] public IState previousState;
    [HideInInspector] public PoliceState previousEnumState;
    [HideInInspector] public float interactionEndTime;

    #endregion StateMachine

    [Header("Patrol Settings")]
    [Tooltip("Sequentially drag the patrol waypoints for the police officer into the scene.")]
    public Transform[] patrolWaypoints;

    [HideInInspector] public int currentWaypointIndex = 0;

    [Header("Stay Setting")]
    public float minStayDuration = 2f;

    public float maxStayDuration = 5f;

    #region Behavior Settings

    [Header("Behavior Assets")]
    [Header("Movement")]
    [SerializeField] private ScriptableMoveBehavior patrolingBehavior;

    public ScriptableMoveBehavior PatrolingBehavior => patrolingBehavior;
    [SerializeField] private ScriptableMoveBehavior chasingBehavior;
    public ScriptableMoveBehavior ChasingBehavior => chasingBehavior;

    [Header("Idle")]
    [SerializeField] private ScriptableIdleBehavior idleBehavior;

    public ScriptableIdleBehavior IdleBehavior => idleBehavior;

    [SerializeField] private ScriptableIdleBehavior keepingWatchBehavior;
    public ScriptableIdleBehavior KeepingWatchBehavior => keepingWatchBehavior;

    [Header("Interacted")]
    [SerializeField] private ScriptableInteractedBehavior interactedBehavior;

    public ScriptableInteractedBehavior InteractedBehavior => interactedBehavior;

    [HideInInspector] public ScriptableBehaviorBase activePanicBehavior;

    #endregion Behavior Settings

    [Space(10)]
    [Header("Debug")]
    [SerializeField] private PoliceState currentState = PoliceState.Staying;

    protected override void Awake()
    {
        base.Awake();
        this.npcType = NPCType.Police;

        PatrolingState = new PolicePatrolingState(this);
        StayingState = new PoliceStayingState(this, Random.Range(minStayDuration, maxStayDuration));
        KeepingWatchState = new PoliceKeepingWatchState(this);
        InteractedState = new PoliceInteractedState(this);
        ChasingState = new PoliceChasingState(this);
    }

    protected override void OnSetupBehavior()
    {
        SetNavigationMode(useAgent: true);
        ChangeToState(PatrolingState, PoliceState.Staying);
        NPCManager.Instance.RegisterPolice(this);
    }

    public void ChangeToState(IState newState, PoliceState enumState)
    {
        StateMachine.ChangeState(newState);
        currentState = enumState;
    }

    public override void OnInteracted()
    {
        base.OnInteracted();
        ChangeToState(InteractedState, PoliceState.Interacted);
    }
}