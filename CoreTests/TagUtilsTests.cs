using NUnit.Framework;
using Osmalyzer;

namespace CoreTests;

public class TagUtilsTests
{
    [Test]
    public void TestSplitValue_SingleToken()
    {
        // Arrange
        string input = "abc";

        // Act
        List<string> result = TagUtils.SplitValue(input);

        // Assert
        Assert.That(result, Is.EqualTo(new List<string> { "abc" }));
    }

    [Test]
    public void TestSplitValue_MultipleTokens_Trimmed_NoEmpties()
    {
        // Arrange
        string input = " a ; b ;  c  ";

        // Act
        List<string> result = TagUtils.SplitValue(input);

        // Assert
        Assert.That(result, Is.EqualTo(new List<string> { "a", "b", "c" }));
    }

    [Test]
    public void TestSplitValue_DuplicatesArePreserved()
    {
        // Arrange
        string input = "a;b;a";

        // Act
        List<string> result = TagUtils.SplitValue(input);

        // Assert
        Assert.That(result, Is.EqualTo(new List<string> { "a", "b", "a" }));
    }

    [Test]
    public void TestSplitValue_TrailingSemicolon_Ignored()
    {
        // Arrange
        string input = "a;b;";

        // Act
        List<string> result = TagUtils.SplitValue(input);

        // Assert
        Assert.That(result, Is.EqualTo(new List<string> { "a", "b" }));
    }

    [Test]
    public void TestValuesMatch_ExactEquality()
    {
        // Arrange
        string v1 = "abc";
        string v2 = "abc";

        // Act
        bool matches = TagUtils.ValuesMatch(v1, v2);

        // Assert
        Assert.That(matches, Is.True);
    }

    [Test]
    public void TestValuesMatch_SimpleInequality_NoSemicolons()
    {
        // Arrange
        string v1 = "abc";
        string v2 = "abd";

        // Act
        bool matches = TagUtils.ValuesMatch(v1, v2);

        // Assert
        Assert.That(matches, Is.False);
    }

    [TestCase("zebra;dots", "dots;zebra")]
    [TestCase(" a ; b ", "b; a")]
    [TestCase("a;b;c", "c;b;a")]
    public void TestValuesMatch_OrderInsensitiveWithSemicolons(string v1, string v2)
    {
        // Arrange

        // Act
        bool matches = TagUtils.ValuesMatch(v1, v2);

        // Assert
        Assert.That(matches, Is.True);
    }

    [TestCase("a;a;b", "b;a")]
    [TestCase("a;a", "a; a")]
    [TestCase("x;x;y;y", "y;x")]
    [TestCase("a;a;b", "a;b")]
    public void TestValuesMatch_RepeatsIgnored(string v1, string v2)
    {
        // Arrange

        // Act
        bool matches = TagUtils.ValuesMatch(v1, v2);

        // Assert
        Assert.That(matches, Is.True);
    }

    [TestCase("A;b", "a;b")]
    [TestCase("a;B", "a;b")]
    [TestCase("Ab", "ab")]
    public void TestValuesMatch_CaseSensitive(string v1, string v2)
    {
        // Arrange

        // Act
        bool matches = TagUtils.ValuesMatch(v1, v2);

        // Assert
        Assert.That(matches, Is.False);
    }

    [TestCase("a;b", "a;c")]
    [TestCase("a", "b")]
    [TestCase("a;b", "a;b;c")]
    [TestCase("x;y", "x;z")]
    public void TestValuesMatch_DifferentTokens_False(string v1, string v2)
    {
        // Arrange

        // Act
        bool matches = TagUtils.ValuesMatch(v1, v2);

        // Assert
        Assert.That(matches, Is.False);
    }

    [TestCase("ab", "a;b")]
    [TestCase("a;b", "ab")]
    [TestCase("a;b", "a,b")]
    public void TestValuesMatch_OnlyOneHasSemicolons_False(string v1, string v2)
    {
        // Arrange

        // Act
        bool matches = TagUtils.ValuesMatch(v1, v2);

        // Assert
        Assert.That(matches, Is.False);
    }

    [TestCase("a; ;b", "a;b")]
    [TestCase(" ; a ; b ", "a;b")]
    [TestCase("a; ; ;b", "a;b")]
    public void TestValuesMatch_EmptyAndWhitespaceTokensIgnored(string v1, string v2)
    {
        // Arrange

        // Act
        bool matches = TagUtils.ValuesMatch(v1, v2);

        // Assert
        Assert.That(matches, Is.True);
    }

    [TestCase("a; b", "a;b")]
    [TestCase(" a ; b ", "a ;b")]
    [TestCase("x; y;z", "x;y; z")]
    public void TestValuesMatchOrderSensitive_ExactWhitespaceDifferences_IgnoredAroundTokens(string v1, string v2)
    {
        // Arrange

        // Act
        bool matches = TagUtils.ValuesMatchOrderSensitive(v1, v2);

        // Assert
        Assert.That(matches, Is.True);
    }

    [TestCase("a;b", "b;a")]
    [TestCase("x;y;z", "z;y;x")]
    [TestCase("1;2;3", "1;3;2")]
    public void TestValuesMatchOrderSensitive_OrderMatters(string v1, string v2)
    {
        // Arrange

        // Act
        bool matches = TagUtils.ValuesMatchOrderSensitive(v1, v2);

        // Assert
        Assert.That(matches, Is.False);
    }

    [TestCase("a;a", "a", false)]
    [TestCase("a;a", "a;a", true)]
    [TestCase("a;a;b", "a;b;a", false)]
    [TestCase("a;a;b", "a;a;b", true)]
    public void TestValuesMatchOrderSensitive_RepeatsPreserved(string v1, string v2, bool expected)
    {
        // Arrange

        // Act
        bool matches = TagUtils.ValuesMatchOrderSensitive(v1, v2);

        // Assert
        Assert.That(matches, Is.EqualTo(expected));
    }

    [TestCase("A;b", "a;b")]
    [TestCase("a;B", "a;b")]
    public void TestValuesMatchOrderSensitive_CaseSensitive(string v1, string v2)
    {
        // Arrange

        // Act
        bool matches = TagUtils.ValuesMatchOrderSensitive(v1, v2);

        // Assert
        Assert.That(matches, Is.False);
    }

    [TestCase("hi;;bye", "hi; ;bye")]
    [TestCase(";a", " ;a")]
    [TestCase("a;;", "a; ;")]
    public void TestValuesMatchOrderSensitive_EmptyToken_PreservedAndEqual(string v1, string v2)
    {
        // Arrange

        // Act
        bool matches = TagUtils.ValuesMatchOrderSensitive(v1, v2);

        // Assert
        Assert.That(matches, Is.True);
    }

    [TestCase("hi;;bye", "hi;bye")]
    [TestCase(";;a;b", ";a;b")]
    [TestCase("a; ;b", "a;b")]
    [TestCase("a;;b", "a;b;")]
    [TestCase("a;;b", "a;;b;;")]
    [TestCase(";a;b", "a;b")]
    public void TestValuesMatchOrderSensitive_EmptyToken_MismatchWhenMissing(string v1, string v2)
    {
        // Arrange

        // Act
        bool matches = TagUtils.ValuesMatchOrderSensitive(v1, v2);

        // Assert
        Assert.That(matches, Is.False);
    }
}
