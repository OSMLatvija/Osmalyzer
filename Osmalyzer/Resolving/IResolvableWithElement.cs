namespace Osmalyzer;

public interface IResolvableWithElement
{
    /// <summary>
    /// A unique ID for the OSM element, presumably a hash of its defining tags.
    /// This should include any data which cannot change or it becomes a different item, e.g. coordinate, name, address, etc. 
    /// This should not include data that is not relevant to the issue, e.g. opening times, roof color, etc.
    /// </summary>
    string Element { get; }
}