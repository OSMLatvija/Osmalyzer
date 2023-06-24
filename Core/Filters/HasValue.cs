namespace Osmalyzer
{
    public class HasValue : OsmFilter
    {
        public override bool ForNodesOnly => false;
        public override bool ForWaysOnly => false;
        public override bool ForRelationsOnly => false;
        public override bool TaggedOnly => true;


        private readonly string _key;
        private readonly string _value;


        public HasValue(string key, string value)
        {
            _key = key;
            _value = value;
        }


        internal override bool Matches(OsmElement element)
        {
            return element.HasValue(_key, _value);
        }
    }
}