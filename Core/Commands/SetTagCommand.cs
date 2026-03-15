namespace Osmalyzer.Commands;

internal class SetTagCommand : Command
{
    public OsmElement.OsmElementType ElementType { get; }
    public long ElementId { get; }
    public string Key { get; }
    public string? Value { get; }
    public OsmElementState State { get; }

    
    internal SetTagCommand(OsmData data, OsmElement.OsmElementType elementType, long elementId, string key, string? value, OsmElementState state)
        : base(data)
    {
        ElementType = elementType;
        ElementId = elementId;
        Key = key;
        Value = value;
        State = state;
    }


    internal override Command? Apply()
    {
        OsmElement element = Data.GetElementById(ElementType, ElementId);
        
        // Store values for undo
        bool hadTagsBefore = element.HasAnyTags;
        string? existingValue = element.GetValue(Key);
        OsmElementState existingState = element.State;
        
        // Actuate
        bool actuated = element.SetValueInternal(Key, Value, State);

        if (!actuated)
            return null;
        
        Data.UpdateElementTagStatus(element, hadTagsBefore);
        
        // Return inverse command, i.e. undo
        return new SetTagCommand(Data, ElementType, ElementId, Key, existingValue, existingState);
    }
}