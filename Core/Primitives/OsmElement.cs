using System;
using System.Linq;
using OsmSharp;
using OsmSharp.Tags;

namespace Osmalyzer;

/// <summary>
///
/// Note that the same <see cref="OsmData"/> will always have unique instances of <see cref="OsmElement"/>,
/// i.e. each ID has exactly one corresponding elements.
/// </summary>
public abstract class OsmElement : IChunkerItem
{
    [PublicAPI]
    public abstract OsmElementType ElementType { get; }

    [PublicAPI]
    public abstract string OsmViewUrl { get; }


    [PublicAPI]
    public long Id { get; }

    [PublicAPI]
    public long? Version { get; }
    
    [PublicAPI]
    public long? Changeset { get; }
    
    [PublicAPI]
    public OsmElementState State { get; set; } = OsmElementState.Live;
    
        
    [PublicAPI]
    public int KeyCount => _tags?.Count ?? 0;
        
    [PublicAPI]
    public IEnumerable<string>? AllKeys => _tags?.Keys;
        
    [PublicAPI]
    public IEnumerable<string>? AllValues => _tags?.Values;
    
    [PublicAPI]
    public IEnumerable<(string, string)>? AllTags => _tags?.Select(kv => (kv.Key, kv.Value));
        
    [PublicAPI]
    public bool HasAnyTags => _tags != null;
        
    [PublicAPI]
    public IReadOnlyList<OsmRelationMember>? Relations => relations?.AsReadOnly();
    
    /// <summary> Filled by <see cref="OsmData.Deduplicate"/>, if called and if duplicated </summary>
    [PublicAPI]
    public List<OsmElement>? Duplicates { get; private set; }
    
    /// <summary> Arbitrary user data that can be attached to this element. </summary>
    public object? UserData { get; set; }

   
    public (double x, double y) ChunkCoord => AverageCoord.ToCartesian();


    internal List<OsmRelationMember>? relations;


    private Dictionary<string, string>? _tags;


    protected OsmElement(long id)
    {
        Id = id;
        
        State = OsmElementState.Created;
    }

    protected OsmElement(OsmGeo rawElement)
    {
        Id = rawElement.Id!.Value;
        Version = rawElement.Version ?? throw new Exception();
        Changeset = rawElement.ChangeSetId ?? throw new Exception();
        // todo: value from OsmSharp is 0, I am not sure what it is supposed to be if I want to upload this afterwards

        if (rawElement.Tags != null)
        {
            if (rawElement.Tags.Count > 0)
            {
                _tags = new Dictionary<string, string>();

                foreach (Tag tag in rawElement.Tags)
                    _tags.Add(tag.Key, tag.Value);
            }
        }
    }

    /// <summary>
    /// Copy constructor for deep copying elements
    /// </summary>
    protected OsmElement(OsmElement original)
    {
        Id = original.Id;
        Version = original.Version;
        Changeset = original.Changeset;
        State = original.State;
        UserData = original.UserData is ICloneable clonable ? clonable.Clone() : original.UserData;

        // Deep copy tags
        if (original._tags != null)
        {
            _tags = new Dictionary<string, string>(original._tags);
        }

        // Deep copy duplicates list if present
        if (original.Duplicates != null)
        {
            Duplicates = new List<OsmElement>(original.Duplicates);
        }

        // Note: relations and backlinks are NOT copied here - they are handled separately in OsmData.Copy()
    }


    internal static OsmElement Create(OsmGeo element)
    {
        switch (element.Type)
        {
            case OsmGeoType.Node:     return new OsmNode(element);
            case OsmGeoType.Way:      return new OsmWay(element);
            case OsmGeoType.Relation: return new OsmRelation(element);
                
            default:
                throw new ArgumentOutOfRangeException();
        }
    }
        
        
    /// <summary>
    ///
    /// Can be null if not present, but can't be empty per OSM specs.
    /// </summary>
    [Pure]
    public string? GetValue(string key)
    {
        if (_tags == null)
            return null;
            
        _tags.TryGetValue(key, out string? value);
        return value;
    }  
        
    [Pure]
    public string[]? GetDelimitedValues(string key)
    {
        if (_tags == null)
            return null;
            
        return key.Split(';', StringSplitOptions.TrimEntries)
                  .Select(v => v.Trim())
                  .ToArray();
    }  
        
