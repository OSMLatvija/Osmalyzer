namespace Osmalyzer.Commands;

internal abstract class Command
{
    internal abstract Command Apply();
}

internal class SetTagCommand : Command
{
    public OsmElement Element { get; }
    public string Key { get; }
    public string? Value { get; }
    public OsmElementState State { get; }

    
    internal SetTagCommand(OsmElement element, string key, string? value, OsmElementState state)
    {
        Element = element;
        Key = key;
        Value = value;
        State = state;
    }


    internal override Command? Apply()
    {
        // Store values for undo
        string? existingValue = Element.GetValue(Key);
        OsmElementState existingState = Element.State;
        
        // Actuate
        bool actuated = Element.SetValueInternal(Key, Value, State);
        
        // Return inverse command, i.e. undo
        return actuated ? new SetTagCommand(Element, Key, existingValue ?? null, existingState) : null;
    }
}