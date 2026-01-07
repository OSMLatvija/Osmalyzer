using NUnit.Framework;

namespace Osmalyzer.Tests;

public class FuzzyAddressMatcherTests
{
    [TestCase("Rīgas gatve 15", "Rīgas gatve", "15")]
    [TestCase("Rīgas Gatve 15", "Rīgas gatve", "15")]
    [TestCase("15, Rīgas gatve", "Rīgas gatve", "15")]
    [TestCase("TC Buyall, Rīgas gatve 15, Rīga", "Rīgas gatve", "15")]
    [TestCase("Rīgas gatve 15D", "Rīgas gatve", "15D")]
    [TestCase("Rīgas gatve 15d", "Rīgas gatve", "15D")]
    [TestCase("Brīvības 15", "Brīvības iela", "15")]
    public void TestMatches(string fullAddress, string tagStreet, string tagHouseNumber)
    {
        bool doesMatch = FuzzyAddressMatcher.Matches(tagStreet, tagHouseNumber, fullAddress);

        Assert.That(doesMatch, Is.True);
    }    
    
    [TestCase("Rīgas gatve 37", "Rīgas gatve", "15")]
    [TestCase("Rīgas iela 15", "Rīgas gatve", "15")]
    public void TestDoesntMatch(string fullAddress, string tagStreet, string tagHouseNumber)
    {
        bool doesMatch = FuzzyAddressMatcher.Matches(tagStreet, tagHouseNumber, fullAddress);

        Assert.That(doesMatch, Is.False);
    }

    [TestCase("Something iela 5-3", "Something iela", "5", "3")]
    [TestCase("Something iela 5A-3", "Something iela", "5A", "3")]
    public void TestMatchesWithUnit(string fullAddress, string tagStreet, string tagHouseNumber, string tagUnit)
    {
        bool doesMatch = FuzzyAddressMatcher.Matches(tagStreet, tagHouseNumber, tagUnit, fullAddress);

        Assert.That(doesMatch, Is.True);
    }

    [Test]
    public void TestDoesntMatchWithDifferentUnit()
    {
        string fullAddress = "Something iela 5-4";
        string tagStreet = "Something iela";
        string tagHouseNumber = "5";
        string tagUnit = "3";

        bool doesMatch = FuzzyAddressMatcher.Matches(tagStreet, tagHouseNumber, tagUnit, fullAddress);

        Assert.That(doesMatch, Is.False);
    }

    [Test]
    public void TestMatchesWhenNoTagUnit()
    {
        string fullAddress = "Something iela 5-3";
        string tagStreet = "Something iela";
        string tagHouseNumber = "5";

        bool doesMatch = FuzzyAddressMatcher.Matches(tagStreet, tagHouseNumber, fullAddress);

        Assert.That(doesMatch, Is.True);
    }
}