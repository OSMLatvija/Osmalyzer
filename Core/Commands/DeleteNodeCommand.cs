using System;

namespace Osmalyzer.Commands;

internal class DeleteNodeCommand : Command
{
    public long NodeId { get; }

    
    internal DeleteNodeCommand(OsmData data, long nodeId)
        : base(data)
    {
        NodeId = nodeId;
    }
    
    
    internal override Command Apply()
    {
        OsmNode node = Data.GetNodeById(NodeId);
        
        if (node.State == OsmElementState.Deleted) throw new InvalidOperationException();
        if (node.ways?.Count > 0) throw new InvalidOperationException();
        if (node.relations?.Count > 0) throw new InvalidOperationException();

        // Store values for undo
        OsmElementState existingState = node.State;
        
        // Actuate
        node.State = OsmElementState.Deleted;
        Data.UnregisterElement(node);
        // TODO: LINKS AND STUFF
        
        // Return inverse command, i.e. recreate
        // Note: holds the instance directly as it is no longer in the registry and must be re-inserted as-is
        return new RestoreNodeCommand(Data, node, existingState);
    }
}