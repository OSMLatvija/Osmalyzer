namespace Osmalyzer;

public class VPVKACOffice
{
    public string Name { get; }
    
    public string ShortName { get; }
    
    public string DisambiguatedName { get; }

    public string DisplayName => IsAmbiguous ? DisambiguatedName : ShortName;
    
    public bool IsAmbiguous { get; private set; }
    
    public string Address { get; }
    
    public string? OriginalAddress { get; }
    
    public string Email { get; }
    
    public string Phone { get; }
    
    public string OpeningHours { get; }


    public VPVKACOffice(
        string name,
        string shortName,
        string disambiguatedName,
        string address,
        string email,
        string phone,
        string openingHours,
        string? originalAddress)
    {
        Name = name.Trim();
        ShortName = shortName.Trim();
        DisambiguatedName = disambiguatedName.Trim();
        Address = address;
        OriginalAddress = originalAddress;
        Email = email.Trim();
        Phone = phone.Trim();
        OpeningHours = openingHours.Trim();
    }

    public void MarkAmbiguous()
    {
        IsAmbiguous = true;
    }
        
        
    public string ReportString(bool full)
    {
        return
            "VPVKAC office " +
            "`" + ShortName + "` " +
            (full && IsAmbiguous ? " disambiguated as `" + DisambiguatedName + "` " : "") +
            (full ? " (from: `" + Name + "`) " : "") +
            "at `" + Address + "`" +
            (full && OriginalAddress != null ? " (from: `" + OriginalAddress + "`)" : "");
    }

    public override string ToString() => ReportString(true);
}