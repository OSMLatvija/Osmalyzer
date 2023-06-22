namespace Osmalyzer
{
    public class HasTag : OsmFilter
    {
        private readonly string _tag;


        public HasTag(string tag)
        {
            _tag = tag;
        }


        internal override bool Matches(OsmElement element)
        {
            return
                element.RawElement.Tags != null &&
                element.RawElement.Tags.ContainsKey(_tag);
        }
    }
}