namespace Osmalyzer;

public class CourthouseData
{
    public string Name { get; }
    
    public string Address { get; }
    
    public string? LocationHint { get; }
    
    public List<string> Phones { get; }
    
    public string? Email { get; }


    public CourthouseData(string name, string address, string? locationHint, List<string> phones, string? email)
    {
        Name = name;
        Address = address;
        LocationHint = locationHint;
        Phones = phones;
        Email = email;
    }


    public string ReportString()
    {
        return 
            "Courthouse `" + Name + "` " +
            (LocationHint != null ? "(located `" + LocationHint + "`) " : "") +
            "at `" + Address + "`" +
            (Phones.Count > 0 ? " with phone(s) " + string.Join(", ", Phones.Select(p => "`" + p + "`")) : "") +
            (Email != null ? " and email `" + Email + "`" : "");
    }
}