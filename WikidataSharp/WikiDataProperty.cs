using JetBrains.Annotations;

namespace WikidataSharp;

[PublicAPI]
public enum WikiDataProperty : long
{
    InstanceOf = 31, // https://www.wikidata.org/wiki/Property:P31
    DissolvedAbolishedOrDemolishedDate = 576, // https://www.wikidata.org/wiki/Property:P576
    OfficialName = 1448, // https://www.wikidata.org/wiki/Property:P1448
    Name = 2561, // https://www.wikidata.org/wiki/Property:P2561
    LocatedInAdministrativeTerritorialEntity = 131 // https://www.wikidata.org/wiki/Property:P131
}