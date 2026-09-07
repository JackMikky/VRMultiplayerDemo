using UnityEngine;

public class AnimationEventReceiver : MonoBehaviour
{
    private AssassinNPC assassinNPC;

    private void Awake()
    {
        assassinNPC = GetComponentInParent<AssassinNPC>();
    }

    public void OnThreatenAnimationFinished()
    {
        Debug.Log("AnimationEventReceiver: Threaten animation finished event received.");
        if (assassinNPC != null)
        {
            assassinNPC.StartMovingAfterThreaten();
        }
    }
}