using Microsoft.AspNetCore.Components;

namespace Shuffler.UI.Components.Common;

public abstract class ObserverComponent : ComponentBase, IDisposable
{
    private readonly Dictionary<object, IDisposable> _subscriptions = [];

    protected IDisposable Observe<T>(Shuffler.Core.IObservable<T> observable, Action<T> observer)
    {
        var subscription = observable.AddObserver(observer);
        _subscriptions[observable] = subscription;
        // Call observer immediately with current value
        observer(observable.Value);
        return subscription;
    }

    protected T Observe<T>(Shuffler.Core.IObservable<T> observable)
    {
        if (!_subscriptions.ContainsKey(observable))
        {
            var subscription = observable.AddObserver(_ =>
                InvokeAsync(StateHasChanged)
            );
            _subscriptions[observable] = subscription;
        }

        return observable.Value;
    }

    public virtual void Dispose()
    {
        foreach (var subscription in _subscriptions.Values)
        {
            subscription.Dispose();
        }

        _subscriptions.Clear();
        GC.SuppressFinalize(this);
    }
}