namespace Shuffler.Core;

public interface IObservable<out T>
{
    T Value { get; }
    IDisposable AddObserver(Action<T> observer);
}

public class Observable<T>(T initialValue) : IObservable<T>
{
    private T _value = initialValue;
    private readonly List<Action<T>> _listeners = [];
    public event Action? OnChange;

    public T Value
    {
        get => _value;
        set
        {
            if (EqualityComparer<T>.Default.Equals(_value, value)) return;
            _value = value;
            NotifyListeners();
        }
    }


    public IDisposable AddObserver(Action<T> observer)
    {
        _listeners.Add(observer);
        return new Subscription(() => _listeners.Remove(observer));
    }

    private void NotifyListeners()
    {
        foreach (var listener in _listeners.ToList())
            listener(_value);
        OnChange?.Invoke();
    }

    private class Subscription : IDisposable
    {
        private readonly Action _onDispose;

        public Subscription(Action onDispose)
        {
            _onDispose = onDispose;
        }

        public void Dispose()
        {
            _onDispose();
        }
    }

    public void Changed()
    {
        NotifyListeners();
    }
}