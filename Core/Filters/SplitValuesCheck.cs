using System;
using System.Collections.Generic;

namespace Osmalyzer
{
    public class SplitValuesCheck : OsmFilter
    {
        public override bool ForNodesOnly => false;
        public override bool ForWaysOnly => false;
        public override bool ForRelationsOnly => false;


        private readonly string _tag;
        private readonly Func<string, bool> _check;


        public SplitValuesCheck(string tag, Func<string, bool> check)
        {
            _tag = tag;
            _check = check;
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
                if (!_check(splitValue))
                    return false;

            return true;
        }
    }
}