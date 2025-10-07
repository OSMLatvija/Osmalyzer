namespace Osmalyzer.Tests;

[TestFixture]
public class FuzzyAddressParserTests
{
    [Test]
    public void TestNull_ExpectException()
    {
        Assert.Throws<ArgumentNullException>(() => FuzzyAddressParser.TryParseAddress(null!));
    }

    [TestCase("")]
    [TestCase(" ")]
    [TestCase("  ")]
    [TestCase("\t")]
    [TestCase(",")]
    [TestCase(",,")]
    [TestCase(" ,")]
    [TestCase(", ")]
    [TestCase("  ,  ")]
    public void TestDegenerate_ExpectNull(string value)
    {
        FuzzyAddress? result = FuzzyAddressParser.TryParseAddress(value);
        Assert.That(result, Is.Null);
    }
    
    [TestCase("Krānu iela 35", "Krānu iela", "35")]
    [TestCase("Īsā iela 1", "Īsā iela", "1")]
    [TestCase("Kr. Krāniņa iela 135", "Kr. Krāniņa iela", "135")]
    public void TestStreetNameAndNumber(string value, string expectedStreet, string expectedNumber)
    {
        // Act
        
        FuzzyAddress? result = FuzzyAddressParser.TryParseAddress(value);

        // Assert
        
        Assert.That(result, Is.Not.Null);
        List<FuzzyAddressPart> parts = result!.Parts;
        Assert.That(parts, Has.Count.EqualTo(1), ResultPrintout(parts));
        Assert.That(parts, Has.All.InstanceOf<FuzzyAddressStreetNameAndNumberPart>(), ResultPrintout(parts));
        
        FuzzyAddressStreetNameAndNumberPart streetPart = (FuzzyAddressStreetNameAndNumberPart)parts[0];
        Assert.That(streetPart.Index, Is.EqualTo(0));
        Assert.That(streetPart.StreetValue, Is.EqualTo(expectedStreet));
        Assert.That(streetPart.NumberValue, Is.EqualTo(expectedNumber));
        Assert.That(streetPart.Confidence, Is.EqualTo(FuzzyConfidence.High));
    }
    
    [TestCase("Krānu iela 35 / Gailīšu aleja 24", "Krānu iela", "35", "Gailīšu aleja", "24")]
    public void TestTwoAddressesInStreetLine(string value, string expectedStreet1, string expectedNumber1, string expectedStreet2, string expectedNumber2)
    {
        // Act
        
        FuzzyAddress? result = FuzzyAddressParser.TryParseAddress(value);

        // Assert
        
        Assert.That(result, Is.Not.Null);
        List<FuzzyAddressPart> parts = result!.Parts;
        Assert.That(parts, Has.Count.EqualTo(2), ResultPrintout(parts));
        Assert.That(parts, Has.Exactly(2).InstanceOf<FuzzyAddressStreetNameAndNumberPart>(), ResultPrintout(parts));
        
        FuzzyAddressStreetNameAndNumberPart streetPart1 = (FuzzyAddressStreetNameAndNumberPart)parts[0];
        FuzzyAddressStreetNameAndNumberPart streetPart2 = (FuzzyAddressStreetNameAndNumberPart)parts[1];
        
        if (streetPart1.StreetValue != expectedStreet1)
            (streetPart1, streetPart2) = (streetPart2, streetPart1);
        
        Assert.That(streetPart1.Index, Is.EqualTo(0));
        Assert.That(streetPart1.StreetValue, Is.EqualTo(expectedStreet1));
        Assert.That(streetPart1.NumberValue, Is.EqualTo(expectedNumber1));
        Assert.That(streetPart1.Confidence, Is.EqualTo(FuzzyConfidence.High));
        
        Assert.That(streetPart2.Index, Is.EqualTo(0));
        Assert.That(streetPart2.StreetValue, Is.EqualTo(expectedStreet2));
        Assert.That(streetPart2.NumberValue, Is.EqualTo(expectedNumber2));
        Assert.That(streetPart2.Confidence, Is.EqualTo(FuzzyConfidence.High));
    }
    
