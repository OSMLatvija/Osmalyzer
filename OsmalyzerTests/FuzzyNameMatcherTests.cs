using NUnit.Framework;

namespace Osmalyzer.Tests;

public class FuzzyNameMatcherTests
{
    [TestCase("A", "A")]
    [TestCase("A", "a")]
    [TestCase("a", "a")]
    [TestCase("a", "A")]
    public void TestMatches(string name1, string name2)
    {
        bool doesMatch = FuzzyNameMatcher.Matches(name1, name2);

        Assert.That(doesMatch, Is.True);
    }    
    
    [TestCase("A", "B")]
    public void TestDoesntMatch(string name1, string name2)
    {
        bool doesMatch = FuzzyNameMatcher.Matches(name1, name2);

        Assert.That(doesMatch, Is.False);
    }
}