namespace Osmalyzer.Commands;

internal class CreateNodeCommand : Command
{
    public long? Id { get; }
    
    public OsmCoord Coord { get; }
    
    /// <summary> The ID of the node that was created by <see cref="Apply"/>, or null if not yet applied </summary>
    public long? CreatedNodeId { get; private set; }

    
    internal CreateNodeCommand(OsmData data, OsmCoord coord)
        : base(data)
    {
        Coord = coord;
        Id = null;
    }
    
    internal CreateNodeCommand(OsmData data, long id, OsmCoord coord)
        : base(data)
    {
        Coord = coord;
        Id = id;
    }
    
    
    internal override Command Apply()
    {
        // Actuate

        OsmNode newNode;
        
        if (Id == null)
            newNode = new OsmNode(Coord, Data);
        else
            newNode = new OsmNode(Id.Value, Coord, Data);
        
        Data.RegisterElement(newNode);
        
        // Store created node ID for reference
        CreatedNodeId = newNode.Id;
        
        // Return inverse command, i.e. delete by ID
        return new DeleteNodeCommand(Data, newNode.Id);
    }
}