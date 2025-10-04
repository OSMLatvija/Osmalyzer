using NUnit.Framework;

namespace Osmalyzer.Tests;

[TestFixture]
public class FuzzyAddressParserTests
{
    [Test]
    public void TestNull_ExpectException()
    {
        Assert.Throws<ArgumentNullException>(() => FuzzyAddressParser.TryParseAddress(null!, out _, out _, out _));
    }
}