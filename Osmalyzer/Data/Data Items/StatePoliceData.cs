namespace Osmalyzer;

public class StatePoliceData : IDataItem
{
    public string Name { get; }
    
    public OsmCoord Coord { get; }
    
    /// <summary>Short display name from the contact list, e.g. "Rīgas Pārdaugavas pārvalde"</summary>
    public string? ListName { get; private set; }

    /// <summary>Phone number from the contact list, excluding 112</summary>
    public string? Phone { get; private set; }

    /// <summary>Email address from the contact list</summary>
    public string? Email { get; private set; }
    
    
    private bool _listEntryAttached;


    public StatePoliceData(string officeName, OsmCoord coord)
    {
        Coord = coord;
        Name = officeName;
    }


    public void SetListData(StatePoliceListEntry listEntry)
    {
        ListName = listEntry.Name;
        Phone = listEntry.Phone;
        Email = listEntry.Email;
        
        _listEntryAttached = true;
    }


    public string ReportString()
    {
        if (_listEntryAttached)
            return "`" + Name + "` " +
                   (ListName != null && ListName != Name ? "(listed as `" + ListName + "`) " : "") +
                   "phone: " + (Phone != null ? "`" + Phone + "`" : "none") + ", " +
                   "email: " + (Email != null ? "`" + Email + "`" : "none");
        
        return "`" + Name + "`";
    }

    public override string ToString() => ReportString();
}