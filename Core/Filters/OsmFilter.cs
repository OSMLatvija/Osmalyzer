namespace Osmalyzer
{
    public abstract class OsmFilter
    {
        internal abstract bool Matches(OsmElement element);
    }
}