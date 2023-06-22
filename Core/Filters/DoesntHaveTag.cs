﻿namespace Osmalyzer
{
    public class DoesntHaveTag : OsmFilter
    {
        private readonly string _tag;


        public DoesntHaveTag(string tag)
        {
            _tag = tag;
        }


        internal override bool Matches(OsmElement element)
        {
            return
                element.RawElement.Tags == null ||
                !element.RawElement.Tags.ContainsKey(_tag);
        }
    }
}