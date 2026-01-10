namespace Osmalyzer.Tests;

[TestFixture]
public class OsmEditingTests
{
    // todo: all the stuff

    // todo: specifically also
    // todo: temp ids

    [Test]
    public void TestCreateNode()
    {
        // Arrange

        OsmData osmData = new OsmData();

        OsmCoord coord = new OsmCoord(1, 2);

        // Act

        OsmNode newNode = osmData.CreateNewNode(coord);

        // Assert

        Assert.That(newNode, Is.Not.Null);
        Assert.That(newNode.State, Is.EqualTo(OsmElementState.Created));
        Assert.That(newNode.coord, Is.EqualTo(coord));
        Assert.That(osmData.Nodes, Is.EquivalentTo([ newNode ]));
    }

    [Test]
    public void TestDeleteNode()
    {
        // Arrange

        OsmData osmData = new OsmData();
        OsmNode node = osmData.CreateNewNode(new OsmCoord(1, 2));

        // Act

        osmData.DeleteNode(node);

        // Assert

        Assert.That(node.State, Is.EqualTo(OsmElementState.Deleted));
        Assert.That(osmData.Nodes, Is.Empty);
    }

    [Test]
    public void TestRestoreNode()
    {
        // Arrange

        OsmData osmData = new OsmData();
        OsmNode node = osmData.CreateNewNode(new OsmCoord(1, 2));
        osmData.DeleteNode(node);

        // Act

        osmData.RestoreNode(node);

        // Assert

        Assert.That(node.State, Is.EqualTo(OsmElementState.Created));
        Assert.That(osmData.Nodes, Is.EquivalentTo([ node ]));
    }
}