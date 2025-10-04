namespace Osmalyzer;

public class CourthouseData
{
    public string Name { get; }
    
    public string Address { get; }


    public CourthouseData(string name, string address)
    {
        Name = name;
        Address = address;
    }


    public string ReportString()
    {
        return "Courthouse `" + Name + "` (`" + Address + "` )";
    }
}