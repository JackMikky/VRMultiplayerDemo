using UnityEngine;

public class PoliceChasingState : IState
{
    private PoliceNPC npc;

    public PoliceChasingState(PoliceNPC npc)
    {
        this.npc = npc;
    }

    public void Dispose()
    {
    }

    public void Enter()
    {
    }

    public void Exit()
    {
    }

    public void Update()
    {
    }
}