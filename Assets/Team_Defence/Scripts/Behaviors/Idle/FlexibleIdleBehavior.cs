using UnityEngine;

[CreateAssetMenu(fileName = "NewFlexibleIdle", menuName = BehaviorConstants.IdleBehaviorMenuName + "FlexibleIdle")]
public class FlexibleIdleBehavior : ScriptableIdleBehavior
{
    [Header("Animation Trigger List")]
    [SerializeField] private string[] availableAnimationTriggers;

    [Header("Action Trigger Time Interval")]
    [SerializeField] private float minInterval = 5f;

    [SerializeField] private float maxInterval = 15f;

    [Header("Probability Settings")]
    [Range(0f, 1f)]
    [SerializeField] private float actionChance = 0.35f;

    public override void Enter(NPCBase npc)
    {
        npc.SetNavigationMode(false);
        if (npc.Anim != null)
        {
            npc.Anim.SetBool(AnimationConstants.IsWalking, false);
            npc.Anim.SetBool(AnimationConstants.IsIdling, true);
        }

        if (availableAnimationTriggers != null && availableAnimationTriggers.Length > 0)
        {
            npc.cachedIdleAnimationHashes = new int[availableAnimationTriggers.Length];

            for (int i = 0; i < availableAnimationTriggers.Length; i++)
            {
                npc.cachedIdleAnimationHashes[i] = Animator.StringToHash(availableAnimationTriggers[i]);
            }
        }

        UpdateNextActionTime(npc);
    }

    public override void UpdateBehavior(NPCBase npc)
    {
        if (npc.Anim == null) return;

        if (npc.cachedIdleAnimationHashes == null || npc.cachedIdleAnimationHashes.Length == 0)
            return;

        if (Time.time >= npc.nextIdleActionTime)
        {
            if (Random.value <= actionChance)
            {
                int randomIndex = Random.Range(0, npc.cachedIdleAnimationHashes.Length);
                int selectedTriggerHash = npc.cachedIdleAnimationHashes[randomIndex];

                npc.Anim.SetTrigger(selectedTriggerHash);
            }

            UpdateNextActionTime(npc);
        }
    }

    public override void Exit(NPCBase npc)
    {
        npc.cachedIdleAnimationHashes = null;
    }

    private void UpdateNextActionTime(NPCBase npc)
    {
        npc.nextIdleActionTime = Time.time + Random.Range(minInterval, maxInterval);
    }
}