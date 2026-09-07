using UnityEngine;

public class CitizenNPC : NPCBase
{
    #region StateMachine

    public CitizenWatchingState WatchingState { get; private set; }
    public CitizenWalkingState WalkingState { get; private set; }
    public CitizenStayingState StayingState { get; private set; }

    public CitizenPanicState PanicState { get; private set; }

    public CitizenInteractedState InteractedState
    { get; private set; }

    [HideInInspector] public IState previousState;
    [HideInInspector] public CitizenState previousEnumState;
    [HideInInspector] public float interactionEndTime;

    #endregion StateMachine

    [Header("Citizen Type")]
    public CitizenType CitizenType = CitizenType.Audience;

    [Header("Stay Setting")]
    public float minStayDuration = 2f;

    public float maxStayDuration = 5f;

    #region Behavior Settings

    [Header("Behavior Assets")]
    [Header("Movement")]
    [SerializeField] private ScriptableMoveBehavior moveBehavior;

    public ScriptableMoveBehavior MoveBehavior => moveBehavior;

    [Header("Idle")]
    [SerializeField] private ScriptableIdleBehavior idleBehavior;

    public ScriptableIdleBehavior IdleBehavior => idleBehavior;

    [Header("Panic")]
    [SerializeField] private ScriptablePanicBehavior panicBehavior;

    public ScriptablePanicBehavior PanicBehavior => panicBehavior;

    [Header("Interacted")]
    [SerializeField] private ScriptableInteractedBehavior interactedBehavior;

    public ScriptableInteractedBehavior InteractedBehavior => interactedBehavior;

    [HideInInspector] public ScriptableBehaviorBase activePanicBehavior;

    #endregion Behavior Settings

    [Space(10)]
    [Header("Debug")]
    [SerializeField] private CitizenState currentState = CitizenState.Staying;

    protected override void Awake()
    {
        base.Awake();
        this.npcType = NPCType.Citizen;

        WatchingState = new CitizenWatchingState(this);
        WalkingState = new CitizenWalkingState(this);
        StayingState = new CitizenStayingState(this, 0f);
        InteractedState = new CitizenInteractedState(this);
        PanicState = new CitizenPanicState(this);
    }

    protected override void OnSetupBehavior()
    {
        switch (this.CitizenType)
        {
            case CitizenType.Audience:
                ChangeToState(WatchingState, CitizenState.Watching);
                break;

            case CitizenType.Passerby:
                ChangeToState(WalkingState, CitizenState.Wandering);
                break;

            case CitizenType.None:
            default:
                break;
        }
        NPCManager.Instance.RegisterCitizen(this);
    }

    public void ChangeToState(IState newState, CitizenState enumState)
    {
        StateMachine.ChangeState(newState);
        currentState = enumState;
    }

    public override void OnInteracted()
    {
        base.OnInteracted();

        if (StateMachine.CurrentState != InteractedState)
        {
            previousState = StateMachine.CurrentState;
            previousEnumState = currentState;
        }

        ChangeToState(InteractedState, CitizenState.Interacted);
        Debug.Log($"{this.gameObject.name}:I'm Citizen");
    }
}