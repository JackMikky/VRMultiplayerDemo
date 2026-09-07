using UnityEngine;

public class PoliceInteractedState : IState
{
    private PoliceNPC npc;

    public PoliceInteractedState(PoliceNPC npc)
    {
        this.npc = npc;
    }

    public void Enter()
    {
        npc.SetNavigationMode(useAgent: false);
        npc.InteractedBehavior?.Enter(npc);
        npc.ShowMark(true);
    }

    public void Update()
    {
        npc.InteractedBehavior?.UpdateBehavior(npc);

        if (Time.time >= npc.interactionEndTime)
        {
            if (npc.previousState != null)
            {
                npc.ChangeToState(npc.previousState, npc.previousEnumState);
            }
        }
    }

    public void Exit()
    {
        npc.ShowMark(false);
        npc.InteractedBehavior?.Exit(npc);
    }

    public void Dispose()
    { }
}