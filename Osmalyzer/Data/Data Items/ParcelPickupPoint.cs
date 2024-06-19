namespace Osmalyzer;

/// <summary>
/// A pickup point in another POI, i.e. `post_office=post_partner` within some `shop` or `amenity` or something. 
/// </summary>
public class ParcelPickupPoint : IDataItem
{
    public string Operator { get; }

    public string? Id { get; }

    public string? Name { get; }

    public string? Address { get; }
    
    public OsmCoord Coord { get; }

    /// <summary> In what POI/amenity/shop are we? </summary>
    public string? Location { get; }


    public ParcelPickupPoint(string operatorCompany, string? id, string? name, string? address, OsmCoord coord, string? location)
    {
        Operator = operatorCompany;
        Id = id;
        Name = name;
        Address = address;
        Coord = coord;
        Location = location;
    }
    
    
    public virtual string ReportString()
    {
        return
            Operator + " parcel pickup point" +
            (Name != null ? " `" + Name + "`" : "") +
            (Id != null ? " (`" + Id + "`)" : "") +
            (Location != null ? " in `" + Location + "`" : "") +
            (Address != null ? " at `" + Address + "`" : "");
    }
}