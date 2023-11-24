using System;
using JetBrains.Annotations;
using OsmSharp;

namespace Osmalyzer;

public class OsmRelationMember
{
    [PublicAPI]
    public OsmRelation Owner { get; }

    [PublicAPI]
    public long Id { get; }

    /// <summary>
    ///
    /// This will be null if the data did not contain the element.
    /// </summary>
    [PublicAPI]
    public OsmElement? Element { get; internal set; }
        
    [PublicAPI]
    public string Role { get; }

        
    internal MemberElementType ElementType { get; }


    internal OsmRelationMember(OsmRelation owner, OsmGeoType rawType, long id, string role)
    {
        Owner = owner;
        Id = id;
        Role = role;
            
        ElementType = RawTypeToOurType(rawType);
    }

        
    private MemberElementType RawTypeToOurType(OsmGeoType rawType)
    {
        switch (rawType)
        {
            case OsmGeoType.Node:     return MemberElementType.Node;
            case OsmGeoType.Way:      return MemberElementType.Way;
            case OsmGeoType.Relation: return MemberElementType.Relation;
                
            default: throw new InvalidOperationException();
        }
    }


    /// <summary>
    /// Temporary while loading
    /// </summary>
    internal enum MemberElementType
    {
        Node,
        Way,
        Relation
    }
}