    [TestCase("\"Krāniņi\"", "Krāniņi", FuzzyConfidence.High)]
    [TestCase("Krāniņi", "Krāniņi", FuzzyConfidence.Low)]
    [TestCase("Krāniņi 18", "Krāniņi 18", FuzzyConfidence.Low)]
    public void TestHouseName(string value, string expectedCleaned, FuzzyConfidence confidence)
    {
        // Act
        
        FuzzyAddress? result = FuzzyAddressParser.TryParseAddress(value);

        // Assert
        
        Assert.That(result, Is.Not.Null);
        List<FuzzyAddressPart> parts = result!.Parts;
        Assert.That(parts, Has.Count.EqualTo(1), ResultPrintout(parts));
        Assert.That(parts, Has.All.InstanceOf<FuzzyAddressHouseNamePart>(), ResultPrintout(parts));
        
        FuzzyAddressHouseNamePart housePart = (FuzzyAddressHouseNamePart)parts[0];
        Assert.That(housePart.Index, Is.EqualTo(0));
        Assert.That(housePart.Value, Is.EqualTo(expectedCleaned));
        Assert.That(housePart.Confidence, Is.EqualTo(confidence));
    }
    
    [TestCase("\"\"")]
    [TestCase("\" \"")]
    [TestCase("\"A\"")]
    [TestCase("\"12345\"")]
    public void TestBadHouseName(string value)
    {
        // Act
        
        FuzzyAddress? result = FuzzyAddressParser.TryParseAddress(value);

        // Assert
        
        Assert.That(result, Is.Null);
    }
    
    [TestCase("Rīga")] // galvaspilsēta
    [TestCase("Jelgava")] // valstspilsēta
    [TestCase("Ludza")] // pilsēta
    [TestCase("Inčukalns")] // ciems
    public void TestKnownCityName(string value)
    {
        // Act
        
        FuzzyAddress? result = FuzzyAddressParser.TryParseAddress(value);

        // Assert
        
        Assert.That(result, Is.Not.Null);
        List<FuzzyAddressPart> parts = result!.Parts;
        Assert.That(parts, Has.Count.EqualTo(1), ResultPrintout(parts));
        Assert.That(parts, Has.All.InstanceOf<FuzzyAddressCityPart>(), ResultPrintout(parts));
        
        FuzzyAddressCityPart streetPart = (FuzzyAddressCityPart)parts[0];
        Assert.That(streetPart.Index, Is.EqualTo(0));
        Assert.That(streetPart.Value, Is.EqualTo(value));
        Assert.That(streetPart.Confidence, Is.EqualTo(FuzzyConfidence.High));
    }
    
    [TestCase("LV-1234", "LV-1234", FuzzyConfidence.High)]
    [TestCase("LV 1234", "LV-1234", FuzzyConfidence.High)]
    [TestCase("LV1234", "LV-1234", FuzzyConfidence.High)]
    [TestCase("1234", "LV-1234", FuzzyConfidence.Low)]
    public void TestPostcode(string value, string expectedPostcode, FuzzyConfidence confidence)
    {
        // Act
        
        FuzzyAddress? result = FuzzyAddressParser.TryParseAddress(value);

        // Assert
        
        Assert.That(result, Is.Not.Null);
        List<FuzzyAddressPart> parts = result!.Parts;
        Assert.That(parts, Has.Count.EqualTo(1), ResultPrintout(parts));
        Assert.That(parts, Has.All.InstanceOf<FuzzyAddressPostcodePart>(), ResultPrintout(parts));
        
        FuzzyAddressPostcodePart postcodePart = (FuzzyAddressPostcodePart)parts[0];
        Assert.That(postcodePart.Index, Is.EqualTo(0));
        Assert.That(postcodePart.Value, Is.EqualTo(expectedPostcode));
        Assert.That(postcodePart.Confidence, Is.EqualTo(confidence));
    }
    
