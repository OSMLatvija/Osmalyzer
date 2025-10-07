namespace Osmalyzer;

public class CourthouseData
{
    public string Name { get; }
    
    public string Address { get; }
    
    public string? LocationHint { get; }
    
    public List<string> Phones { get; }
    
    public string Email { get; }
    
    public string OpeningHours { get; }


    public CourthouseData(string name, string address, string? locationHint, List<string> phones, string email, string openingHours)
    {
        Name = name;
        Address = address;
        LocationHint = locationHint;
        Phones = phones;
        Email = email;
        OpeningHours = openingHours;
    }


    public string ReportString()
    {
        return
            "Courthouse `" + Name + "` " +
            (LocationHint != null ? "(located `" + LocationHint + "`) " : "") +
            "at `" + Address + "`" +
            " with phone(s) " + string.Join(", ", Phones.Select(p => "`" + p + "`")) +
            " and email `" + Email + "`" +
            " and opening hours `" + OpeningHours + "`";
    }
}