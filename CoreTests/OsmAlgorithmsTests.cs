namespace Osmalyzer.Tests;

[TestFixture]
public class OsmAlgorithmsTests
{
    [Test]
    public void IsChained_Way_Node_Terminal_True()
    {
        OsmNode n1 = TestOsmElementBuilder.CreateNode();
        OsmNode n2 = TestOsmElementBuilder.CreateNode();
        OsmWay w = TestOsmElementBuilder.CreateWay(n1, n2);

        Assert.That(OsmAlgorithms.IsChained(w, n1), Is.True);
        Assert.That(OsmAlgorithms.IsChained(w, n2), Is.True);
    }

    [Test]
    public void IsChained_Node_Way_Terminal_True()
    {
        OsmNode n1 = TestOsmElementBuilder.CreateNode();
        OsmNode n2 = TestOsmElementBuilder.CreateNode();
        OsmWay w = TestOsmElementBuilder.CreateWay(n1, n2);

        Assert.That(OsmAlgorithms.IsChained(n1, w), Is.True);
        Assert.That(OsmAlgorithms.IsChained(n2, w), Is.True);
    }

    [Test]
    public void IsChained_Way_Node_NonTerminal_False()
    {
        OsmNode n1 = TestOsmElementBuilder.CreateNode();
        OsmNode n2 = TestOsmElementBuilder.CreateNode();
        OsmNode n3 = TestOsmElementBuilder.CreateNode();
        OsmWay w = TestOsmElementBuilder.CreateWay(n1, n2, n3);

        Assert.That(OsmAlgorithms.IsChained(w, n2), Is.False);
        Assert.That(OsmAlgorithms.IsChained(n2, w), Is.False);
    }

    [Test]
    public void IsChained_Way_Way_ShareTerminal_True()
    {
        OsmNode n1 = TestOsmElementBuilder.CreateNode();
        OsmNode n2 = TestOsmElementBuilder.CreateNode();
        OsmNode n3 = TestOsmElementBuilder.CreateNode();

        OsmWay w1 = TestOsmElementBuilder.CreateWay(n1, n2);
        OsmWay w2 = TestOsmElementBuilder.CreateWay(n2, n3);

        Assert.That(OsmAlgorithms.IsChained(w1, w2), Is.True);
        Assert.That(OsmAlgorithms.IsChained(w2, w1), Is.True);
    }

    [Test]
    public void IsChained_Way_Way_ShareNonTerminal_False()
    {
        OsmNode n1 = TestOsmElementBuilder.CreateNode();
        OsmNode n2 = TestOsmElementBuilder.CreateNode();
        OsmNode n3 = TestOsmElementBuilder.CreateNode();
        OsmNode n4 = TestOsmElementBuilder.CreateNode();

        // Share n2, which is non-terminal of w1
        OsmWay w1 = TestOsmElementBuilder.CreateWay(n1, n2, n3);
        OsmWay w2 = TestOsmElementBuilder.CreateWay(n2, n4);

        Assert.That(OsmAlgorithms.IsChained(w1, w2), Is.False);
        Assert.That(OsmAlgorithms.IsChained(w2, w1), Is.False);
    }

    [Test]
    public void IsChained_Way_Node_Way_True()
    {
        OsmNode n1 = TestOsmElementBuilder.CreateNode();
        OsmNode n2 = TestOsmElementBuilder.CreateNode();
        OsmNode n3 = TestOsmElementBuilder.CreateNode();

        OsmWay w1 = TestOsmElementBuilder.CreateWay(n1, n2);
        OsmWay w2 = TestOsmElementBuilder.CreateWay(n2, n3);

        Assert.That(OsmAlgorithms.IsChained(w1, n2, w2), Is.True);
        // IEnumerable<OsmElement> overload
        Assert.That(OsmAlgorithms.IsChained(w1, n2, w2), Is.True);
    }

    [Test]
    public void IsChained_Way_Way_Way_True()
    {
        OsmNode n1 = TestOsmElementBuilder.CreateNode();
        OsmNode n2 = TestOsmElementBuilder.CreateNode();
        OsmNode n3 = TestOsmElementBuilder.CreateNode();
        OsmNode n4 = TestOsmElementBuilder.CreateNode();

        OsmWay w1 = TestOsmElementBuilder.CreateWay(n1, n2);
        OsmWay w2 = TestOsmElementBuilder.CreateWay(n2, n3);
        OsmWay w3 = TestOsmElementBuilder.CreateWay(n3, n4);

        Assert.That(OsmAlgorithms.IsChained(w1, w2, w3), Is.True);
        Assert.That(OsmAlgorithms.IsChained(w1, w2, w3), Is.True);
    }

    [Test]
    public void IsChained_AdjacentNodes_False()
    {
        OsmNode n1 = TestOsmElementBuilder.CreateNode();
        OsmNode n2 = TestOsmElementBuilder.CreateNode();

        Assert.That(OsmAlgorithms.IsChained(n1, n2), Is.False);

        OsmWay w = TestOsmElementBuilder.CreateWay(n1, n2);
        Assert.That(OsmAlgorithms.IsChained(w, n1, n2), Is.False);
        Assert.That(OsmAlgorithms.IsChained(n1, n2, w), Is.False);
    }

    [Test]
    public void IsChained_Relation_Anywhere_False()
    {
        OsmNode n1 = TestOsmElementBuilder.CreateNode();
        OsmNode n2 = TestOsmElementBuilder.CreateNode();
        OsmWay w = TestOsmElementBuilder.CreateWay(n1, n2);

        OsmRelation r = TestOsmElementBuilder.CreateRelation();

        Assert.That(OsmAlgorithms.IsChained(r), Is.False);
        Assert.That(OsmAlgorithms.IsChained(w, r), Is.False);
        Assert.That(OsmAlgorithms.IsChained(r, w), Is.False);
        Assert.That(OsmAlgorithms.IsChained(w, r, w), Is.False);
    }

    [Test]
    public void IsChained_Empty_False()
    {
        Assert.That(OsmAlgorithms.IsChained(), Is.False);
    }
}