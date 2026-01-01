namespace Osmalyzer;

public enum OsmElementState
{
    /// <summary> As seen in the original data; no changes </summary>
    Live,
    
    /// <summary> Modified since original data </summary>
    Modified,
    
    /// <summary> Deleted from original data </summary>
    Deleted,
    
    /// <summary> Newly created since original data </summary> 
    Created
}