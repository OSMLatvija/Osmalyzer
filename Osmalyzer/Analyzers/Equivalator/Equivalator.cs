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


    public void MatchItems(Func<T1, T2, bool> matcher)
    {
        if (_matches != null) throw new InvalidOperationException("Items have already been matched.");
        
        _matches = [ ];

        List<T1> items1 = _items1.ToList();
        List<T2> items2 = _items2.ToList();

        for (int i1 = 0; i1 < items1.Count; i1++)
        {
            T1 item1 = items1[i1];

            for (int i2 = 0; i2 < items2.Count; i2++)
            {
                T2 item2 = items2[i2];
                
                if (matcher(item1, item2))
                {
                    _matches.Add((item1, item2));
                    items1.RemoveAt(i1);
                    i1--;
                    items2.RemoveAt(i2);
                    break;
                }
            }
        }
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