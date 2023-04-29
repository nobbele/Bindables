namespace Bindables;

public interface IBindable
{
    object? Value { get; set; }
}

public interface IBindable<T> : IBindable
{
    new T? Value { get; set; }
}