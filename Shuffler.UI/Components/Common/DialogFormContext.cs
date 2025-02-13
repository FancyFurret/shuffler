namespace Shuffler.UI.Components.Common;

public class DialogFormContext<T>
{
    public T Model { get; }

    public DialogFormContext(T model)
    {
        Model = model;
    }
}