    [TestCase("Krānu iela 35, Krāniņmuiža", "Krānu iela", "35", 0, "Krāniņmuiža", 1)]
    [TestCase("Krāniņmuiža, Krānu iela 35", "Krānu iela", "35", 1, "Krāniņmuiža", 0)]
    public void TestStreetNameNumberAndCity(string value, string expectedStreet, string expectedNumber, int expectedStreetAndNumberIndex, string expectedCity, int expectedCityIndex)
    {
        // Act
        
        FuzzyAddress? result = FuzzyAddressParser.TryParseAddress(value);

        // Assert
        
        Assert.That(result, Is.Not.Null);
        List<FuzzyAddressPart> parts = result!.Parts;
        Assert.That(parts, Has.Count.EqualTo(2), ResultPrintout(parts));
        Assert.That(parts, Has.Exactly(1).InstanceOf<FuzzyAddressStreetNameAndNumberPart>(), ResultPrintout(parts));
        Assert.That(parts, Has.Exactly(1).InstanceOf<FuzzyAddressCityPart>(), ResultPrintout(parts));
        
        FuzzyAddressStreetNameAndNumberPart streetPart = parts.OfType<FuzzyAddressStreetNameAndNumberPart>().First();
        Assert.That(streetPart.Index, Is.EqualTo(expectedStreetAndNumberIndex));
        Assert.That(streetPart.StreetValue, Is.EqualTo(expectedStreet));
        Assert.That(streetPart.NumberValue, Is.EqualTo(expectedNumber));
        
        FuzzyAddressCityPart cityPart = parts.OfType<FuzzyAddressCityPart>().First();
        Assert.That(cityPart.Index, Is.EqualTo(expectedCityIndex));
        Assert.That(cityPart.Value, Is.EqualTo(expectedCity));
    }
    
    [TestCase("Krānu iela 35", "35")]
    [TestCase("Krānu iela 35A", "35A")]
    [TestCase("Krānu iela 35K", "35K")]
    [TestCase("Krānu iela 35 A", "35A")]
    [TestCase("Krānu iela 35a", "35A")]
    [TestCase("Krānu iela 35k", "35K")]
    [TestCase("Krānu iela 35 a", "35A")]
    [TestCase("Krānu iela 35 k-24", "35 k-24")]
    [TestCase("Krānu iela 35 k24", "35 k-24")]
    [TestCase("Krānu iela 35A k-24", "35A k-24")]
    [TestCase("Krānu iela 35K k-24", "35K k-24")]
    [TestCase("Krānu iela 35k k-24", "35K k-24")]
    [TestCase("Krānu iela 35 A k-24", "35A k-24")]
    [TestCase("Krānu iela 3/5", "3/5")]
    public void TestStreetNumberGetsRecognizedAndSanitized(string value, string expectedNumber)
    {
        // Act
        
        FuzzyAddress? result = FuzzyAddressParser.TryParseAddress(value);

        // Assert
        
        Assert.That(result, Is.Not.Null);
        List<FuzzyAddressPart> parts = result!.Parts;
        Assert.That(parts, Has.Count.EqualTo(1), ResultPrintout(parts));
        Assert.That(parts, Has.All.InstanceOf<FuzzyAddressStreetNameAndNumberPart>(), ResultPrintout(parts));
        
        FuzzyAddressStreetNameAndNumberPart streetPart = (FuzzyAddressStreetNameAndNumberPart)parts[0];
        Assert.That(streetPart.NumberValue, Is.EqualTo(expectedNumber));
        Assert.That(streetPart.Confidence, Is.EqualTo(FuzzyConfidence.High));
    }
    
    [TestCase("Krānu ielā 35", "Krānu iela")]
    public void TestStreetNameGetsSanitized(string value, string expectedStreet)
    {
        // Act
        
        FuzzyAddress? result = FuzzyAddressParser.TryParseAddress(value);

        // Assert
        
        Assert.That(result, Is.Not.Null);
        List<FuzzyAddressPart> parts = result!.Parts;
        Assert.That(parts, Has.Count.EqualTo(1), ResultPrintout(parts));
        Assert.That(parts, Has.All.InstanceOf<FuzzyAddressStreetNameAndNumberPart>(), ResultPrintout(parts));
        
        FuzzyAddressStreetNameAndNumberPart streetPart = (FuzzyAddressStreetNameAndNumberPart)parts[0];
        Assert.That(streetPart.StreetValue, Is.EqualTo(expectedStreet));
        Assert.That(streetPart.Confidence, Is.EqualTo(FuzzyConfidence.High));
    }
    
