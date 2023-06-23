namespace Osmalyzer
{
    public class IsNode : OsmFilter
    {
        internal override bool Matches(OsmElement element)
        {
            return element is OsmNode;
        }
    }
}