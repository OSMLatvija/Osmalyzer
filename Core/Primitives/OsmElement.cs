using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using OsmSharp;
using OsmSharp.Tags;

namespace Osmalyzer;

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
    public bool HasAnyTags => _tags != null;
        
    [PublicAPI]
    public IReadOnlyList<OsmRelationMember>? Relations => relations?.AsReadOnly();

   
    public (double x, double y) ChunkCoord => GetAverageCoord().ToCartesian();


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
    public string? GetAllTagsAsString()
    {
        if (_tags == null)
            return null;

        string s = "";

        foreach (KeyValuePair<string, string> tag in _tags)
            s += tag.Key + "=" + tag.Value + Environment.NewLine;

        return s;
    }


    [Pure]
    public abstract OsmCoord GetAverageCoord();
        

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