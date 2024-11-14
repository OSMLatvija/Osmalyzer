using NUnit.Framework;
using NUnit.Framework.Legacy;
using Osmalyzer;

namespace OsmalyzerTests;

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

        ClassicAssert.IsTrue(doesMatch);
    }    
    
    [TestCase("Rīgas gatve 37", "Rīgas gatve", "15")]
    [TestCase("Rīgas iela 15", "Rīgas gatve", "15")]
    public void TestDoesntMatch(string fullAddress, string tagStreet, string tagHouseNumber)
    {
        bool doesMatch = FuzzyAddressMatcher.Matches(tagStreet, tagHouseNumber, fullAddress);

        ClassicAssert.IsFalse(doesMatch);
    }
}