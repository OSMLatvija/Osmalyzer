namespace Osmalyzer;

public class StatePoliceListEntry : IDataItem
{
    /// <summary>Short name as shown on the contact list page, e.g. "Rīgas Pārdaugavas pārvalde"</summary>
    public string Name { get; }

    /// <summary>Phone number from the contact list, excluding 112 (null if not listed or only 112)</summary>
    public string? Phone { get; }

    /// <summary>Email address from the contact list</summary>
    public string? Email { get; }

    public OsmCoord Coord => throw new InvalidOperationException();
    

    public StatePoliceListEntry(string name, string? phone, string? email)
    {
        Name = name;
        Phone = phone;
        Email = email;
    }

    public string ReportString()
    {
        return "`" + Name + "` " +
               "phone: " + (Phone != null ? "`" + Phone + "`" : "none") + ", " +
               "email: " + (Email != null ? "`" + Email + "`" : "none");
    }
    
    public override string ToString() => ReportString();
}

