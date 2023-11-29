namespace Osmalyzer;

public interface IResolvableWithItem
{
    /// <summary>
    /// A unique ID for the data item, presumably a hash of its defining data.
    /// This should include any data which cannot change or it becomes a different item, e.g. coordinate, name, address, etc. 
    /// This should not include data that is not relevant to the issue, e.g. opening times, roof color, etc.
    /// </summary>
    string Item { get; }
}