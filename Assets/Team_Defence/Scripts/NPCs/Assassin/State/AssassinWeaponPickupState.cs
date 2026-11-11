using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;

public class AssassinWeaponPickupState : IState
{
    private const float NavMeshSampleDistance = 2f;

    private readonly AssassinNPC npc;
    private SearchableObject targetSearchableObject;
    private float nextSearchTime;
    private float nextPathUpdateTime;

    public AssassinWeaponPickupState(AssassinNPC npc)
    {
        this.npc = npc;
    }

    public void Enter()
    {
        targetSearchableObject = null;
        nextSearchTime = Time.time;
        nextPathUpdateTime = 0f;

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

            if (targetSearchableObject != null)
            {
                ItemObject weapon = SelectBestWeapon(targetSearchableObject);
                if (weapon != null)
                {
                    if (npc.TryEquipWeapon(weapon))
                    {
                        ReleaseTarget();
                        npc.ResumeAfterWeaponPickup();
                        return;
                    }

                    targetSearchableObject.AddItem(weapon);
                    weapon.SetEnabled(false);
                }
            }

            ReleaseTarget();
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
    }

    private ItemObject SelectBestWeapon(SearchableObject searchable)
    {
        IReadOnlyList<ItemObject> availableWeapons = searchable.GetAvailableWeapons();
        if (availableWeapons == null || availableWeapons.Count == 0)
        {
            return null;
        }

        AssassinPersonalityProfile personality = npc.PersonalityProfile;

        float totalWeight = 0f;
        for (int i = 0; i < availableWeapons.Count; i++)
        {
            ItemObject candidate = availableWeapons[i];
            if (candidate == null)
            {
                continue;
            }

            totalWeight += personality != null ? personality.GetWeaponWeight(candidate.ItemCategory) : 1f;
        }

        ItemObject selectedWeapon = null;

        if (totalWeight <= 0f)
        {
            selectedWeapon = availableWeapons.FirstOrDefault(item => item != null);
        }
        else
        {
            float roll = Random.Range(0f, totalWeight);
            float cumulative = 0f;

            for (int i = 0; i < availableWeapons.Count; i++)
            {
                ItemObject candidate = availableWeapons[i];
                if (candidate == null)
                {
                    continue;
                }

                cumulative += personality != null ? personality.GetWeaponWeight(candidate.ItemCategory) : 1f;
                if (roll <= cumulative)
                {
                    selectedWeapon = candidate;
                    break;
                }
            }

            if (selectedWeapon == null)
            {
                selectedWeapon = availableWeapons.LastOrDefault(item => item != null);
            }
        }

        if (selectedWeapon != null)
        {
            searchable.RemoveItem(selectedWeapon);
        }

        return selectedWeapon;
    }
}