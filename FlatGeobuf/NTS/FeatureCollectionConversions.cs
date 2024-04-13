using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;

using Google.FlatBuffers;
using NetTopologySuite.Features;
using FlatGeobuf.Index;
using NetTopologySuite.Geometries;
using System.Threading.Tasks;
using Nito.AsyncEx;
using System.Text;

namespace FlatGeobuf.NTS
{
    public class ColumnMeta
    {
        public string Name { get; set; }
        public ColumnType Type { get; set; }
    }

    public class LayerMeta
    {
        public string Name { get; set; }
        public GeometryType GeometryType { get; set; }
        public byte Dimensions { get; set; }
        public IList<ColumnMeta> Columns { get; set; }
    }

    public static class FeatureCollectionConversions {
        public static async Task<byte[]> SerializeAsync(FeatureCollection fc, GeometryType geometryType, byte dimensions = 2, IList<ColumnMeta> columns = null)
        {
            IFeature featureFirst = fc.First();
            if (columns == null && featureFirst.Attributes != null)
                    columns = featureFirst.Attributes.GetNames()
                        .Select(n => new ColumnMeta() { Name = n, Type = ToColumnType(featureFirst.Attributes.GetType(n)) })
                        .ToList();
            using MemoryStream memoryStream = new MemoryStream();
            await SerializeAsync(memoryStream, fc, geometryType, dimensions, columns);
            return memoryStream.ToArray();
        }

        public static byte[] Serialize(FeatureCollection fc, GeometryType geometryType, byte dimensions = 2, IList<ColumnMeta> columns = null)
        {
            IFeature featureFirst = fc.First();
            if (columns == null && featureFirst.Attributes != null)
                    columns = featureFirst.Attributes.GetNames()
                        .Select(n => new ColumnMeta() { Name = n, Type = ToColumnType(featureFirst.Attributes.GetType(n)) })
                        .ToList();
            using MemoryStream memoryStream = new MemoryStream();
            Serialize(memoryStream, fc, geometryType, dimensions, columns);
            return memoryStream.ToArray();
        }

        public static void Serialize(Stream output, IEnumerable<IFeature> features, GeometryType geometryType, byte dimensions = 2, IList<ColumnMeta> columns = null)
        {
            AsyncContext.Run(async () => await SerializeAsync(output, features, geometryType, dimensions, columns));
        }

#if NETSTANDARD2_0
        public static async Task SerializeAsync(Stream output, IEnumerable<IFeature> features, GeometryType geometryType, byte dimensions = 2, IList<ColumnMeta> columns = null)
        {
            await output.WriteAsync(Constants.MagicBytes, 0, Constants.MagicBytes.Length);
            ByteBuffer headerBuffer = BuildHeader(0, geometryType, dimensions, columns, null);
            byte[] bytes = headerBuffer.ToSizedArray();
            await output.WriteAsync(bytes, 0, bytes.Length);
            headerBuffer.Position += 4;
            HeaderT header = Header.GetRootAsHeader(headerBuffer).UnPack();
            foreach (IFeature feature in features)
            {
                ByteBuffer buffer = FeatureConversions.ToByteBuffer(feature, header);
                bytes = buffer.ToSizedArray();
                await output.WriteAsync(bytes, 0, bytes.Length);
            }
        }
#else
        public static async Task SerializeAsync(Stream output, IEnumerable<IFeature> features, GeometryType geometryType, byte dimensions = 2, IList<ColumnMeta> columns = null)
        {
            await output.WriteAsync(Constants.MagicBytes);
            var headerBuffer = BuildHeader(0, geometryType, dimensions, columns, null);
            await output.WriteAsync(headerBuffer.ToReadOnlyMemory(headerBuffer.Position, headerBuffer.Length - headerBuffer.Position));
            headerBuffer.Position += 4;
            var header = Header.GetRootAsHeader(headerBuffer).UnPack();
            foreach (var feature in features)
            {
                var buffer = FeatureConversions.ToByteBuffer(feature, header);
                await output.WriteAsync(buffer.ToReadOnlyMemory(buffer.Position, buffer.Length - buffer.Position));
            }
        }
#endif

