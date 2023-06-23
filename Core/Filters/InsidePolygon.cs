namespace Osmalyzer
{
    public class InsidePolygon : OsmFilter
    {
        private readonly OsmPolygon _polygon;
        
        private readonly OsmPolygon.RelationInclusionCheck _relationInclusionCheck;


        public InsidePolygon(OsmPolygon polygon, OsmPolygon.RelationInclusionCheck relationInclusionCheck)
        {
            _polygon = polygon;
            _relationInclusionCheck = relationInclusionCheck;
        }


        internal override bool Matches(OsmElement element)
        {
            return _polygon.ContainsElement(element, _relationInclusionCheck);
        }
    }
}