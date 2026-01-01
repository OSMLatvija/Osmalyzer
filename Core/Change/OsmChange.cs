using System;
using System.Linq;
using System.Text;
using System.Xml;

namespace Osmalyzer;

public class OsmChange
{
    public IReadOnlyList<OsmChangeAction> Actions => _actions;

    
    private readonly List<OsmChangeAction> _actions;

    
    public OsmChange(List<OsmChangeAction> actions)
    {
        if (actions == null) throw new ArgumentNullException(nameof(actions));
        if (actions.Count == 0) throw new ArgumentException("Actions list cannot be empty.", nameof(actions));
        
        _actions = actions;
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
            OmitXmlDeclaration = false
        };

        StringBuilder stringBuilder = new StringBuilder();
        
        using (XmlWriter writer = XmlWriter.Create(stringBuilder, settings))
        {
            writer.WriteStartDocument();
            
            // Root element
            writer.WriteStartElement("osmChange");
            writer.WriteAttributeString("version", "0.6");
            writer.WriteAttributeString("generator", "Osmalyzer");
            
            // Convert user actions to XML-specific actions
            List<XmlAction> xmlActions = [ ];
            
            foreach (OsmChangeAction action in _actions)
            {
                switch (action)
                {
                    case OsmSetValueAction setValue:
                        // OsmSetValueAction generates a modify action
                        xmlActions.Add(new XmlModifyAction(setValue.Element, setValue.Changeset));
                        break;
                    
                    default:
                        throw new NotImplementedException($"Action type {action.GetType().Name} is not supported");
                }
            }
            
            // Group by action type
            List<XmlCreateAction> createActions = xmlActions.OfType<XmlCreateAction>().ToList();
            List<XmlModifyAction> modifyActions = xmlActions.OfType<XmlModifyAction>().ToList();
            List<XmlDeleteAction> deleteActions = xmlActions.OfType<XmlDeleteAction>().ToList();
            
            // Write create block
            if (createActions.Count > 0)
            {
                writer.WriteStartElement("create");
                foreach (XmlCreateAction action in createActions)
                    WriteElement(writer, action.Element, action.Changeset);
                writer.WriteEndElement();
            }
            
            // Write modify block
            if (modifyActions.Count > 0)
            {
                writer.WriteStartElement("modify");
                foreach (XmlModifyAction action in modifyActions)
                    WriteElement(writer, action.Element, action.Changeset);
                writer.WriteEndElement();
            }
            
            // Write delete block
            if (deleteActions.Count > 0)
            {
                // Group by if-unused flag
                List<XmlDeleteAction> normalDeletes = deleteActions.Where(d => !d.IfUnused).ToList();
                List<XmlDeleteAction> ifUnusedDeletes = deleteActions.Where(d => d.IfUnused).ToList();
                
                if (normalDeletes.Count > 0)
                {
                    writer.WriteStartElement("delete");
                    foreach (XmlDeleteAction action in normalDeletes)
                        WriteElement(writer, action.Element, action.Changeset);
                    writer.WriteEndElement();
                }
                
                if (ifUnusedDeletes.Count > 0)
                {
                    writer.WriteStartElement("delete");
                    writer.WriteAttributeString("if-unused", "true");
                    foreach (XmlDeleteAction action in ifUnusedDeletes)
                        WriteElement(writer, action.Element, action.Changeset);
                    writer.WriteEndElement();
                }
            }
            
            writer.WriteEndElement(); // osmChange
            writer.WriteEndDocument();
        }
        
        return stringBuilder.ToString();
    }

    
    private void WriteElement(XmlWriter writer, OsmElement element, long changeset)
    {
        switch (element)
        {
            case OsmNode node:
                WriteNode(writer, node, changeset);
                break;
            case OsmWay way:
                WriteWay(writer, way, changeset);
                break;
            case OsmRelation relation:
                WriteRelation(writer, relation, changeset);
                break;
        }
    }

    
    private void WriteNode(XmlWriter writer, OsmNode node, long changeset)
    {
        writer.WriteStartElement("node");
        writer.WriteAttributeString("id", node.Id.ToString());
        writer.WriteAttributeString("changeset", changeset.ToString());
        writer.WriteAttributeString("version", node.Version.ToString());
        writer.WriteAttributeString("lat", node.coord.lat.ToString("F7"));
        writer.WriteAttributeString("lon", node.coord.lon.ToString("F7"));
        
        WriteTags(writer, node);
        
        writer.WriteEndElement();
    }

    
    private void WriteWay(XmlWriter writer, OsmWay way, long changeset)
    {
        writer.WriteStartElement("way");
        writer.WriteAttributeString("id", way.Id.ToString());
        writer.WriteAttributeString("changeset", changeset.ToString());
        writer.WriteAttributeString("version", way.Version.ToString());
        
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

    
    private void WriteRelation(XmlWriter writer, OsmRelation relation, long changeset)
    {
        writer.WriteStartElement("relation");
        writer.WriteAttributeString("id", relation.Id.ToString());
        writer.WriteAttributeString("changeset", changeset.ToString());
        writer.WriteAttributeString("version", relation.Version.ToString());
        
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