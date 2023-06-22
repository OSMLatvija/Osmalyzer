namespace Osmalyzer
{
    public class InsidePolygon : OsmFilter
    {
        private readonly OsmPolygon _polygon;


        public InsidePolygon(OsmPolygon polygon)
        {
            _polygon = polygon;
        }


        internal override bool Matches(OsmElement element)
        {
            return _polygon.ContainsElement(element);
        }
    }
}