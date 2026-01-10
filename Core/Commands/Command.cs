using System;

namespace Osmalyzer.Commands;

internal abstract class Command
{
    public OsmData Data { get; }

    
    protected Command(OsmData data)
    {
        Data = data;
    }

    
    internal abstract Command? Apply();
}

internal class SetTagCommand : Command
{
    public OsmElement Element { get; }
    public string Key { get; }
    public string? Value { get; }
    public OsmElementState State { get; }

    
    internal SetTagCommand(OsmData data, OsmElement element, string key, string? value, OsmElementState state)
        : base(data)
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
        return actuated ? new SetTagCommand(Data, Element, Key, existingValue ?? null, existingState) : null;
    }
}

internal class CreateNodeCommand : Command
{
    public OsmCoord Coord { get; }
    
    public OsmNode? CreatedNode { get; private set; }

    
    internal CreateNodeCommand(OsmData data, OsmCoord coord)
        : base(data)
    {
        Coord = coord;
    }
    
    
    internal override Command Apply()
    {
        // Actuate
        OsmNode newNode = new OsmNode(Coord, Data); // Owner will be set in OsmData.ApplyCommand
        Data.RegisterElement(newNode);
        
        // Store created node for reference
        CreatedNode = newNode;
        
        // Return inverse command, i.e. delete
        return new DeleteNodeCommand(Data, newNode);
    }
}

internal class DeleteNodeCommand : Command
{
    public OsmNode Node { get; }

    
    internal DeleteNodeCommand(OsmData data, OsmNode node)
        : base(data)
    {
        Node = node;
    }
    
    
    internal override Command Apply()
    {
        if (Node.State == OsmElementState.Deleted) throw new InvalidOperationException();
        if (Node.ways?.Count > 0) throw new InvalidOperationException();
        if (Node.relations?.Count > 0) throw new InvalidOperationException();

        // Store values for undo
        OsmElementState existingState = Node.State;
        
        // Actuate
        Node.State = OsmElementState.Deleted;
        Data.UnregisterElement(Node);
        // TODO: LINKS AND STUFF
        
        // Return inverse command, i.e. recreate
        return new RestoreNodeCommand(Data, Node, existingState);
    }
}

internal class RestoreNodeCommand : Command
{
    public OsmNode Node { get; }
    public OsmElementState State { get; }

    
    internal RestoreNodeCommand(OsmData data, OsmNode node, OsmElementState state)
        : base(data)
    {
        Node = node;
        State = state;
    }
    
    
    internal override Command Apply()
    {
        if (Node.State != OsmElementState.Deleted) throw new InvalidOperationException();
        
        // Actuate
        Node.State = State;
        Data.RegisterElement(Node);
        
        // Return inverse command, i.e. delete
        return new DeleteNodeCommand(Data, Node);
    }
}