using System;

namespace Osmalyzer.Commands;

internal class DeleteNodeCommand : Command
{
    public OsmNode Node { get; }

    
    internal DeleteNodeCommand(OsmData data, OsmNode node)
        : base(data)
    {
        if (node.Owner != Data) throw new InvalidOperationException();
     
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