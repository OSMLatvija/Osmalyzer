namespace Osmalyzer;

public class StatePoliceData : IDataItem
{
    public string Name { get; }

    public OsmCoord Coord { get; }

    /// <summary>Address text from the branch page</summary>
    public string? Address { get; }

    /// <summary>Phone number from the branch page, excluding 112</summary>
    public string? Phone { get; }

    /// <summary>Email address from the branch page</summary>
    public string? Email { get; }

    /// <summary>Opening hours in OSM format</summary>
    public string? OpeningHours { get; }


    public StatePoliceData(string name, OsmCoord coord, string? address, string? phone, string? email, string? openingHours)
    {
        Name = name;
        Coord = coord;
        Address = address;
        Phone = phone;
        Email = email;
        OpeningHours = openingHours;
    }


    [Pure]
    public string ReportString()
    {
        return "State police office `" + Name + "` " +
               (Address != null ? "at `" + Address + "` " : "") +
               "phone: " + (Phone != null ? "`" + Phone + "`" : "none") + ", " +
               "email: " + (Email != null ? "`" + Email + "`" : "none") +
               (OpeningHours != null ? ", hours: `" + OpeningHours + "`" : "");
    }

    public override string ToString() => ReportString();
}