namespace Osmalyzer;

public class ParcelLocker : IDataItem
{
    public string Operator { get; }

    public string? Id { get; }

    public string? Name { get; }

    public string? Address { get; }
    
    public OsmCoord Coord { get; }


    public ParcelLocker(string operatorCompany, string? id, string? name, string? address, OsmCoord coord)
    {
        Id = id;
        Name = name;
        Address = address;
        Coord = coord;
        Operator = operatorCompany;
    }
    
    
    public virtual string ReportString()
    {
        return
            Operator + " parcel locker" +
            (Name != null ? " `" + Name + "`" : "") +
            (Id != null ? " (`" + Id + "`)" : "") +
            (Address != null ? " at `" + Address + "`" : "");
    }
}