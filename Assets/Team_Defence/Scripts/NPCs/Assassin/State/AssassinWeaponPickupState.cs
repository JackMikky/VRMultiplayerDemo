using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;

public class AssassinWeaponPickupState : IState
{
    private const float NavMeshSampleDistance = 2f;
    private const float DefaultPickupDuration = 1.5f;

    private readonly AssassinNPC npc;
    private SearchableObject targetSearchableObject;
    private float nextSearchTime;
    private float nextPathUpdateTime;
    private float pickupStartTime;
    private bool isStandingUp;
    private float standUpEndTime;

    public AssassinWeaponPickupState(AssassinNPC npc)
    {
        this.npc = npc;
    }

    public void Enter()
    {
        targetSearchableObject = null;
        nextSearchTime = Time.time;
        nextPathUpdateTime = 0f;
        pickupStartTime = 0f;
        isStandingUp = false;
        standUpEndTime = 0f;

        npc.SetNavigationMode(useAgent: true);
        npc.ResetMovementAnimationFlags();

        if (npc.Agent == null || !npc.Agent.enabled || !npc.Agent.isOnNavMesh)
        {
            return;
        }

        npc.Agent.autoTraverseOffMeshLink = true;
        npc.SetAgentVelocity(npc.walkSpeed, isStopped: false);
    }

    public void Update()
    {
        if (isStandingUp)
        {
            if (Time.time >= standUpEndTime)
            {
                npc.ResumeAfterWeaponPickup();
            }
            return;
        }

        if (npc.CurrentEquipment != null)
        {
            npc.ResumeAfterWeaponPickup();
            return;
        }

        if (npc.Agent == null || !npc.Agent.enabled || !npc.Agent.isOnNavMesh)
        {
            return;
        }

        if (targetSearchableObject != null && !targetSearchableObject.HasWeaponItems())
        {
            ReleaseTarget();
            nextSearchTime = Time.time + npc.WeaponSearchInterval;
        }

        if (targetSearchableObject == null)
        {
            npc.Agent.isStopped = true;
            npc.SyncMovementAnimation();

            if (Time.time >= nextSearchTime)
            {
                TryFindWeapon();
            }

            return;
        }

        if (Time.time >= nextPathUpdateTime && !SetDestinationToTarget())
        {
            ReleaseTarget();
            nextSearchTime = Time.time + npc.WeaponSearchInterval;
            return;
        }

        if (HasReachedWeapon())
        {
            npc.Agent.isStopped = true;
            npc.ResetMovementAnimationFlags();
            if (npc.Anim != null)
            {
                npc.Anim.SetBool(AnimationConstants.IsIdling, true);
                npc.Anim.SetBool(AnimationConstants.IsSearching, true);
            }

            if (pickupStartTime == 0f)
            {
                pickupStartTime = Time.time;
            }

            float pickupDuration = npc.WeaponPickupBehavior != null ? npc.WeaponPickupBehavior.PickupDuration : DefaultPickupDuration;
            if (Time.time - pickupStartTime < pickupDuration)
            {
                return;
            }

            if (targetSearchableObject != null)
            {
                ItemObject weapon = SelectBestWeapon(targetSearchableObject);
                if (weapon != null)
                {
                    if (npc.TryEquipWeapon(weapon))
                    {
                        ReleaseTarget();

                        if (npc.Anim != null)
                        {
                            npc.Anim.SetBool(AnimationConstants.IsSearching, false);
                        }

                        isStandingUp = true;
                        standUpEndTime = Time.time + npc.StandUpDuration;
                        return;
                    }

                    targetSearchableObject.AddItem(weapon);
                    weapon.SetEnabled(false);
                }
            }

            ReleaseTarget();
            pickupStartTime = 0f;
            nextSearchTime = Time.time + npc.WeaponSearchInterval;
            return;
        }

        if (!npc.Agent.pathPending && npc.Agent.pathStatus == NavMeshPathStatus.PathInvalid)
        {
            ReleaseTarget();
            nextSearchTime = Time.time + npc.WeaponSearchInterval;
            return;
        }

        npc.SyncMovementAnimation();
    }

    public void Exit()
    {
        ReleaseTarget();

        if (npc.Anim != null)
        {
            npc.Anim.SetBool(AnimationConstants.IsSearching, false);
        }

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

    private void TryFindWeapon()
    {
        nextSearchTime = Time.time + npc.WeaponSearchInterval;

        SceneItemManager manager = SceneItemManager.Instance;
        if (manager == null || !manager.TryReserveRandomSearchableObjectWithWeapon(npc, out targetSearchableObject))
        {
            return;
        }

        if (!SetDestinationToTarget())
        {
            ReleaseTarget();
            nextSearchTime = Time.time + npc.WeaponSearchInterval;
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
        nextPathUpdateTime = Time.time + npc.WeaponSearchInterval;
        return true;
    }

    private bool HasReachedWeapon()
    {
        if (targetSearchableObject == null)
        {
            return false;
        }

        Vector3 offset = targetSearchableObject.transform.position - npc.transform.position;
        offset.y = 0f;

        float pickupDistance = Mathf.Max(npc.WeaponPickupDistance, npc.Agent.stoppingDistance);
        return offset.sqrMagnitude <= pickupDistance * pickupDistance
            || (!npc.Agent.pathPending
                && npc.Agent.hasPath
                && npc.Agent.remainingDistance <= pickupDistance);
    }

    private void ReleaseTarget()
    {
        if (targetSearchableObject != null)
        {
            SceneItemManager.Instance?.ReleaseSearchableReservation(npc, targetSearchableObject);
            targetSearchableObject = null;
        }
        pickupStartTime = 0f;
    }

    private ItemObject SelectBestWeapon(SearchableObject searchable)
    {
        IReadOnlyList<ItemObject> availableWeapons = searchable.GetAvailableWeapons();
        if (availableWeapons == null || availableWeapons.Count == 0)
        {
            return null;
        }

        ItemObject selectedWeapon = npc.WeaponPickupBehavior != null
            ? npc.WeaponPickupBehavior.SelectBestWeapon(npc, availableWeapons)
            : availableWeapons.FirstOrDefault(item => item != null);

        if (selectedWeapon != null)
        {
            searchable.RemoveItem(selectedWeapon);
        }

        return selectedWeapon;
    }
}