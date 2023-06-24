namespace Osmalyzer
{
    public class HasValue : OsmFilter
    {
        public override bool ForNodesOnly => false;
        public override bool ForWaysOnly => false;
        public override bool ForRelationsOnly => false;


        private readonly string _tag;
        private readonly string _value;


        public HasValue(string tag, string value)
        {
            _tag = tag;
            _value = value;
        }


        internal override bool Matches(OsmElement element)
        {
            return
                element.RawElement.Tags != null &&
                element.RawElement.Tags.Contains(_tag, _value);
        }
    }
}