using NUnit.Framework;

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
}