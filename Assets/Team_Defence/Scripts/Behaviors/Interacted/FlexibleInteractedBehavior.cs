using UnityEngine;

[CreateAssetMenu(fileName = "NewFlexibleInteracted", menuName = BehaviorConstants.InteractedBehaviorMenuName + "FlexibleInteracted")]
public class FlexibleInteractedBehavior : ScriptableInteractedBehavior
{
    [SerializeField] private string[] availableAnimationTriggers;
    [SerializeField] private float interactionDuration = 3.0f;

    public override void Enter(NPCBase npc)
    {
        npc.SetNavigationMode(false);
        if (npc.Anim != null)
        {
            npc.Anim.SetBool(AnimationConstants.IsWalking, false);
            npc.Anim.SetBool(AnimationConstants.IsIdling, true);
        }

        if (availableAnimationTriggers != null && availableAnimationTriggers.Length > 0 && npc.Anim != null)
        {
            int randomIndex = Random.Range(0, availableAnimationTriggers.Length);
            npc.Anim.SetTrigger(availableAnimationTriggers[randomIndex]);
        }

        if (npc is CitizenNPC citizen)
        {
            citizen.interactionEndTime = Time.time + interactionDuration;
        }
    }

    public override void UpdateBehavior(NPCBase npc)
    {
        var player = Camera.main.transform;
        npc.LookAtPlayer(player);
    }

    public override void Exit(NPCBase npc)
    { }
}