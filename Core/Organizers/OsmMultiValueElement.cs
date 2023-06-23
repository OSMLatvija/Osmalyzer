using JetBrains.Annotations;

namespace Osmalyzer
{
    [PublicAPI]
    public class OsmMultiValueElement
    {
        /// <summary> The group's value that had this element </summary>
        public string Value { get; }

        public OsmElement Element { get; }


        public OsmMultiValueElement(string value, OsmElement element)
        {
            Value = value;
            Element = element;
        }
    }
}