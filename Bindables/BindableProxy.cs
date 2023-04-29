namespace Bindables;

public class BindableProxy<T> : IBindable<T>
{
    private readonly IBindable target;

    object? IBindable.Value
    {
        get => target.Value;
        set => target.Value = value;
    }

    public T? Value
    {
        get => (T?)(this as IBindable).Value;
        set => (this as IBindable).Value = value;
    }

    public BindableProxy(IBindable target)
    {
        this.target = target;
    }
}