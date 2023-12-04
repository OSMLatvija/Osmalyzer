using NUnit.Framework;
using Osmalyzer;

namespace OsmalyzerTests;

public class FuzzyNameMatcherTests
{
    [TestCase("A", "A")]
    [TestCase("A", "a")]
    [TestCase("a", "a")]
    [TestCase("a", "A")]
    public void TestMatches(string name1, string name2)
    {
        bool doesMatch = FuzzyNameMatcher.Matches(name1, name2);

        Assert.IsTrue(doesMatch);
    }    
    
    [TestCase("A", "B")]
    public void TestDoesntMatch(string name1, string name2)
    {
        bool doesMatch = FuzzyNameMatcher.Matches(name1, name2);

        Assert.IsFalse(doesMatch);
    }
}