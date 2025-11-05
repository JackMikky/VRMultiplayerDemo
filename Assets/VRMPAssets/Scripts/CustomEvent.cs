using UnityEngine;
using UnityEngine.Events;

[System.Serializable]
public class CustomEvent : UnityEvent, ICustomEvent
{
    public void AddOnceListener(UnityAction call)
    {
        if (call == null) return;
        UnityAction wrapper = null;
        wrapper = () =>
        {
            try { call(); } catch { }
            this.RemoveListener(wrapper);
        };
        this.AddListener(wrapper);
    }
}

[System.Serializable]
public class CustomEvent<T> : UnityEvent<T>, ICustomEvent<T>
{
    public void AddOnceListener(UnityAction<T> call)
    {
        if (call == null) return;
        UnityAction<T> wrapper = null;
        wrapper = (arg) =>
        {
            try { call(arg); } catch { }
            this.RemoveListener(wrapper);
        };
        this.AddListener(wrapper);
    }
}

public interface ICustomEventBase
{ }

public interface ICustomEvent : ICustomEventBase
{
    void AddOnceListener(UnityAction call);
}

public interface ICustomEvent<T> : ICustomEventBase
{
    void AddOnceListener(UnityAction<T> call);
}