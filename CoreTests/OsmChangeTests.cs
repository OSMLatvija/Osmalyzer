using System.Reflection;
using System.Xml;
using NUnit.Framework;
using OsmSharp;
using OsmSharp.Tags;

namespace Osmalyzer;

[TestFixture]
public class OsmChangeTests
{
    private static OsmNode CreateTestNode(long id, int version, double lat, double lon, TagsCollection? tags = null)
    {
        Node rawNode = new Node()
        {
            Id = id,
            Version = version,
            Latitude = lat,
            Longitude = lon,
            Tags = tags
        };
        
        // Use reflection to call internal Create method
        MethodInfo? createMethod = typeof(OsmElement).GetMethod("Create", BindingFlags.NonPublic | BindingFlags.Static);
        if (createMethod == null) throw new Exception("Cannot find OsmElement.Create method");
        
        OsmElement? element = (OsmElement?)createMethod.Invoke(null, [ rawNode ]);
        if (element == null) throw new Exception("Failed to create OsmElement");
        
        return (OsmNode)element;
    }

    [Test]
    public void TestOsmChangeXmlGeneration()
    {
        // Arrange
        
        // Create a test node
        TagsCollection tags = new TagsCollection()
        {
            { "amenity", "school" }
        };
        
        OsmNode node = CreateTestNode(1234, 2, 12.1234567, -8.7654321, tags);
        
        OsmChangeAction[] actions = 
        [
            new OsmSetValueAction(node, 42, "amenity", "school")
        ];
        
        OsmChange osmChange = new OsmChange(actions.ToList());

        // Act
        string xml = osmChange.ToXml();

        // Assert
        Assert.That(xml, Is.Not.Null);
        Assert.That(xml, Is.Not.Empty);
        
        // Verify it's valid XML
        XmlDocument doc = new XmlDocument();
        Assert.DoesNotThrow(() => doc.LoadXml(xml));
        
        // Verify structure
        XmlNode? osmChangeNode = doc.SelectSingleNode("/osmChange");
        Assert.That(osmChangeNode, Is.Not.Null);
        Assert.That(osmChangeNode!.Attributes!["version"]!.Value, Is.EqualTo("0.6"));
        Assert.That(osmChangeNode.Attributes["generator"]!.Value, Is.EqualTo("Osmalyzer"));
        
        XmlNode? modifyNode = osmChangeNode.SelectSingleNode("modify");
        Assert.That(modifyNode, Is.Not.Null);
        
        XmlNode? nodeElement = modifyNode!.SelectSingleNode("node");
        Assert.That(nodeElement, Is.Not.Null);
        Assert.That(nodeElement!.Attributes!["id"]!.Value, Is.EqualTo("1234"));
        Assert.That(nodeElement.Attributes["changeset"]!.Value, Is.EqualTo("42"));
        Assert.That(nodeElement.Attributes["version"]!.Value, Is.EqualTo("2"));
        Assert.That(nodeElement.Attributes["lat"]!.Value, Is.EqualTo("12.1234567"));
        Assert.That(nodeElement.Attributes["lon"]!.Value, Is.EqualTo("-8.7654321"));
        
        XmlNode? tagNode = nodeElement.SelectSingleNode("tag[@k='amenity']");
        Assert.That(tagNode, Is.Not.Null);
        Assert.That(tagNode!.Attributes!["v"]!.Value, Is.EqualTo("school"));
    }

    [Test]
    public void TestOsmChangeMultipleActions()
    {
        // Arrange
        OsmNode node1 = CreateTestNode(1001, 1, -33.9133123, 151.1173123);
        OsmNode node2 = CreateTestNode(1002, 2, -33.9233321, 151.1173321);
        
        OsmChangeAction[] actions = 
        [
            new OsmSetValueAction(node1, 43, "name", "Test 1"),
            new OsmSetValueAction(node2, 43, "name", "Test 2")
        ];
        
        OsmChange osmChange = new OsmChange(actions.ToList());

        // Act
        string xml = osmChange.ToXml();

        // Assert
        Assert.That(xml, Is.Not.Null);
        
        XmlDocument doc = new XmlDocument();
        doc.LoadXml(xml);
        
        XmlNode? modifyNode = doc.SelectSingleNode("/osmChange/modify");
        Assert.That(modifyNode, Is.Not.Null);
        
        XmlNodeList? nodeElements = modifyNode!.SelectNodes("node");
        Assert.That(nodeElements, Is.Not.Null);
        Assert.That(nodeElements, Has.Count.EqualTo(2));
    }

    [Test]
    public void TestOsmChangeSetValueAction()
    {
        // Arrange
        TagsCollection tags = new TagsCollection()
        {
            { "name", "Old Name" },
            { "amenity", "cafe" }
        };
        
        OsmNode node = CreateTestNode(5000, 5, 50.0, 10.0, tags);
        
        OsmChangeAction[] actions = 
        [
            new OsmSetValueAction(node, 44, "name", "New Name")
        ];
        
        OsmChange osmChange = new OsmChange(actions.ToList());

        // Act
        string xml = osmChange.ToXml();

        // Assert
        XmlDocument doc = new XmlDocument();
        doc.LoadXml(xml);
        
        // Should be in modify block
        XmlNode? modifyNode = doc.SelectSingleNode("/osmChange/modify");
        Assert.That(modifyNode, Is.Not.Null);
        
        XmlNode? nodeElement = modifyNode!.SelectSingleNode("node");
        Assert.That(nodeElement, Is.Not.Null);
        Assert.That(nodeElement!.Attributes!["id"]!.Value, Is.EqualTo("5000"));
        
        // All tags should be present (full element replacement)
        XmlNodeList? tagNodes = nodeElement.SelectNodes("tag");
        Assert.That(tagNodes, Is.Not.Null);
        Assert.That(tagNodes, Has.Count.EqualTo(2));
    }
}

