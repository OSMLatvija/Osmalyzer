using WikidataSharp;

namespace Osmalyzer;

/// <summary>
/// Represents a data item that can be associated with a Wikidata item
/// </summary>
public interface IHasWikidataItem
{
    WikidataItem? WikidataItem { get; set; }
}

