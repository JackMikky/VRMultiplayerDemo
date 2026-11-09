using UnityEngine;

public class VipNPC : NPCBase
{
    private static readonly int IsTalkingHash = AnimationConstants.IsTalking;

    private static readonly int IsWalkingHash = AnimationConstants.IsWalking;

    protected override void Awake()
    {
        base.Awake();
        this.npcType = NPCType.VIP;

        anim = GetComponentInChildren<Animator>();
    }

    protected override void OnSetupBehavior()
    {
        SetNavigationMode(useAgent: false);
        NPCManager.Instance.RegisterVIP(this);

        if (anim != null)
        {
            anim.SetBool(IsTalkingHash, true);
            anim.SetBool(IsWalkingHash, false);
        }

        Debug.Log($"[{name}] VIP is in position and started speaking...");
    }

    public override void OnInteracted()
    {
        base.OnInteracted();

        Debug.LogWarning("Warning: You attacked or interfered with the VIP! All guards are on high alert!");
    }

    public void TriggerPanic()
    {
        if (anim != null)
        {
            anim.SetBool(IsTalkingHash, false); // stop talking
        }
        Debug.Log("VIP panicked and stopped speaking!");
    }

    protected override void Die(GameObject attacker)
    {
        base.Die(attacker);

        Debug.LogWarning("VIP is dead; mission failed.!");
        GameManager.Instance.EndGame();
    }
}