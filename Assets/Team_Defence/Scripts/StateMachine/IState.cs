using System;

public interface IState : IDisposable
{
    void Enter();

    void Exit();

    void Update();
}