    [TestCase("Krānu 35")]
    public void TestAmbiguousStreetNameHasFallback(string value)
    {
        // Act
        
        FuzzyAddress? result = FuzzyAddressParser.TryParseAddress(value);

        // Assert
        
        Assert.That(result, Is.Not.Null);
        List<FuzzyAddressPart> parts = result!.Parts;
        Assert.That(parts, Has.Count.EqualTo(1), ResultPrintout(parts));
        Assert.That(parts, Has.All.InstanceOf<FuzzyAddressHouseNamePart>(), ResultPrintout(parts));
        
        FuzzyAddressHouseNamePart housePart = (FuzzyAddressHouseNamePart)parts[0];
        Assert.That(housePart.Value, Is.EqualTo("Krānu 35"));
        Assert.That(housePart.Confidence, Is.EqualTo(FuzzyConfidence.Low));
        Assert.That(housePart.Fallbacks, Is.Not.Null);
        Assert.That(housePart.Fallbacks, Has.Count.EqualTo(1));
        Assert.That(housePart.Fallbacks![0], Is.InstanceOf<FuzzyAddressStreetNameAndNumberPart>());
        FuzzyAddressStreetNameAndNumberPart fallbackPart = (FuzzyAddressStreetNameAndNumberPart)housePart.Fallbacks![0];
        Assert.That(fallbackPart.StreetValue, Is.EqualTo("Krānu iela"));
        Assert.That(fallbackPart.NumberValue, Is.EqualTo("35"));
        Assert.That(fallbackPart.Confidence, Is.EqualTo(FuzzyConfidence.Low));
    }

    [TestCase("Limbažu novads", "Limbažu novads", FuzzyConfidence.High)] // real
    [TestCase("Ornitoloģijas novads", "Ornitoloģijas novads", FuzzyConfidence.Low)]
    [TestCase("Ornitoloģijas nov.", "Ornitoloģijas novads", FuzzyConfidence.Low)]
    public void TestMunicipality(string value, string expected, FuzzyConfidence confidence)
    {
        // Arrange
        
        // Act
        FuzzyAddress? result = FuzzyAddressParser.TryParseAddress(value);
        
        // Assert
        Assert.That(result, Is.Not.Null);
        List<FuzzyAddressPart> parts = result!.Parts;
        Assert.That(parts, Has.Count.EqualTo(1), ResultPrintout(parts));
        Assert.That(parts, Has.All.InstanceOf<FuzzyAddressMunicipalityPart>(), ResultPrintout(parts));
        
        FuzzyAddressMunicipalityPart part = (FuzzyAddressMunicipalityPart)parts[0];
        Assert.That(part.Index, Is.EqualTo(0));
        Assert.That(part.Value, Is.EqualTo(expected));
        Assert.That(part.Confidence, Is.EqualTo(confidence));
    }

    [TestCase("Brenguļu pagasts", "Brenguļu pagasts", FuzzyConfidence.High)] // real
    [TestCase("Vistiņu pagasts", "Vistiņu pagasts", FuzzyConfidence.Low)]
    [TestCase("Vistiņu pag.", "Vistiņu pagasts", FuzzyConfidence.Low)]
    public void TestParish(string value, string expected, FuzzyConfidence confidence)
    {
        // Arrange
        
        // Act
        FuzzyAddress? result = FuzzyAddressParser.TryParseAddress(value);
        
        // Assert
        Assert.That(result, Is.Not.Null);
        List<FuzzyAddressPart> parts = result!.Parts;
        Assert.That(parts, Has.Count.EqualTo(1), ResultPrintout(parts));
        Assert.That(parts, Has.All.InstanceOf<FuzzyAddressParishPart>(), ResultPrintout(parts));
        
        FuzzyAddressParishPart part = (FuzzyAddressParishPart)parts[0];
        Assert.That(part.Index, Is.EqualTo(0));
        Assert.That(part.Value, Is.EqualTo(expected));
        Assert.That(part.Confidence, Is.EqualTo(confidence));
    }

