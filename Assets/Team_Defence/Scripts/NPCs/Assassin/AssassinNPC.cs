using UnityEngine;

public class AssassinNPC : NPCBase
{
    #region StateMachine

    public ThreateningState ThreateningState { get; private set; }
    public AssassinStayingState StayingState { get; private set; }
    public AssassinApproachingState ApproachingState { get; private set; }
    public AssassinRushingState RushingState { get; private set; }
    public AssassinNavLinkState NavLinkState { get; private set; }
    public AssassinInteractedState InteractedState { get; private set; }

    public AssassinAttackState AttackState { get; private set; }

    [HideInInspector] public IState previousState;
    [HideInInspector] public AssassinState previousEnumState;
    [HideInInspector] public float interactionEndTime;

    #endregion StateMachine

    #region Behavior Settings

    [Header("Behavior Assets")]
    [Header("Movement")]
    [SerializeField] private ScriptableMoveBehavior approachingBehavior;

    public ScriptableMoveBehavior ApproachingBehavior => approachingBehavior;

    [SerializeField] private ScriptableMoveBehavior rushingBehavior;
    public ScriptableMoveBehavior RushingBehavior => rushingBehavior;

    [Header("Idle")]
    [SerializeField] private ScriptableIdleBehavior idleBehavior;

    public ScriptableIdleBehavior IdleBehavior => idleBehavior;

    [Header("Interacted")]
    [SerializeField] private ScriptableInteractedBehavior interactedBehavior;

    public ScriptableInteractedBehavior InteractedBehavior => interactedBehavior;

    [Header("Attack")]
    [SerializeField]
    private ScriptableAttackBehavior attackBehavior;

    public ScriptableAttackBehavior AttackBehavior => attackBehavior;

    [Header("NavLinking")]
    [SerializeField] private ScriptableNavLinkBehavior navLinkingBehavior;

    public ScriptableNavLinkBehavior NavLinkingBehavior => navLinkingBehavior;

    [HideInInspector] public float nextPathUpdateTime;

    #endregion Behavior Settings

    [Header("Speed and Distance Settings")]
    public float walkSpeed = 1.2f;

    public float runSpeed = 2.8f;

    [Tooltip("Start running when distance to VIP is less than this")]
    public float startRunningDistance = 4.0f;

    [Header("Stay Time Settings")]
    [Tooltip("How long to idle after the threatening animation finishes")]
    [SerializeField] private float stayDuration = 2.5f;

    #region Animation hash cache

    public static int ThreateningTrigger => AnimationConstants.Threatening;

    #endregion Animation hash cache

    [Space(10)]
    [Header("Debug")]
    [SerializeField] private AssassinState currentState = AssassinState.Threatening;

    protected override void Awake()
    {
        base.Awake();
        this.npcType = NPCType.Assassin;

        // Initialize the state machine and the various state classes
        ThreateningState = new ThreateningState(this);
        StayingState = new AssassinStayingState(this, this.stayDuration);
        ApproachingState = new AssassinApproachingState(this);
        RushingState = new AssassinRushingState(this);
        NavLinkState = new AssassinNavLinkState(this);
        InteractedState = new AssassinInteractedState(this);
        AttackState = new AssassinAttackState(this);
    }

    protected override void OnSetupBehavior()
    {
        SetNavigationMode(useAgent: true);
        LookAtTargetNPC();
        NPCManager.Instance.RegisterAssassin(this);

        if (agent != null) agent.autoTraverseOffMeshLink = false;

        ChangeToState(ThreateningState, AssassinState.Threatening);
    }

    protected override void OnUpdate()
    {
        if (StateMachine.CurrentState == ApproachingState || StateMachine.CurrentState == RushingState)
        {
            if (agent != null && agent.enabled && agent.isOnNavMesh && agent.isOnOffMeshLink)
            {
                ChangeToState(NavLinkState, AssassinState.NavLinking);
            }
        }
    }

    /// <summary>
    /// Wrapper to change state and update debug enum field
    /// </summary>
    public void ChangeToState(IState newState, AssassinState enumState)
    {
        StateMachine.ChangeState(newState);
        currentState = enumState;
    }

    #region Helpers exposed for states

    public void TargetDistanceCheckAndTransition()
    {
        if (target == null) return;
        float distance = Vector3.Distance(transform.position, target.position);

        if (distance <= startRunningDistance)
            ChangeToState(RushingState, AssassinState.Rushing);
        else
            ChangeToState(ApproachingState, AssassinState.Approaching);
    }

    public void SetDestinationToTarget()
    {
        if (agent != null && agent.enabled && agent.isOnNavMesh && target != null)
        {
            agent.SetDestination(target.position);
        }
    }

    public void SyncMovementAnimation()
    {
        if (anim == null || agent == null || !agent.enabled) return;

        Vector3 horizontalVelocity = new Vector3(agent.velocity.x, 0, agent.velocity.z);
        float currentSpeed = horizontalVelocity.magnitude;

        if (currentSpeed > 0.15f)
        {
            bool shouldRun = (StateMachine.CurrentState == RushingState) && currentSpeed > (walkSpeed * 1.1f);
            ResetMovementAnimationFlags();

            if (shouldRun)
                anim.SetBool(AnimationConstants.IsRunning, true);
            else
                anim.SetBool(AnimationConstants.IsWalking, true);
        }
        else
        {
            ResetMovementAnimationFlags();
            anim.SetBool(AnimationConstants.IsIdling, true);
        }
    }

    private void LookAtTargetNPC()
    {
        if (target != null)
        {
            transform.LookAt(new Vector3(target.position.x, transform.position.y, target.position.z));
        }
    }

    public void StartMovingAfterThreaten()
    {
        if (StateMachine.CurrentState == ThreateningState)
        {
            ChangeToState(StayingState, AssassinState.Staying);
        }
    }

    #endregion Helpers exposed for states

    public override void OnInteracted()
    {
        base.OnInteracted();
        ChangeToState(InteractedState, AssassinState.Interacted);
        NPCManager.Instance.UnregisterAssassin(this);
    }
}