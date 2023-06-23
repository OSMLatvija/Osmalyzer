namespace Osmalyzer
{
    public class IsRelation : OsmFilter
    {
        internal override bool Matches(OsmElement element)
        {
            return element is OsmRelation;
        }
    }
}