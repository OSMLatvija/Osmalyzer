namespace Osmalyzer.Tests;

public class ChunkerTests
{
    [TestCase(true)]
    [TestCase(false)]
    public void TestGetClosestLimited(bool chunked)
    {
        List<TestItem> nodes = new List<TestItem>()
        {
            new TestItem(5, 5),
            new TestItem(15, 15),
            new TestItem(15, 5),
            new TestItem(5, 15),
            new TestItem(30, 30)
        };
        
        Chunker<TestItem> chunker = new Chunker<TestItem>(nodes, chunked);

        TestItem? closest = chunker.GetClosest((12, 3), 20);

        Assert.That(closest, Is.Not.Null);
        Assert.That(closest, Is.EqualTo(nodes[2]));
    }    
    
    [TestCase(true)]
    [TestCase(false)]
    public void TestGetClosestUnlimited(bool chunked)
    {
        List<TestItem> nodes = new List<TestItem>()
        {
            new TestItem(5, 5),
            new TestItem(15, 15),
            new TestItem(15, 5),
            new TestItem(5, 15),
            new TestItem(30, 30)
        };
        
        Chunker<TestItem> chunker = new Chunker<TestItem>(nodes, chunked);

        TestItem? closest = chunker.GetClosest((12, 3));

        Assert.That(closest, Is.Not.Null);
        Assert.That(closest, Is.EqualTo(nodes[2]));
    }    
    
    [TestCase(true)]
    [TestCase(false)]
    public void TestGetAllClosest(bool chunked)
    {
        List<TestItem> nodes = new List<TestItem>()
        {
            new TestItem(5, 5),
            new TestItem(15, 15),
            new TestItem(15, 5),
            new TestItem(5, 15),
            new TestItem(30, 30)
        };
        
        Chunker<TestItem> chunker = new Chunker<TestItem>(nodes, chunked);

        List<TestItem> closest = chunker.GetAllClosest((6, 8), 20);

        Assert.That(closest, Is.Not.Null);
        Assert.That(closest, Is.EqualTo(new[] { nodes[0], nodes[3], nodes[2], nodes[1] }));
    }


    private class TestItem : IChunkerItem
    {
        public (double x, double y) ChunkCoord => (_x, _y);

        
        private readonly int _x;
        private readonly int _y;

        
        public TestItem(int x, int y)
        {
            _x = x;
            _y = y;
        }

        
        public override string ToString() => "(" + _x.ToString("F1") + ", " + _y.ToString("F1") + ")";
    }
}