    [TestCase("pagasts")]
    [TestCase("pag.")]
    [TestCase("B pagasts")]
    [TestCase("12345 pagasts")]
    [TestCase("Nepagasts")]
    [TestCase("novads")]
    [TestCase("nov.")]
    [TestCase("B novads")]
    [TestCase("12345 novads")]
    [TestCase("Nenovads")]
    public void TestBadParishOrMunicipality(string value)
    {
        // Act
        
        FuzzyAddress? result = FuzzyAddressParser.TryParseAddress(value);

        // Assert
        
        Assert.That(result, Is.Null);
    }

    [Test]
    public void TestFullAddressExample1()
    {
        // Arrange
        
        const string address = "Krānu iela 35, Krāniņmuiža, Vistiņu pagasts, Ornitoloģijas novads, LV-1234";

        // Act
        
        FuzzyAddress? result = FuzzyAddressParser.TryParseAddress(address);
        
        // Assert
        
        Assert.That(result, Is.Not.Null);
        List<FuzzyAddressPart> parts = result!.Parts;
        Assert.That(parts, Has.Count.EqualTo(5), ResultPrintout(parts));
        Assert.That(parts, Has.Exactly(1).InstanceOf<FuzzyAddressStreetNameAndNumberPart>(), ResultPrintout(parts));
        Assert.That(parts, Has.Exactly(1).InstanceOf<FuzzyAddressCityPart>(), ResultPrintout(parts));
        Assert.That(parts, Has.Exactly(1).InstanceOf<FuzzyAddressParishPart>(), ResultPrintout(parts));
        Assert.That(parts, Has.Exactly(1).InstanceOf<FuzzyAddressMunicipalityPart>(), ResultPrintout(parts));
        Assert.That(parts, Has.Exactly(1).InstanceOf<FuzzyAddressPostcodePart>(), ResultPrintout(parts));
        Assert.That(parts.OfType<FuzzyAddressStreetNameAndNumberPart>().First().StreetValue, Is.EqualTo("Krānu iela"));
        Assert.That(parts.OfType<FuzzyAddressStreetNameAndNumberPart>().First().NumberValue, Is.EqualTo("35"));
        Assert.That(parts.OfType<FuzzyAddressPostcodePart>().First().Value, Is.EqualTo("LV-1234"));
        Assert.That(parts.OfType<FuzzyAddressCityPart>().First().Value, Is.EqualTo("Krāniņmuiža"));
        Assert.That(parts.OfType<FuzzyAddressParishPart>().First().Value, Is.EqualTo("Vistiņu pagasts"));
        Assert.That(parts.OfType<FuzzyAddressMunicipalityPart>().First().Value, Is.EqualTo("Ornitoloģijas novads"));
    }

    [TestCase("\"Krāniņi\", Krāniņmuiža, Vistiņu pagasts, Ornitoloģijas novads, LV-1234")]
    [TestCase("Krāniņi, Krāniņmuiža, Vistiņu pagasts, Ornitoloģijas novads, LV-1234")]
    public void TestFullAddressExample2(string value)
    {
        // Act
        
        FuzzyAddress? result = FuzzyAddressParser.TryParseAddress(value);
        
        // Assert
        
        Assert.That(result, Is.Not.Null);
        List<FuzzyAddressPart> parts = result!.Parts;
        Assert.That(parts, Has.Count.EqualTo(5), ResultPrintout(parts));
        Assert.That(parts, Has.Exactly(1).InstanceOf<FuzzyAddressHouseNamePart>(), ResultPrintout(parts));
        Assert.That(parts, Has.Exactly(1).InstanceOf<FuzzyAddressCityPart>(), ResultPrintout(parts));
        Assert.That(parts, Has.Exactly(1).InstanceOf<FuzzyAddressParishPart>(), ResultPrintout(parts));
        Assert.That(parts, Has.Exactly(1).InstanceOf<FuzzyAddressMunicipalityPart>(), ResultPrintout(parts));
        Assert.That(parts, Has.Exactly(1).InstanceOf<FuzzyAddressPostcodePart>(), ResultPrintout(parts));
        Assert.That(parts.OfType<FuzzyAddressHouseNamePart>().First().Value, Is.EqualTo("Krāniņi"));
        Assert.That(parts.OfType<FuzzyAddressPostcodePart>().First().Value, Is.EqualTo("LV-1234"));
        Assert.That(parts.OfType<FuzzyAddressCityPart>().First().Value, Is.EqualTo("Krāniņmuiža"));
        Assert.That(parts.OfType<FuzzyAddressParishPart>().First().Value, Is.EqualTo("Vistiņu pagasts"));
        Assert.That(parts.OfType<FuzzyAddressMunicipalityPart>().First().Value, Is.EqualTo("Ornitoloģijas novads"));
    }

