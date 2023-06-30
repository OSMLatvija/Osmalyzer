namespace Osmalyzer
{
    public struct OsmCoord
    {
        public readonly double lat;
        
        public readonly double lon;


        public string OsmUrl => @"https://www.openstreetmap.org/#map=19/" + lat.ToString("F5") + @"/" + lon.ToString("F5");

        
        public OsmCoord(double lat, double lon)
        {
            this.lat = lat;
            this.lon = lon;
        }


        public override string ToString()
        {
            return lat.ToString("F5") + @"/" + lon.ToString("F5");
        }
    }
}