namespace Osmalyzer;

public class CourthouseData
{
    public string Name { get; }
    
    public string Address { get; }
    
    public string? LocationHint { get; }


    public CourthouseData(string name, string address, string? locationHint)
    {
        Name = name;
        Address = address;
        LocationHint = locationHint;
    }


    public string ReportString()
    {
        return 
            "Courthouse `" + Name + "` " +
            (LocationHint != null ? "(located `" + LocationHint + "`) " : "") +
            "at `" + Address + "`";
    }
}