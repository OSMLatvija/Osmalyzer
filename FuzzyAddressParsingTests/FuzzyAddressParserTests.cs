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
        List<FuzzyAddressPart>? result = FuzzyAddressParser.TryParseAddress(value);
        Assert.That(result, Is.Null);
    }
    
    [TestCase("Krānu iela 35", "Krānu iela", "35")]
    public void TestStreetNameAndNumber(string value, string expectedStreet, string expectedNumber)
    {
        // Act
        
        List<FuzzyAddressPart>? result = FuzzyAddressParser.TryParseAddress(value);

        // Assert
        
        Assert.That(result, Is.Not.Null);
        Assert.That(result, Has.Count.EqualTo(1));
        Assert.That(result, Has.All.InstanceOf<FuzzyAddressStreetNameAndNumberPart>());
        
        FuzzyAddressStreetNameAndNumberPart streetPart = (FuzzyAddressStreetNameAndNumberPart)result![0];
        Assert.That(streetPart.Index, Is.EqualTo(0));
        Assert.That(streetPart.StreetValue, Is.EqualTo(expectedStreet));
        Assert.That(streetPart.NumberValue, Is.EqualTo(expectedNumber));
        Assert.That(streetPart.Confidence, Is.EqualTo(FuzzyConfidence.High));
    }
    
    [TestCase("\"Krāniņi\"")]
    public void TestHouseName(string value)
    {
        // Act
        
        List<FuzzyAddressPart>? result = FuzzyAddressParser.TryParseAddress(value);

        // Assert
        
        Assert.That(result, Is.Not.Null);
        Assert.That(result, Has.Count.EqualTo(1));
        Assert.That(result, Has.All.InstanceOf<FuzzyAddressHouseNamePart>());
        
        FuzzyAddressHouseNamePart housePart = (FuzzyAddressHouseNamePart)result![0];
        Assert.That(housePart.Index, Is.EqualTo(0));
        Assert.That(housePart.Value, Is.EqualTo("Krāniņi"));
        Assert.That(housePart.Confidence, Is.EqualTo(FuzzyConfidence.High));
    }
    
    [TestCase("\"\"")]
    [TestCase("\" \"")]
    [TestCase("\"A\"")]
    [TestCase("\"12345\"")]
    public void TestBadHouseName(string value)
    {
        // Act
        
        List<FuzzyAddressPart>? result = FuzzyAddressParser.TryParseAddress(value);

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
        
        List<FuzzyAddressPart>? result = FuzzyAddressParser.TryParseAddress(value);

        // Assert
        
        Assert.That(result, Is.Not.Null);
        Assert.That(result, Has.Count.EqualTo(1));
        Assert.That(result, Has.All.InstanceOf<FuzzyAddressCityPart>());
        
        FuzzyAddressCityPart streetPart = (FuzzyAddressCityPart)result![0];
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
        
        List<FuzzyAddressPart>? result = FuzzyAddressParser.TryParseAddress(value);

        // Assert
        
        Assert.That(result, Is.Not.Null);
        Assert.That(result, Has.Count.EqualTo(1));
        Assert.That(result, Has.All.InstanceOf<FuzzyAddressPostcodePart>());
        
        FuzzyAddressPostcodePart postcodePart = (FuzzyAddressPostcodePart)result![0];
        Assert.That(postcodePart.Index, Is.EqualTo(0));
        Assert.That(postcodePart.Value, Is.EqualTo(expectedPostcode));
        Assert.That(postcodePart.Confidence, Is.EqualTo(confidence));
    }
    
    [TestCase("Krānu iela 35, Krāniņmuiža", "Krānu iela", "35", 0, "Krāniņmuiža", 1)]
    [TestCase("Krāniņmuiža, Krānu iela 35", "Krānu iela", "35", 1, "Krāniņmuiža", 0)]
    public void TestStreetNameNumberAndCity(string value, string expectedStreet, string expectedNumber, int expectedStreetAndNumberIndex, string expectedCity, int expectedCityIndex)
    {
        // Act
        
        List<FuzzyAddressPart>? result = FuzzyAddressParser.TryParseAddress(value);

        // Assert
        
        Assert.That(result, Is.Not.Null);
        Assert.That(result, Has.Count.EqualTo(2));
        Assert.That(result, Has.Exactly(1).InstanceOf<FuzzyAddressStreetNameAndNumberPart>());
        Assert.That(result, Has.Exactly(1).InstanceOf<FuzzyAddressCityPart>());
        
        FuzzyAddressStreetNameAndNumberPart streetPart = result.OfType<FuzzyAddressStreetNameAndNumberPart>().First();
        Assert.That(streetPart.Index, Is.EqualTo(expectedStreetAndNumberIndex));
        Assert.That(streetPart.StreetValue, Is.EqualTo(expectedStreet));
        Assert.That(streetPart.NumberValue, Is.EqualTo(expectedNumber));
        
        FuzzyAddressCityPart cityPart = result.OfType<FuzzyAddressCityPart>().First();
        Assert.That(cityPart.Index, Is.EqualTo(expectedCityIndex));
        Assert.That(cityPart.Value, Is.EqualTo(expectedCity));
    }
    
    [TestCase("Krānu iela 35", "35")]
    [TestCase("Krānu 35", "35")]
    [TestCase("Krānu iela 35A", "35A")]
    [TestCase("Krānu iela 35a", "35A")]
    [TestCase("Krānu iela 35 k-24", "35 k-24")]
    [TestCase("Krānu iela 35 k24", "35 k-24")]
    [TestCase("Krānu iela 35A k-24", "35A k-24")]
    [TestCase("Krānu iela 3/5", "3/5")]
    public void TestStreetNumberGetsRecognizedAndSanitized(string value, string expectedNumber)
    {
        // Act
        
        List<FuzzyAddressPart>? result = FuzzyAddressParser.TryParseAddress(value);

        // Assert
        
        Assert.That(result, Is.Not.Null);
        Assert.That(result, Has.Count.EqualTo(1));
        Assert.That(result, Has.All.InstanceOf<FuzzyAddressStreetNameAndNumberPart>());
        
        FuzzyAddressStreetNameAndNumberPart streetPart = (FuzzyAddressStreetNameAndNumberPart)result![0];
        Assert.That(streetPart.NumberValue, Is.EqualTo(expectedNumber));
        Assert.That(streetPart.Confidence, Is.EqualTo(FuzzyConfidence.High));
    }
    
    [TestCase("Krānu ielā 35", "Krānu iela")]
    [TestCase("Krānu 35", "Krānu iela")]
    public void TestStreetNameGetsSanitized(string value, string expectedStreet)
    {
        // Act
        
        List<FuzzyAddressPart>? result = FuzzyAddressParser.TryParseAddress(value);

        // Assert
        
        Assume.That(result, Is.Not.Null);
        Assume.That(result, Has.Count.EqualTo(1));
        Assume.That(result, Has.All.InstanceOf<FuzzyAddressStreetNameAndNumberPart>());
        
        FuzzyAddressStreetNameAndNumberPart streetPart = (FuzzyAddressStreetNameAndNumberPart)result![0];
        Assert.That(streetPart.StreetValue, Is.EqualTo(expectedStreet));
        Assert.That(streetPart.Confidence, Is.EqualTo(FuzzyConfidence.High));
    }
}