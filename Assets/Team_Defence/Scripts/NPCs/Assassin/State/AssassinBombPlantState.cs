using UnityEngine;
using UnityEngine.AI;

public class AssassinBombPlantState : IState
{
    private const float NavMeshSampleDistance = 2f;
    private const float PlantingDuration = 2.5f;

    private readonly AssassinNPC npc;
    private SearchableObject targetSearchableObject;
    private float nextSearchTime;
    private float nextPathUpdateTime;
    private float plantingStartTime;

    public AssassinBombPlantState(AssassinNPC npc)
    {
        this.npc = npc;
    }

    public void Enter()
    {
        targetSearchableObject = null;
        nextSearchTime = Time.time;
        nextPathUpdateTime = 0f;
        plantingStartTime = 0f;

        npc.SetNavigationMode(useAgent: true);
        npc.ResetMovementAnimationFlags();

        if (npc.Agent == null || !npc.Agent.enabled || !npc.Agent.isOnNavMesh)
        {
            Debug.LogWarning($"[{npc.name}] Agent is not available for bomb planting.", npc);
            return;
        }

        npc.Agent.autoTraverseOffMeshLink = true;
        npc.SetAgentVelocity(npc.walkSpeed, isStopped: false);

        Debug.Log($"[{npc.name}] Entered BombPlantState, starting to search for target location.");
    }

    public void Update()
    {
        if (npc.CurrentEquipment == null || npc.CurrentEquipment.ItemCategory != EquipmentCategory.Bomb)
        {
            ResumeNormalBehavior();
            return;
        }

        if (npc.Agent == null || !npc.Agent.enabled || !npc.Agent.isOnNavMesh)
        {
            return;
        }

        if (targetSearchableObject != null && !targetSearchableObject.HasWeaponItems())
        {
            ReleaseTarget();
            nextSearchTime = Time.time + npc.BombSearchInterval;
        }

        if (targetSearchableObject == null)
        {
            npc.Agent.isStopped = true;
            npc.SyncMovementAnimation();

            if (Time.time >= nextSearchTime)
            {
                TryFindSearchableObject();
            }

            return;
        }

        if (Time.time >= nextPathUpdateTime && !SetDestinationToTarget())
        {
            ReleaseTarget();
            nextSearchTime = Time.time + npc.BombSearchInterval;
            return;
        }

        if (HasReachedSearchable())
        {
            npc.Agent.isStopped = true;
            npc.ResetMovementAnimationFlags();

            if (plantingStartTime == 0f)
            {
                plantingStartTime = Time.time;
                if (npc.Anim != null)
                {
                    npc.Anim.SetTrigger(AnimationConstants.Exposed);
                }
            }

            if (Time.time - plantingStartTime >= PlantingDuration)
            {
                PlantBomb();
                ReleaseTarget();
                ResumeNormalBehavior();
                return;
            }

            return;
        }

        if (!npc.Agent.pathPending && npc.Agent.pathStatus == NavMeshPathStatus.PathInvalid)
        {
            ReleaseTarget();
            nextSearchTime = Time.time + npc.BombSearchInterval;
            return;
        }

        npc.SyncMovementAnimation();
    }

    public void Exit()
    {
        ReleaseTarget();

        if (npc.Agent != null && npc.Agent.enabled)
        {
            if (npc.Agent.isOnNavMesh)
            {
                npc.Agent.ResetPath();
            }

            npc.Agent.autoTraverseOffMeshLink = false;
        }
    }

    public void Dispose()
    {
    }

    private void TryFindSearchableObject()
    {
        nextSearchTime = Time.time + npc.BombSearchInterval;

        SceneItemManager manager = SceneItemManager.Instance;
        if (manager == null)
        {
            Debug.LogWarning($"[{npc.name}] SceneItemManager not found!", npc);
            return;
        }

        if (!manager.TryReserveRandomSearchableObjectWithWeapon(npc, out targetSearchableObject))
        {
            Debug.Log($"[{npc.name}] No available searchable objects found for bomb planting.");
            return;
        }

        Debug.Log($"[{npc.name}] Found target searchable object: {targetSearchableObject.name}");

        if (!SetDestinationToTarget())
        {
            Debug.LogWarning($"[{npc.name}] Failed to set destination to target {targetSearchableObject.name}", npc);
            ReleaseTarget();
            nextSearchTime = Time.time + npc.BombSearchInterval;
        }
    }

    private bool SetDestinationToTarget()
    {
        if (targetSearchableObject == null || npc.Agent == null || !npc.Agent.isOnNavMesh)
        {
            return false;
        }

        if (!NavMesh.SamplePosition(
                targetSearchableObject.transform.position,
                out NavMeshHit navHit,
                NavMeshSampleDistance,
                npc.Agent.areaMask))
        {
            return false;
        }

        if (!npc.Agent.SetDestination(navHit.position))
        {
            return false;
        }

        npc.Agent.isStopped = false;
        nextPathUpdateTime = Time.time + 0.25f;
        return true;
    }

    private bool HasReachedSearchable()
    {
        if (targetSearchableObject == null)
        {
            return false;
        }

        Vector3 offset = targetSearchableObject.transform.position - npc.transform.position;
        offset.y = 0f;

        float plantDistance = Mathf.Max(npc.BombPlantDistance, npc.Agent.stoppingDistance);
        return offset.sqrMagnitude <= plantDistance * plantDistance
            || (!npc.Agent.pathPending
                && npc.Agent.hasPath
                && npc.Agent.remainingDistance <= plantDistance);
    }

    private void PlantBomb()
    {
        if (targetSearchableObject == null || npc.CurrentEquipment == null)
        {
            return;
        }

        TimedBomb bomb = npc.CurrentEquipment.Equipment as TimedBomb;
        if (bomb != null)
        {
            ItemObject bombItem = npc.CurrentEquipment;

            // Drop the bomb from the NPC
            if (bombItem.Drop())
            {
                // AddItem handles all the positioning and hierarchy setup
                targetSearchableObject.AddItem(bombItem);

                // Clear the NPC's equipment reference
                npc.ClearEquipment();

                Debug.Log($"[{npc.name}] Transferred bomb to {targetSearchableObject.name}");
            }

            // Plant the bomb (trigger the explosion timer)
            targetSearchableObject.PlantTimedBomb(bomb, npc.transform, bombItem);
            Debug.Log($"[{npc.name}] Planted bomb at {targetSearchableObject.name}");
        }
    }

    private void ReleaseTarget()
    {
        if (targetSearchableObject != null)
        {
            SceneItemManager.Instance?.ReleaseSearchableReservation(npc, targetSearchableObject);
            targetSearchableObject = null;
        }
        plantingStartTime = 0f;
    }

    private void ResumeNormalBehavior()
    {
        npc.ChangeToState(npc.WanderingState, AssassinState.Wandering);
    }
}