using UnityEngine;

[CreateAssetMenu(fileName = "NewAssassinInteracted", menuName = BehaviorConstants.InteractedBehaviorMenuName + "AssassinInteracted")]
public class AssassinInteractedBehavior : ScriptableInteractedBehavior
{
    public override void Enter(NPCBase npc)
    {
        npc.SetNavigationMode(false);

        var player = Camera.main.transform;
        npc.LookAtPlayer(player);

        if (npc.Anim != null) npc.Anim.SetTrigger(AnimationConstants.Exposed);

        Debug.Log("[GameManager] Game Over! You caught the assassin!");
        // GameManager.Instance.TriggerVictory();
    }

    public override void UpdateBehavior(NPCBase npc)
    { }

    public override void Exit(NPCBase npc)
    { }
}