using UnityEngine;

public class AssassinNPC : NPCBase
{
    #region StateMachine

    public AssassinWanderingState WanderingState { get; private set; }
    public AssassinStayingState StayingState { get; private set; }
    public AssassinApproachingState ApproachingState { get; private set; }
    public AssassinRushingState RushingState { get; private set; }
    public AssassinAttackState AttackState { get; private set; }
    public AssassinWeaponPickupState WeaponPickupState { get; private set; }
    public AssassinNavLinkState NavLinkState { get; private set; }
    public AssassinInteractedState InteractedState { get; private set; }
    public AssassinBombPlantState BombPlantState { get; private set; }

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

    [Tooltip("Number of Staying animations selectable by StayingIndex")]
    [SerializeField, Min(1)] private int stayingAnimationCount = 1;

    public int StayingAnimationCount => stayingAnimationCount;

    [Header("Post-Pickup Wandering")]
    [SerializeField, Min(0f)] private float minWanderDuration = 4f;

    [SerializeField, Min(0f)] private float maxWanderDuration = 8f;
    [SerializeField, Min(0.1f)] private float wanderRadius = 4f;
    [SerializeField, Min(0.1f)] private float minWanderPauseInterval = 3f;
    [SerializeField, Min(0.1f)] private float maxWanderPauseInterval = 6f;
    [SerializeField, Range(0f, 1f)] private float wanderPauseChance = 0.35f;

    public float MinWanderDuration => minWanderDuration;
    public float MaxWanderDuration => maxWanderDuration;
    public float WanderRadius => wanderRadius;
    public float MinWanderPauseInterval => minWanderPauseInterval;
    public float MaxWanderPauseInterval => maxWanderPauseInterval;
    public float WanderPauseChance => wanderPauseChance;

    [Header("Weapon Pickup")]
    [SerializeField] private Transform weaponHoldPoint;

    [SerializeField, Min(0.1f)] private float weaponPickupDistance = 1.25f;
    [SerializeField, Min(0.05f)] private float weaponSearchInterval = 0.5f;

    public Transform WeaponHoldPoint => weaponHoldPoint;
    public float WeaponPickupDistance => weaponPickupDistance;
    public float WeaponSearchInterval => weaponSearchInterval;

    [Header("Personality")]
    [SerializeField] private AssassinPersonalityProfile personalityProfile;

    public AssassinPersonalityProfile PersonalityProfile => personalityProfile;

    [Header("Bomb Planting")]
    [SerializeField, Min(0.1f)] private float bombPlantDistance = 1.5f;

    [SerializeField, Min(0.05f)] private float bombSearchInterval = 0.5f;

    public float BombPlantDistance => bombPlantDistance;
    public float BombSearchInterval => bombSearchInterval;

    [Space(10)]
    [Header("Debug")]
    [SerializeField] private AssassinState currentState = AssassinState.Wandering;

    protected override void Awake()
    {
        base.Awake();
        this.npcType = NPCType.Assassin;
        // Initialize the state machine and the various state classes
        WanderingState = new AssassinWanderingState(this);
        StayingState = new AssassinStayingState(this);
        ApproachingState = new AssassinApproachingState(this);
        RushingState = new AssassinRushingState(this);
        AttackState = new AssassinAttackState(this);
        NavLinkState = new AssassinNavLinkState(this);
        InteractedState = new AssassinInteractedState(this);
        WeaponPickupState = new AssassinWeaponPickupState(this);
        BombPlantState = new AssassinBombPlantState(this);
    }

    protected override void OnSetupBehavior()
    {
        SetNavigationMode(useAgent: true);
        LookAtTargetNPC();
        NPCManager.Instance.RegisterAssassin(this);

        if (agent != null) agent.autoTraverseOffMeshLink = false;
        UpdateWeaponAttackDistance();

        if (currentEquipment == null && SceneItemManager.Instance != null && weaponHoldPoint != null)
        {
            ChangeToState(WeaponPickupState, AssassinState.SeekingWeapon);
            return;
        }

        if (currentEquipment == null && SceneItemManager.Instance != null)
        {
            Debug.LogWarning($"[{name}] Weapon hold point is not assigned; skipping weapon pickup.", this);
        }

        if (currentEquipment != null)
        {
            currentEquipment.SetEnabled(false);
            currentEquipment.PickUp(this.weaponHoldPoint);

            if (currentEquipment.ItemCategory == EquipmentCategory.Bomb)
            {
                ChangeToState(BombPlantState, AssassinState.BombPlanting);
                return;
            }
            else
            {
                ChangeToState(WanderingState, AssassinState.Wandering);
                return;
            }
        }

        ChangeToState(WanderingState, AssassinState.Wandering);
    }

    public bool TryEquipWeapon(ItemObject weapon)
    {
        if (currentEquipment != null || weapon == null || weaponHoldPoint == null)
        {
            return false;
        }

        if (!weapon.PickUp(weaponHoldPoint))
        {
            return false;
        }

        currentEquipment = weapon;
        UpdateWeaponAttackDistance();
        return true;
    }

    private void UpdateWeaponAttackDistance()
    {
        if (agent != null && currentEquipment != null && currentEquipment.Equipment != null)
        {
            agent.stoppingDistance = Mathf.Max(0f, currentEquipment.Equipment.attackDistance);
        }
    }

    public void ResumeAfterWeaponPickup()
    {
        ChangeToState(WanderingState, AssassinState.Wandering);
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

    public void ClearEquipment()
    {
        currentEquipment = null;
    }

    #endregion Helpers exposed for states

    public override void OnInteracted()
    {
        base.OnInteracted();
        ChangeToState(InteractedState, AssassinState.Interacted);
        NPCManager.Instance.UnregisterAssassin(this);
    }
}