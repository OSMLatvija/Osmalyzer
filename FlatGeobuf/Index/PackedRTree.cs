using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using NetTopologySuite.Geometries;

namespace FlatGeobuf.Index
{
    public class PackedRTree
    {
        private const ulong NODE_ITEM_LEN = 8 * 4 + 8;

        public delegate Stream ReadNode(ulong offset, ulong length);

        public static ulong CalcSize(ulong numItems, ushort nodeSize)
        {
            if (nodeSize < 2)
                throw new Exception("Node size must be at least 2");
            if (numItems == 0)
                throw new Exception("Number of items must be greater than 0");
            ushort nodeSizeMin = Math.Min(Math.Max(nodeSize, (ushort) 2), (ushort) 65535);
            // limit so that resulting size in bytes can be represented by ulong
            if (numItems > 1 << 56)
                throw new OverflowException("Number of items must be less than 2^56");
            ulong n = numItems;
            ulong numNodes = n;
            do {
                n = (n + nodeSizeMin - 1) / nodeSizeMin;
                numNodes += n;
            } while (n != 1);
            return numNodes * NODE_ITEM_LEN;
        }

        static IList<(ulong Start, ulong End)> GenerateLevelBounds(ulong numItems, ushort nodeSize) {
            if (nodeSize < 2)
                throw new Exception("Node size must be at least 2");
            if (numItems == 0)
                throw new Exception("Number of items must be greater than 0");

            // number of nodes per level in bottom-up order
            ulong n = numItems;
            ulong numNodes = n;
            List<ulong> levelNumNodes = new List<ulong>() { n };
            do {
                n = (n + nodeSize - 1) / nodeSize;
                numNodes += n;
                levelNumNodes.Add(n);
            } while (n != 1);

            // bounds per level in reversed storage order (top-down)
            List<ulong> levelOffsets = new List<ulong>();
            n = numNodes;
            foreach (ulong size in levelNumNodes) {
                levelOffsets.Add(n - size);
                n -= size;
            };
            List<(ulong Start, ulong End)> levelBounds = new List<(ulong Start, ulong End)>();
            for (int i = 0; i < levelNumNodes.Count; i++)
                levelBounds.Add((levelOffsets[i], levelOffsets[i] + levelNumNodes[i]));
            return levelBounds;
        }

        internal static List<(long Offset, ulong Index)> StreamSearch(Stream stream, ulong numItems, ushort nodeSize, Envelope rect)
        {
            long treePosition = stream.Position;
            double minX = rect.MinX;
            double minY = rect.MinY;
            double maxX = rect.MaxX;
            double maxY = rect.MaxY;
            IList<(ulong Start, ulong End)> levelBounds = GenerateLevelBounds(numItems, nodeSize);
            ulong leafNodesOffset = levelBounds.First().Start;
            ulong numNodes = levelBounds.First().End;
            Stack<(ulong NodeIndex, int Level)> stack = new Stack<(ulong NodeIndex, int Level)>();
            stack.Push((0UL, levelBounds.Count() - 1));
            using BinaryReader reader = new BinaryReader(stream, Encoding.UTF8, true);
            List<(long Offset, ulong Index)> res = new List<(long Offset, ulong Index)>((int)numItems);
            while (stack.Count != 0)
            {
                (ulong nodeIndex, int level) = stack.Pop();
                bool isLeafNode = nodeIndex >= numNodes - numItems;
                // find the end index of the node
                ulong levelBound = levelBounds[level].End;
                ulong end = Math.Min(nodeIndex + nodeSize, levelBound);
                stream.Seek(treePosition + (long)(nodeIndex * NODE_ITEM_LEN), SeekOrigin.Begin);
                long start = (long)(nodeIndex * NODE_ITEM_LEN);
                // search through child nodes
                for (ulong pos = nodeIndex; pos < end; pos++)
                {
                    stream.Seek(treePosition + start + (long)((pos - nodeIndex) * NODE_ITEM_LEN), SeekOrigin.Begin);
                    if (maxX < reader.ReadDouble()) continue; // maxX < nodeMinX
                    if (maxY < reader.ReadDouble()) continue; // maxY < nodeMinY
                    if (minX > reader.ReadDouble()) continue; // minX > nodeMaxX
                    if (minY > reader.ReadDouble()) continue; // minY > nodeMaxY
                    ulong offset = reader.ReadUInt64();
                    if (isLeafNode)
                        res.Add(((long)offset, pos - leafNodesOffset));
                    else
                        stack.Push((offset, level - 1));
                }
                // order queue to traverse sequential
                //queue.sort((a, b) => b[0] - a[0])
            }
            return res;
        }

        public static IEnumerable<(ulong Offset, ulong Index)> StreamSearch(ulong numItems, ushort nodeSize, Envelope rect, ReadNode readNode)
        {
            double minX = rect.MinX;
            double minY = rect.MinY;
            double maxX = rect.MaxX;
            double maxY = rect.MaxY;
            IList<(ulong Start, ulong End)> levelBounds = GenerateLevelBounds(numItems, nodeSize);
            ulong leafNodesOffset = levelBounds.First().Start;
            ulong numNodes = levelBounds.First().End;
            Stack<(ulong NodeIndex, int Level)> stack = new Stack<(ulong NodeIndex, int Level)>();
            stack.Push((0UL, levelBounds.Count() - 1));
            while (stack.Count != 0)
            {
                (ulong nodeIndex, int level) = stack.Pop();
                bool isLeafNode = nodeIndex >= numNodes - numItems;
                // find the end index of the node
                ulong levelBound = levelBounds[level].End;
                ulong end = Math.Min(nodeIndex + nodeSize, levelBound);
                ulong length = end - nodeIndex;
                Stream stream = readNode(nodeIndex * NODE_ITEM_LEN, length * NODE_ITEM_LEN);
                long start = stream.Position;
                using BinaryReader reader = new BinaryReader(stream, Encoding.UTF8, true);
                // search through child nodes
                for (ulong pos = nodeIndex; pos < end; pos++)
                {
                    stream.Seek(start + (long)((pos - nodeIndex) * NODE_ITEM_LEN), SeekOrigin.Begin);
                    if (maxX < reader.ReadDouble()) continue; // maxX < nodeMinX
                    if (maxY < reader.ReadDouble()) continue; // maxY < nodeMinY
                    if (minX > reader.ReadDouble()) continue; // minX > nodeMaxX
                    if (minY > reader.ReadDouble()) continue; // minY > nodeMaxY
                    ulong offset = reader.ReadUInt64();
                    if (isLeafNode)
                        yield return (offset, pos - leafNodesOffset);
                    else
                        stack.Push((offset, level - 1));
                }
                // order queue to traverse sequential
                //queue.sort((a, b) => b[0] - a[0])
            }
        }
    }
}
