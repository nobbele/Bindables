namespace Bindables;

/// <summary>
/// Bindable data container.
/// </summary>
/// <typeparam name="T">Type of data to bind</typeparam>
public class Bindable<T>
{
    /// <summary>
    /// Called when the value changes.
    /// </summary>
    public event Action<ValueChangedEvent<T>>? ValueChanged;

    /// <summary>
    /// Sets the value of this bindable; notifies and propogates the update to this and any other bound bindables.
    /// </summary>
    public T? Value
    {
        get => value;
        set
        {
            SetValue(value, this);
        }
    }

    private T? value;
    private WeakReference<Bindable<T>> WeakReference => new(this);
    private readonly List<WeakReference<Bindable<T>>> bindings = new();

    public Bindable(T? defaultValue = default)
    {
        value = defaultValue;
    }

    /// <summary>
    /// Bind this bindable to another bindable.
    /// </summary>
    /// <remarks>
    /// The value of the current bindable will be updated to receive the value of the target.
    /// </remarks>
    public void BindTo(Bindable<T> target, bool updateImmediatelyOnlyIfDifferent = false)
    {
        bindings.Add(target.WeakReference);
        target.bindings.Add(WeakReference);

        bool doUpdate = true;
        if (updateImmediatelyOnlyIfDifferent)
        {
            doUpdate = !EqualityComparer<T>.Default.Equals(Value, target.Value);
        }

        if (doUpdate)
        {
            Value = target.Value;
        }
    }

    /// <summary>
    /// Registers an event handler `f` to be called whenever the value of this bindable changes.
    /// </summary>
    /// <remarks>
    /// It's also triggered by any other bound bindables.
    /// </remarks>
    /// <param name="f">Event handler callback.</param>
    /// <param name="callImmediately">Whether or not the parameter `f` should be called immediately with the current value.</param>
    public void OnValueChanged(Action<ValueChangedEvent<T>> f, bool callImmediately = true)
    {
        ValueChanged += f;
        if (callImmediately)
        {
            f(new()
            {
                oldValue = default,
                newValue = Value,
            });
        }
    }

    void SetValue(T? newValue, Bindable<T> source)
    {
        var oldValue = value;
        value = newValue;

        bindings.RemoveAll(b => !b.TryGetTarget(out var _));
        foreach (var b in bindings)
        {
            if (b.TryGetTarget(out var existB) && existB != source)
            {
                existB.SetValue(newValue, this);
            }
        }

        ValueChanged?.Invoke(new()
        {
            oldValue = oldValue,
            newValue = newValue,
        });
    }
}