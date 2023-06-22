namespace Osmalyzer
{
    public class InsidePolygon : OsmFilter
    {
        private readonly OsmPolygon _polygon;


        public InsidePolygon(string polyFileName)
        {
            _polygon = new OsmPolygon(polyFileName);
        }


        internal override bool Matches(OsmElement element)
        {
            return _polygon.ContainsElement(element);
        }
    }
}