namespace Osmalyzer
{
    public class DrinkingWater
    {
        public string Name { get; }
        
        public InstallationType Type { get; }
        
        public OsmCoord Coord { get; }
        

        public DrinkingWater(string name, InstallationType type, OsmCoord coord)
        {
            Name = name;
            Type = type;
            Coord = coord;
        }


        public enum InstallationType
        {
            Static,
            Mobile
        }
    }
}