    [TestCase("Krāns 18, Krāniņmuiža, Vistiņu pagasts, Ornitoloģijas novads, LV-1234")]
    public void TestFullAddressExample3(string value)
    {
        // Act
        
        FuzzyAddress? result = FuzzyAddressParser.TryParseAddress(value);
        
        // Assert
        
        Assert.That(result, Is.Not.Null);
        List<FuzzyAddressPart> parts = result!.Parts;
        Assert.That(parts, Has.Count.EqualTo(5), ResultPrintout(parts));
        Assert.That(parts, Has.Exactly(1).InstanceOf<FuzzyAddressHouseNamePart>(), ResultPrintout(parts));
        Assert.That(parts, Has.Exactly(1).InstanceOf<FuzzyAddressCityPart>(), ResultPrintout(parts));
        Assert.That(parts, Has.Exactly(1).InstanceOf<FuzzyAddressParishPart>(), ResultPrintout(parts));
        Assert.That(parts, Has.Exactly(1).InstanceOf<FuzzyAddressMunicipalityPart>(), ResultPrintout(parts));
        Assert.That(parts, Has.Exactly(1).InstanceOf<FuzzyAddressPostcodePart>(), ResultPrintout(parts));
        Assert.That(parts.OfType<FuzzyAddressHouseNamePart>().First().Value, Is.EqualTo("Krāns 18"));
        Assert.That(parts.OfType<FuzzyAddressPostcodePart>().First().Value, Is.EqualTo("LV-1234"));
        Assert.That(parts.OfType<FuzzyAddressCityPart>().First().Value, Is.EqualTo("Krāniņmuiža"));
        Assert.That(parts.OfType<FuzzyAddressParishPart>().First().Value, Is.EqualTo("Vistiņu pagasts"));
        Assert.That(parts.OfType<FuzzyAddressMunicipalityPart>().First().Value, Is.EqualTo("Ornitoloģijas novads"));
    }

    [TestCase("Krānu iela 35-3", "Krānu iela", "35", "3")]
    [TestCase("Krānu iela 35A-3", "Krānu iela", "35A", "3")]
    [TestCase("Krānu iela 35A-3 k-24", "Krānu iela", "35A k-24", "3")]
    public void TestStreetNameNumberAndUnit(string value, string expectedStreet, string expectedNumber, string expectedUnit)
    {
        // Act
        
        FuzzyAddress? result = FuzzyAddressParser.TryParseAddress(value);

        // Assert
        
        Assert.That(result, Is.Not.Null);
        List<FuzzyAddressPart> parts = result!.Parts;
        Assert.That(parts, Has.Count.EqualTo(1), ResultPrintout(parts));
        Assert.That(parts, Has.All.InstanceOf<FuzzyAddressStreetNameAndNumberPart>(), ResultPrintout(parts));
        
        FuzzyAddressStreetNameAndNumberPart streetPart = (FuzzyAddressStreetNameAndNumberPart)parts[0];
        Assert.That(streetPart.Index, Is.EqualTo(0));
        Assert.That(streetPart.StreetValue, Is.EqualTo(expectedStreet));
        Assert.That(streetPart.NumberValue, Is.EqualTo(expectedNumber));
        Assert.That(streetPart.UnitValue, Is.EqualTo(expectedUnit));
        Assert.That(streetPart.Confidence, Is.EqualTo(FuzzyConfidence.High));
    }
    

    private static string ResultPrintout(List<FuzzyAddressPart> parts)
    {
        return "Results were: " + string.Join("\n", parts.Select(p => p.GetQuickString()));
    }
}