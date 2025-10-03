namespace Osmalyzer;

public class VPVKACOffice
{
    public string Name { get; }
    
    public string ShortName { get; }
    
    public string DisambiguatedName { get; }
    
    public VPVKACAddress Address { get; }
    
    public string Email { get; }
    
    public string Phone { get; }
    
    public string OpeningHours { get; }


    public VPVKACOffice(string name, string shortName, string disambiguatedName, VPVKACAddress address, string email, string phone, string openingHours)
    {
        Name = name.Trim();
        ShortName = shortName.Trim();
        DisambiguatedName = disambiguatedName.Trim();
        Address = address;
        Email = email.Trim();
        Phone = phone.Trim();
        OpeningHours = openingHours.Trim();
    }
        
        
    public string ReportString()
    {
        return
            "VPVKAC office " +
            "`" + Name + "` " +
            "at `" + Address + "`";
    }


    public record VPVKACAddress(string Name, string Location, string? Pagasts, string? Novads, string PostalCode)
    {
        public override string ToString() => ToString(false);

        public string ToString(bool full)
        {
            if (full)
                return Name.Trim() +
                       ", " + Location.Trim() +
                       (Pagasts != null ? ", " + Pagasts.Trim() : "") +
                       (Novads != null ? ", " + Novads.Trim() : "") +
                       ", " + PostalCode;
            else
                return Name + 
                       ", " + Location.Trim() + 
                       ", " + PostalCode.Trim();
        }
    }
}