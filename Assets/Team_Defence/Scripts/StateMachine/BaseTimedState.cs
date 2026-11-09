using UnityEngine;

public abstract class BaseTimedState<T> : IState where T : NPCBase
{
    protected T npc;
    protected float duration;
    protected float timer;

    public BaseTimedState(T npc, float duration)
    {
        this.npc = npc;
        this.duration = duration;
    }

    public virtual void Enter()
    {
        timer = duration;
    }

    public virtual void Update()
    {
        timer -= Time.deltaTime;
        if (timer <= 0f)
        {
            OnTimeOut();
        }
    }

    public virtual void Exit()
    {
    }

    protected abstract void OnTimeOut();

    public virtual void Dispose()
    {
        // Clean up resources if needed
    }
}