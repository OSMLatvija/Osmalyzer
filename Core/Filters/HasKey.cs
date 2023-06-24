namespace Osmalyzer
{
    public class HasKey : OsmFilter
    {
        public override bool ForNodesOnly => false;
        public override bool ForWaysOnly => false;
        public override bool ForRelationsOnly => false;
        public override bool TaggedOnly => true;


        private readonly string _key;


        public HasKey(string key)
        {
            _key = key;
        }


        internal override bool Matches(OsmElement element)
        {
            return element.HasKey(_key);
        }
    }
}