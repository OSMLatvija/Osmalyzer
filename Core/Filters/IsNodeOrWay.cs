namespace Osmalyzer
{
    public class IsNodeOrWay : OsmFilter
    {
        internal override bool Matches(OsmElement element)
        {
            return element is OsmNode or OsmWay;
        }
    }
}