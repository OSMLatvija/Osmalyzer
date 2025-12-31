using OsmSharp;
using OsmSharp.Streams;
using OsmSharp.Tags;

namespace Osmalyzer;

public static class OsmUploader
{
    public static void CreateChangesetFromSuggestedChanges(List<SuggestedChange> suggestedChanges)
    {
        if (suggestedChanges == null) throw new ArgumentNullException(nameof(suggestedChanges));
        if (suggestedChanges.Count == 0) throw new ArgumentException("No suggested changes provided", nameof(suggestedChanges));
        
        
        using FileStream fileStream = File.Create("suggested changes.osm.xml");
        
        XmlOsmStreamTarget target = new XmlOsmStreamTarget(fileStream);
        
        // todo: THIS ISNT CHANGE, THIS IS PLAIN XML so stuff like JOSM doesn't understand these are changes
        
        target.Initialize();

        // todo: keep list, we might modify the same element multiple times
        
        foreach (SuggestedChange suggestedChange in suggestedChanges)
        {
            switch (suggestedChange)
            {
                case AddValueSuggested addValue:
                    OsmGeo osmGeo = ConvertToOsmGeo(addValue.Element);

                    osmGeo.Tags.Add(addValue.Key, addValue.Value);
                    // todo: or update? what if multiple suggestions for same key?
                    
                    AddGeoToTarget(target, osmGeo);
                    break;
                
                default:
                    throw new ArgumentOutOfRangeException(nameof(suggestedChange));
            }
        }

        target.Flush();
        
        target.Close();
    }

    private static void AddGeoToTarget(XmlOsmStreamTarget target, OsmGeo osmGeo)
    {
        switch (osmGeo)
        {
            case Node node:
                target.AddNode(node);
                break;
            
            case Relation relation:
                target.AddRelation(relation);
                break;
            
            case Way way:
                target.AddWay(way);
                break;
            
            default:
                throw new ArgumentOutOfRangeException(nameof(osmGeo));
        }
    }

    private static OsmGeo ConvertToOsmGeo(OsmElement element)
    {
        switch (element)
        {
            case OsmNode osmNode:
                return new Node()
                {
                    Id = osmNode.Id,
                    Version = osmNode.Version + 1,
                    Latitude = osmNode.coord.lat,
                    Longitude = osmNode.coord.lon,
                    Tags = new TagsCollection(osmNode.AllTags?.ToDictionary() ?? [])
                };
                
            case OsmWay osmWay:
                return new Way()
                {
                    Id = osmWay.Id,
                    Version = osmWay.Version + 1,
                    Nodes = osmWay.Nodes.Select(n => n.Id).ToArray(),
                    Tags = new TagsCollection(osmWay.AllTags?.ToDictionary() ?? [])
                };
            
            case OsmRelation osmRelation:
                RelationMember[] members = osmRelation.Members.Select(m => new RelationMember()
                {
                    Type = GetOsmGeoType(m),
                    Id = m.Id,
                    Role = m.Role
                }).ToArray();
                
                return new Relation()
                {
                    Id = osmRelation.Id,
                    Version = osmRelation.Version + 1,
                    Members = members,
                    Tags = new TagsCollection(osmRelation.AllTags?.ToDictionary() ?? [])
                };
            
            default:
                throw new ArgumentOutOfRangeException(nameof(element));
        }
    }

    private static OsmGeoType GetOsmGeoType(OsmRelationMember member)
    {
        // Since ElementType is internal, we check the Element property (which may be null if not loaded)
        // or infer from the member ID and context
        if (member.Element != null)
        {
            switch (member.Element)
            {
                case OsmNode:     return OsmGeoType.Node;
                case OsmWay:      return OsmGeoType.Way;
                case OsmRelation: return OsmGeoType.Relation;
                default:          throw new InvalidOperationException($"Unknown element type: {member.Element.GetType()}");
            }
        }
        
        // If Element is null, we cannot determine the type - this shouldn't happen for loaded relations
        throw new InvalidOperationException($"Cannot determine type for relation member {member.Id} - Element is null");
    }
}