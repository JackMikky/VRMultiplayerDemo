using UnityEngine;

public class CitizenWatchingState : IState
{
    private readonly CitizenNPC npc;
    private readonly StayingAnimationPlayer player;
    private bool isPlaying;
    private float nextPlayTime;

    public CitizenWatchingState(CitizenNPC npc)
    {
        this.npc = npc;
        player = new StayingAnimationPlayer(npc);
    }

    public void Enter()
    {
        npc.SetNavigationMode(useAgent: false);
        BeginIdle();
    }

    public void Update()
    {
        if (isPlaying)
        {
            if (player.Tick())
            {
                player.End();
                BeginIdle();
            }
        }
        else if (Time.time >= nextPlayTime)
        {
            isPlaying = true;
            player.Begin(npc.StayingAnimationCount);
        }
    }

    public void Exit()
    {
        isPlaying = false;
        player.End();
    }

    public void Dispose()
    { }

    private void BeginIdle()
    {
        isPlaying = false;
        nextPlayTime = Time.time + Random.Range(npc.minIdleInterval, npc.maxIdleInterval);

        npc.ResetMovementAnimationFlags();
        if (npc.Anim != null) npc.Anim.SetBool(AnimationConstants.IsIdling, true);
    }
}
