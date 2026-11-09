using UnityEngine;
using UnityEngine.AI;

public class AssassinWeaponPickupState : IState
{
    private const float NavMeshSampleDistance = 2f;

    private readonly AssassinNPC npc;
    private WeaponObject targetWeapon;
    private float nextSearchTime;
    private float nextPathUpdateTime;

    public AssassinWeaponPickupState(AssassinNPC npc)
    {
        this.npc = npc;
    }

    public void Enter()
    {
        targetWeapon = null;
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

        if (targetWeapon != null && targetWeapon.IsPickedUp)
        {
            ReleaseTarget();
            nextSearchTime = Time.time + npc.WeaponSearchInterval;
        }

        if (targetWeapon == null)
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

            if (npc.TryEquipWeapon(targetWeapon))
            {
                WeaponManager.Instance?.ReleaseReservation(npc, targetWeapon);
                targetWeapon = null;
                npc.ResumeAfterWeaponPickup();
                return;
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

        WeaponManager manager = WeaponManager.Instance;
        if (manager == null || !manager.TryReserveRandomWeapon(npc, out targetWeapon))
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
        if (targetWeapon == null || npc.Agent == null || !npc.Agent.isOnNavMesh)
        {
            return false;
        }

        if (!NavMesh.SamplePosition(
                targetWeapon.transform.position,
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
        Vector3 offset = targetWeapon.transform.position - npc.transform.position;
        offset.y = 0f;

        float pickupDistance = Mathf.Max(npc.WeaponPickupDistance, npc.Agent.stoppingDistance);
        return offset.sqrMagnitude <= pickupDistance * pickupDistance
            || (!npc.Agent.pathPending
                && npc.Agent.hasPath
                && npc.Agent.remainingDistance <= pickupDistance);
    }

    private void ReleaseTarget()
    {
        if (targetWeapon != null)
        {
            WeaponManager.Instance?.ReleaseReservation(npc, targetWeapon);
            targetWeapon = null;
        }
    }
}