        private static ColumnType ToColumnType(Type type)
        {
            return (Type.GetTypeCode(type)) switch
            {
                TypeCode.Byte => ColumnType.UByte,
                TypeCode.SByte => ColumnType.Byte,
                TypeCode.Boolean => ColumnType.Bool,
                TypeCode.Int32 => ColumnType.Int,
                TypeCode.Int64 => ColumnType.Long,
                TypeCode.Double => ColumnType.Double,
                TypeCode.String => ColumnType.String,
                _ => throw new ApplicationException("Unknown type"),
            };
        }

        public static FeatureCollection Deserialize(byte[] bytes)
        {
            FeatureCollection fc = new FeatureCollection();

            foreach (IFeature feature in Deserialize(new MemoryStream(bytes)))
                fc.Add(feature);

            return fc;
        }

        public static IEnumerable<IFeature> Deserialize(Stream stream, Envelope rect = null)
        {
            using BinaryReader reader = new BinaryReader(stream, Encoding.UTF8, true);
            HeaderT header = Helpers.ReadHeader(stream, out int headerSize).UnPack();

            ulong count = header.FeaturesCount;
            ushort nodeSize = header.IndexNodeSize;
            GeometryType geometryType = header.GeometryType;

            FlatGeobufCoordinateSequenceFactory seqFactory = new FlatGeobufCoordinateSequenceFactory();
            GeometryFactory factory = new GeometryFactory(seqFactory);

            if (nodeSize > 0)
            {
                long offset = 8 + 4 + headerSize;
                ulong size = PackedRTree.CalcSize(count, nodeSize);
                if (rect != null) {
                    List<(ulong Offset, ulong Index)> result = PackedRTree.StreamSearch(count, nodeSize, rect, (ulong treeOffset, ulong size) => {
                        stream.Seek(offset + (long) treeOffset, SeekOrigin.Begin);
                        return stream;
                    }).ToList();
                    foreach ((ulong Offset, ulong Index) item in result) {
                        stream.Seek(offset + (long) size + (long) item.Offset, SeekOrigin.Begin);
                        int featureLength = reader.ReadInt32();
                        ByteBuffer byteBuffer = new ByteBuffer(reader.ReadBytes(featureLength));
                        IFeature feature = FeatureConversions.FromByteBuffer(factory, seqFactory, byteBuffer, header);
                        yield return feature;
                    }
                    yield break;
                }
                stream.Seek(8 + 4 + headerSize + (long) size, SeekOrigin.Begin);
            }

            while (stream.Position < stream.Length)
            {
                int featureLength = reader.ReadInt32();
                ByteBuffer byteBuffer = new ByteBuffer(reader.ReadBytes(featureLength));
                IFeature feature = FeatureConversions.FromByteBuffer(factory, seqFactory, byteBuffer, header);
                yield return feature;
            }
        }

        public static ByteBuffer BuildHeader(ulong count, GeometryType geometryType, byte dimensions, IList<ColumnMeta> columns, PackedRTree index)
        {
            FlatBufferBuilder builder = new FlatBufferBuilder(1024);
            VectorOffset? columnsOffset = null;
            if (columns != null)
            {
                Offset<Column>[] columnsArray = columns
                                                .Select(c => Column.CreateColumn(builder, builder.CreateString(c.Name), c.Type))
                                                .ToArray();
                columnsOffset = Header.CreateColumnsVector(builder, columnsArray);
            }

            Header.StartHeader(builder);
            Header.AddGeometryType(builder, geometryType);
            if (dimensions == 3)
                Header.AddHasZ(builder, true);
            if (dimensions == 4)
                Header.AddHasM(builder, true);
            if (columnsOffset.HasValue)
                Header.AddColumns(builder, columnsOffset.Value);
            if (index != null)
                Header.AddIndexNodeSize(builder, 16);
            else
                Header.AddIndexNodeSize(builder, 0);
            Header.AddFeaturesCount(builder, count);
            Offset<Header> offset = Header.EndHeader(builder);

            builder.FinishSizePrefixed(offset.Value);

            return builder.DataBuffer;
        }
    }
}