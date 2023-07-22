namespace Osmalyzer
{
    public class GlikaOak
    {
        public OsmCoord Coord { get; }
        
        public string Name { get; }
        
        public string? Description { get; }
        
        public string StartDate { get; }


        public GlikaOak(OsmCoord coord, string name, string? description, string startDate)
        {
            Coord = coord;
            Name = name;
            Description = description;
            StartDate = startDate;
        }
    }
}