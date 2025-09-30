using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
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

   
    public (double x, double y) ChunkCoord => AverageCoord.ToCartesian();


    internal List<OsmRelationMember>? relations;


    private readonly Dictionary<string, string>? _tags;


    protected OsmElement(OsmGeo rawElement)
    {
        Id = rawElement.Id!.Value;

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


    public abstract OsmCoord AverageCoord { [Pure] get; }


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