using NUnit.Framework;

namespace Osmalyzer.Tests;

public class Tests
{
    [Test]
    public void TestGetAllClosest()
    {
        List<TestItem> nodes = new List<TestItem>()
        {
            new TestItem(5, 5),
            new TestItem(15, 15),
            new TestItem(15, 5),
            new TestItem(5, 15),
            new TestItem(50, 50)
        };
        
        Chunker<TestItem> chunker = new Chunker<TestItem>(nodes);

        List<TestItem> closest = chunker.GetAllClosest((6, 8), 20);

        CollectionAssert.AreEqual(new [] { nodes[0], nodes[3], nodes[2], nodes[1] }, closest);
    }


    private class TestItem : IChunkerItem
    {
        public (double x, double y) ChunkCoord => (_x, _y);

        private int _x;
        private int _y;

        public TestItem(int x, int y)
        {
            _x = x;
            _y = y;
        }
    }
}