namespace Osmalyzer
{
    public abstract class OsmFilter
    {
        public abstract bool ForNodesOnly { get; }
        public abstract bool ForWaysOnly { get; }
        public abstract bool ForRelationsOnly { get; }


        internal abstract bool Matches(OsmElement element);
    }
}