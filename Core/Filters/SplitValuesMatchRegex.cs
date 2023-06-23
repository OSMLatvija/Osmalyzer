using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Osmalyzer
{
    public class SplitValuesMatchRegex : OsmFilter
    {
        private readonly string _tag;
        private readonly string _pattern;


        public SplitValuesMatchRegex(string tag, string pattern)
        {
            _tag = tag;
            _pattern = pattern;
        }


        internal override bool Matches(OsmElement element)
        {
            if (element.RawElement.Tags == null)
                return false;

            string rawValue = element.RawElement.Tags.GetValue(_tag);

            List<string> splitValues = TagUtils.SplitValue(rawValue);

            if (splitValues.Count == 0)
                return false;

            foreach (string splitValue in splitValues)
                if (!Regex.IsMatch(splitValue, _pattern))
                    return false;

            return true;
        }
    }
}