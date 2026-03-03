using System;

namespace Osmalyzer.Commands;

internal class RestoreNodeCommand : Command
{
    public OsmNode Node { get; }
    public OsmElementState State { get; }

    
    internal RestoreNodeCommand(OsmData data, OsmNode node, OsmElementState state)
        : base(data)
    {
        if (node.Owner != Data) throw new InvalidOperationException();
       
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