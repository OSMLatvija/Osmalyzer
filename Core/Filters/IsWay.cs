namespace Osmalyzer
{
    public class IsWay : OsmFilter
    {
        internal override bool Matches(OsmElement element)
        {
            return element is OsmWay;
        }
    }
}