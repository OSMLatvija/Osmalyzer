namespace Osmalyzer;

public class VPVKACOffice
{
    public string Name { get; }
    
    public string ShortName { get; }
    
    public string DisambiguatedName { get; }

    public string DisplayName => IsAmbiguous ? DisambiguatedName : ShortName;
    
    public bool IsAmbiguous { get; private set; }
    
    public string Address { get; }
    
    public string Email { get; }
    
    public string Phone { get; }
    
    public string OpeningHours { get; }


    public VPVKACOffice(string name, string shortName, string disambiguatedName, string address, string email, string phone, string openingHours)
    {
        Name = name.Trim();
        ShortName = shortName.Trim();
        DisambiguatedName = disambiguatedName.Trim();
        Address = address;
        Email = email.Trim();
        Phone = phone.Trim();
        OpeningHours = openingHours.Trim();
    }

    public void MarkAmbiguous()
    {
        IsAmbiguous = true;
    }
        
        
    public string ReportString()
    {
        return
            "VPVKAC office " +
            "`" + Name + "` " +
            "at `" + Address + "`";
    }
}