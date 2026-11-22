using System.Collections;
using UnityEngine;

public class CitizenNPC : NPCBase
{
    #region StateMachine

    public CitizenWatchingState WatchingState { get; private set; }
    public CitizenWalkingState WalkingState { get; private set; }
    public CitizenStayingState StayingState { get; private set; }
    public CitizenPasserbyState PasserbyState { get; private set; }
    public CitizenLeavingState LeavingState { get; private set; }

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
    [Tooltip("Number of Staying animations selectable by StayingIndex")]
    [SerializeField, Min(1)] private int stayingAnimationCount = 1;

    public int StayingAnimationCount => stayingAnimationCount;

    [Header("Panic Setting")]
    [Tooltip("Number of Panic animations selectable by PanicIndex")]
    [SerializeField, Min(1)] private int panicAnimationCount = 1;

    public int PanicAnimationCount => panicAnimationCount;

    [Tooltip("How long a citizen stays panicked before calming down and returning to idle.")]
    [Min(0f)] public float panicDuration = 10f;

    [Header("Audience Settings")]
    [Tooltip("How long an audience member stays in Idle between two random Staying animations.")]
    [Min(0f)] public float minIdleInterval = 2f;

    [Min(0f)] public float maxIdleInterval = 6f;

    [Header("Passerby Settings")]
    [Tooltip("Total time a passerby stays at the venue before leaving.")]
    [Min(0f)] public float minVisitDuration = 40f;

    [Min(0f)] public float maxVisitDuration = 90f;

    [Tooltip("How long a passerby stands and watches the speech each time.")]
    [Min(0f)] public float minWatchDuration = 5f;

    [Min(0f)] public float maxWatchDuration = 12f;

    [Tooltip("How long a passerby pauses after reaching a roaming point.")]
    [Min(0f)] public float minPauseDuration = 2f;

    [Min(0f)] public float maxPauseDuration = 6f;

    [Tooltip("Idle time after standing still (and between animations) before the next random Staying animation starts.")]
    [Min(0.1f)] public float minReactionInterval = 4f;

    [Min(0.1f)] public float maxReactionInterval = 10f;

    [Tooltip("Chance to play another random animation after one finishes, if the stay time has not run out.")]
    [Range(0f, 1f)] public float animationRepeatChance = 0.5f;

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

    private bool isMovingToGatheringPoint;
    private Vector3 gatheringPosition;
    private Transform audienceLookTarget;
    private Coroutine panicRoutine;

    public bool IsMovingToGatheringPoint => isMovingToGatheringPoint;
    public Transform LookTarget => audienceLookTarget;

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
        StayingState = new CitizenStayingState(this);
        PasserbyState = new CitizenPasserbyState(this);
        LeavingState = new CitizenLeavingState(this);
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
                if (isMovingToGatheringPoint)
                    ChangeToState(WalkingState, CitizenState.Wandering);
                else
                    ChangeToState(PasserbyState, CitizenState.Roaming);
                break;

            case CitizenType.None:
            default:
                break;
        }
        NPCManager.Instance.RegisterCitizen(this);
    }

    protected override void OnUpdate()
    {
        if (StateMachine.CurrentState == WatchingState && audienceLookTarget != null)
        {
            LookAtPlayer(audienceLookTarget);
        }
    }

    public void ConfigureAudienceGathering(Vector3 position, Transform lookTarget)
    {
        gatheringPosition = position;
        audienceLookTarget = lookTarget;
        isMovingToGatheringPoint = true;
    }

    public void StartGatheringMovement()
    {
        SetNavigationMode(useAgent: true);
        SetAgentVelocity(Agent.speed, isStopped: false);
        ResetMovementAnimationFlags();

        if (Anim != null) Anim.SetBool(AnimationConstants.IsWalking, true);
        if (Agent != null && Agent.enabled && Agent.isOnNavMesh) Agent.SetDestination(gatheringPosition);
    }

    public void CompleteAudienceGathering()
    {
        isMovingToGatheringPoint = false;

        if (CitizenType == CitizenType.Passerby)
            ChangeToState(PasserbyState, CitizenState.Roaming);
        else
            ChangeToState(WatchingState, CitizenState.Watching);
    }

    public void ChangeToState(IState newState, CitizenState enumState)
    {
        StateMachine.ChangeState(newState);
        currentState = enumState;
    }

    public void TriggerPanic(float delay = 0f)
    {
        if (IsDead || StateMachine.CurrentState == PanicState || StateMachine.CurrentState == LeavingState) return;

        if (delay > 0f)
        {
            if (panicRoutine == null) panicRoutine = StartCoroutine(PanicAfterDelay(delay));
            return;
        }

        EnterPanic();
    }

    private IEnumerator PanicAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        panicRoutine = null;

        if (IsDead || StateMachine.CurrentState == PanicState || StateMachine.CurrentState == LeavingState) yield break;
        EnterPanic();
    }

    private void EnterPanic()
    {
        ChangeToState(PanicState, CitizenState.Panic);
    }

    public void RecoverFromPanic()
    {
        if (CitizenType == CitizenType.Audience)
            ChangeToState(WatchingState, CitizenState.Watching);
        else
            ChangeToState(PasserbyState, CitizenState.Roaming);
    }

    protected override void Die(GameObject attacker)
    {
        base.Die(attacker);

        if (panicRoutine != null)
        {
            StopCoroutine(panicRoutine);
            panicRoutine = null;
        }

        PlayDeathAnimation();
        NPCManager.Instance?.UnregisterCitizen(this);
    }

    public override void OnInteracted()
    {
        if (IsDead) return;

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