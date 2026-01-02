namespace Osmalyzer;

public class Equivalator<T1, T2> where T1 : IDataItem 
                                 where T2 : IDataItem
{
    private readonly IEnumerable<T1> _items1;
    private readonly IEnumerable<T2> _items2;

    private List<(T1, T2)>? _matches;
    
    
    public Equivalator(IEnumerable<T1> items1, IEnumerable<T2> items2)
    {
        _items1 = items1;
        _items2 = items2;
    }


    public void MatchItemsByValues<TD>(Func<T1, TD> item1ValueGetter, Func<T2, TD> item2ValueGetter) where TD : notnull
    {
        if (_matches != null) throw new InvalidOperationException("Items have already been matched.");
        
        _matches = [ ];
        
        Dictionary<TD, T1> item1ValueMap = new Dictionary<TD, T1>();
        foreach (T1 item1 in _items1)
            if (!item1ValueMap.TryAdd(item1ValueGetter(item1), item1))
                throw new Exception($"Duplicate value '{item1ValueGetter(item1)}' found in first item collection");
        
        Dictionary<TD, T2> item2ValueMap = new Dictionary<TD, T2>();
        foreach (T2 item2 in _items2)
            if (!item2ValueMap.TryAdd(item2ValueGetter(item2), item2))
                throw new Exception($"Duplicate value '{item2ValueGetter(item2)}' found in second item collection");
        
        foreach (T1 item1 in _items1)
            if (item2ValueMap.TryGetValue(item1ValueGetter(item1), out T2? item2))
                _matches.Add((item1, item2));
    }

    [Pure]
    public Dictionary<T1, T2> AsDictionary()
    {
        if (_matches == null) throw new InvalidOperationException("Items have not been matched yet.");
        
        Dictionary<T1, T2> result = new Dictionary<T1, T2>();
        
        foreach ((T1 item1, T2 item2) in _matches)
            result[item1] = item2;
        
        return result;
    }
}