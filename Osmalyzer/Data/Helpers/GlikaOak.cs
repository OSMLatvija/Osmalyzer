namespace Osmalyzer
{
    public class GlikaOak : ICorrelatorItem
    {
        public OsmCoord Coord { get; }

        public string Name { get; }

        public string? Description { get; }

        public string StartDate { get; }

        public int? Id { get; set; }


        public GlikaOak(OsmCoord coord, string name, string? description, string startDate)
        {
            Coord = coord;
            Name = name;
            Description = description;
            StartDate = startDate;
        }

        
        public string ReportString()
        {
            return
                "Glika oak " +
                "`" + Name + "` " +
                (Id != null ? "#" + Id + " " : "") +
                "(" + StartDate + ")";
        }
    }
}