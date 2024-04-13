using System.Threading.Tasks;
using FlatGeobuf.NTS;
using NetTopologySuite.Features;
using NetTopologySuite.IO;

namespace FlatGeobuf
{
    public static class GeoJsonConversions
    {
        public static byte[] Serialize(string geojson)
        {
            GeoJsonReader reader = new GeoJsonReader();
            FeatureCollection fc = reader.Read<FeatureCollection>(geojson);
            byte[] bytes = FeatureCollectionConversions.Serialize(fc, GeometryType.Unknown);
            return bytes;
        }

        public static async Task<byte[]> SerializeAsync(string geojson)
        {
            GeoJsonReader reader = new GeoJsonReader();
            FeatureCollection fc = reader.Read<FeatureCollection>(geojson);
            byte[] bytes = await FeatureCollectionConversions.SerializeAsync(fc, GeometryType.Unknown);
            return bytes;
        }

        public static string Deserialize(byte[] bytes)
        {
            FeatureCollection fc = FeatureCollectionConversions.Deserialize(bytes);
            GeoJsonWriter writer = new GeoJsonWriter();
            string geojson = writer.Write(fc);
            return geojson;
        }
    }
}