    [Pure]
    public List<(string, string)>? GetPrefixedValues(string keyPrefix)
    {
        if (_tags == null)
            return null;
            
        List<(string, string)> values = new List<(string, string)>();

        foreach ((string? key, string? value) in _tags)
            if (key.StartsWith(keyPrefix))
                values.Add((key, value));

        return values.Count > 0 ? values : null;
    }
        
    [Pure]
    public bool HasKey(string key)
    {
        if (_tags == null)
            return false;
            
        return _tags.ContainsKey(key);
    }
        
    [Pure]
    public bool HasKeyPrefixed(string keyPrefix)
    {
        if (_tags == null)
            return false;
            
        foreach ((string? key, string? _) in _tags)
            if (key.StartsWith(keyPrefix))
                return true;

        return false;
    }
        
    [Pure]
    public bool HasValue(string key, string value, bool caseSensitive = true)
    {
        if (_tags == null)
            return false;

        if (!_tags.TryGetValue(key, out string? actualValue))
            return false;
            
        if (caseSensitive)
            return value == actualValue;
        else
            return string.Equals(value, actualValue, StringComparison.OrdinalIgnoreCase);
    }
    
    [Pure]
    public bool HasValue(string key, params string[] values)
    {
        if (values.Length == 0) throw new ArgumentException("At least one value must be provided.", nameof(values));
        
        return HasValue(key, (IEnumerable<string>)values);
    }

    [Pure]
    public bool HasValue(string key, IEnumerable<string> values, bool caseSensitive = true)
    {
        if (_tags == null)
            return false;

        if (!_tags.TryGetValue(key, out string? actualValue))
            return false;

        foreach (string value in values)
        {
            if (caseSensitive)
            {
                if (value == actualValue)
                    return true;
            }
            else
            {
                if (string.Equals(value, actualValue, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
        }
        
        return false;
    } 
        
    [Pure]
    public bool HasDelimitedValue(string key, string value)
    {
        if (_tags == null)
            return false;
            
        if (!_tags.TryGetValue(key, out string? actualValue))
            return false;

        return actualValue.Split(';')
                          .Select(v => v.Trim())
                          .Any(v => v == value);
    }  

    [Pure]
    public string? GetAllTagsAsString()
    {
        if (_tags == null)
            return null;

        string s = "";

        foreach (KeyValuePair<string, string> tag in _tags)
        {
            string value = tag.Value;

            // "Remove" newlines from value
            // (OSM package seems to convert literal "\n" string in tag value into newline)
            value = value.Replace("\r\n", "↲");
            value = value.Replace("\r", "↲");
            value = value.Replace("\n", "↲");

            s += tag.Key + "=" + value + Environment.NewLine;
        }

        return s;
    }
    
    public void SetValue(string key, string value)
    {
        if (_tags == null)
            _tags = new Dictionary<string, string>();

        if (_tags.TryGetValue(key, out string? existingValue))
            if (existingValue == value)
                return;

        State = OsmElementState.Modified;
        
        _tags[key] = value;
    }
    
    public void RemoveKey(string key)
    {
        if (_tags == null || !_tags.ContainsKey(key))
            return;

        State = OsmElementState.Modified;
        
        _tags.Remove(key);

        if (_tags.Count == 0)
            _tags = null;
    }
    

    public abstract OsmCoord AverageCoord { [Pure] get; }

    
    internal void AddDuplicates(OsmElement duplicate)
    {
        if (Duplicates == null)
            Duplicates = [ ];
        
        AddDuplicatesRecursive(Duplicates, duplicate);
        

        void AddDuplicatesRecursive(List<OsmElement> list, OsmElement targetDupe)
        {
            list.Add(targetDupe);

            if (targetDupe.Duplicates != null)
                foreach (OsmElement dupe in targetDupe.Duplicates)
                    AddDuplicatesRecursive(list, dupe);
        }
    }


    /// <summary>
    /// Speeding up collections with hashing, basically dictionaries
    /// </summary>
    public override int GetHashCode()
    {
        unchecked
        {
            return (int)Id ^ (int)(Id >> 32);
        }
    }


    public enum OsmElementType
    {
        Node,
        Way,
        Relation
    }
}