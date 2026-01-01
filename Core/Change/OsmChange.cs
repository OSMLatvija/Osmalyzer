using System;
using System.Linq;
using System.Text;
using System.Xml;

namespace Osmalyzer;

public class OsmChange
{
    public IReadOnlyList<OsmChangeAction> Actions => _actions;
    
    
    private readonly List<OsmChangeAction> _actions;

    
    public OsmChange(OsmData data)
    {
        ArgumentNullException.ThrowIfNull(data);

        _actions = [];
        
        // todo:

        foreach (OsmElement element in data.Elements)
        {
            switch (element.State)
            {
                case OsmElementState.Live:
                    // No changes
                    break;
                
                case OsmElementState.Modified:
                    // We have to pass the whole element for modify actions with all the values, so we don't care about the actual change
                    _actions.Add(new OsmChangeModifyAction(element));
                    break;
                
                case OsmElementState.Deleted:
                    _actions.Add(new OsmChangeDeleteAction(element));
                    break;
                
                case OsmElementState.Created:
                    _actions.Add(new OsmChangeCreateAction(element));
                    break;
                
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }


    /// <summary>
    /// Generates osmChange XML format according to OSM specification
    /// </summary>
    [Pure]
    public string ToXml()
    {
        XmlWriterSettings settings = new XmlWriterSettings()
        {
            Indent = true,
            IndentChars = "    ",
            Encoding = Encoding.UTF8,
            OmitXmlDeclaration = true
        };

        StringBuilder stringBuilder = new StringBuilder();
        
        using (XmlWriter writer = XmlWriter.Create(stringBuilder, settings))
        {
            writer.WriteStartDocument();
            
            // Root element
            writer.WriteStartElement("osmChange");
            writer.WriteAttributeString("version", "0.6");
            writer.WriteAttributeString("generator", "Osmalyzer");

            // Group by action type
            List<OsmChangeCreateAction> createActions = _actions.OfType<OsmChangeCreateAction>().ToList();
            List<OsmChangeModifyAction> modifyActions = _actions.OfType<OsmChangeModifyAction>().ToList();
            List<OsmChangeDeleteAction> deleteActions = _actions.OfType<OsmChangeDeleteAction>().ToList();
            
            // Write create block
            if (createActions.Count > 0)
            {
                writer.WriteStartElement("create");
                foreach (OsmChangeCreateAction action in createActions)
                    WriteElement(writer, action.Element);
                writer.WriteEndElement();
            }
            
            // Write modify block
            if (modifyActions.Count > 0)
            {
                writer.WriteStartElement("modify");
                foreach (OsmChangeModifyAction action in modifyActions)
                    WriteElement(writer, action.Element);
                writer.WriteEndElement();
            }
            
            // Write delete block
            if (deleteActions.Count > 0)
            {
                // Group by if-unused flag
                List<OsmChangeDeleteAction> normalDeletes = deleteActions.Where(d => !d.IfUnused).ToList();
                List<OsmChangeDeleteAction> ifUnusedDeletes = deleteActions.Where(d => d.IfUnused).ToList();
                
                if (normalDeletes.Count > 0)
                {
                    writer.WriteStartElement("delete");
                    foreach (OsmChangeDeleteAction action in normalDeletes)
                        WriteElement(writer, action.Element); // todo: all of the values?
                    writer.WriteEndElement();
                }
                
                if (ifUnusedDeletes.Count > 0)
                {
                    writer.WriteStartElement("delete");
                    //writer.WriteAttributeString("if-unused", "true"); -- todo: figure out if this is wanted
                    foreach (OsmChangeDeleteAction action in ifUnusedDeletes)
                        WriteElement(writer, action.Element);
                    writer.WriteEndElement();
                }
            }
            
            writer.WriteEndElement(); // osmChange
            writer.WriteEndDocument();
        }
        
        return stringBuilder.ToString();
    }

    
    private void WriteElement(XmlWriter writer, OsmElement element)
    {
        switch (element)
        {
            case OsmNode node:
                WriteNode(writer, node);
                break;
            
            case OsmWay way:
                WriteWay(writer, way);
                break;
            
            case OsmRelation relation:
                WriteRelation(writer, relation);
                break;
        }
    }

    
    private void WriteNode(XmlWriter writer, OsmNode node)
    {
        writer.WriteStartElement("node");
        writer.WriteAttributeString("id", node.Id.ToString());
        if (node.Changeset > 0) writer.WriteAttributeString("changeset", node.Changeset.ToString());
        if (node.Version > 0) writer.WriteAttributeString("version", node.Version.ToString());
        writer.WriteAttributeString("lat", node.coord.lat.ToString("F7"));
        writer.WriteAttributeString("lon", node.coord.lon.ToString("F7"));
        
        WriteTags(writer, node);
        
        writer.WriteEndElement();
    }

    
    private void WriteWay(XmlWriter writer, OsmWay way)
    {
        writer.WriteStartElement("way");
        writer.WriteAttributeString("id", way.Id.ToString());
        if (way.Changeset > 0) writer.WriteAttributeString("changeset", way.Changeset.ToString());
        if (way.Version > 0) writer.WriteAttributeString("version", way.Version.ToString());
        
        // Write node references
        foreach (OsmNode node in way.Nodes)
        {
            writer.WriteStartElement("nd");
            writer.WriteAttributeString("ref", node.Id.ToString());
            writer.WriteEndElement();
        }
        
        WriteTags(writer, way);
        
        writer.WriteEndElement();
    }

    
    private void WriteRelation(XmlWriter writer, OsmRelation relation)
    {
        writer.WriteStartElement("relation");
        writer.WriteAttributeString("id", relation.Id.ToString());
        if (relation.Changeset > 0) writer.WriteAttributeString("changeset", relation.Changeset.ToString());
        if (relation.Version > 0) writer.WriteAttributeString("version", relation.Version.ToString());
        
        // Write members
        foreach (OsmRelationMember member in relation.Members)
        {
            writer.WriteStartElement("member");
            
            string memberType = member.Type switch
            {
                OsmElement.OsmElementType.Node => "node",
                OsmElement.OsmElementType.Way => "way",
                OsmElement.OsmElementType.Relation => "relation",
                _ => throw new ArgumentOutOfRangeException()
            };
            
            writer.WriteAttributeString("type", memberType);
            writer.WriteAttributeString("ref", member.Id.ToString());
            writer.WriteAttributeString("role", member.Role);
            writer.WriteEndElement();
        }
        
        WriteTags(writer, relation);
        
        writer.WriteEndElement();
    }

    
    private void WriteTags(XmlWriter writer, OsmElement element)
    {
        if (element.AllTags == null)
            return;
        
        foreach ((string key, string value) in element.AllTags)
        {
            writer.WriteStartElement("tag");
            writer.WriteAttributeString("k", key);
            writer.WriteAttributeString("v", value);
            writer.WriteEndElement();
        }
    }
}