using JetBrains.Annotations;

namespace WikidataSharp;

[PublicAPI]
public enum WikiDataProperty : long
{
    InstanceOf = 31, // P31
    OfficialName = 1448, // P1448
    Name = 2561, // P2561
    LocatedInAdministrativeTerritorialEntity = 131 // P131
}