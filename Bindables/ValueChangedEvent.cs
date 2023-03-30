namespace Bindables;

public class ValueChangedEvent<T>
{
    public required T? oldValue;
    public required T? newValue;
}
