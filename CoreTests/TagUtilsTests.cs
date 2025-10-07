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

    [Test]
    public void TestValuesMatch_OrderInsensitiveWithSemicolons()
    {
        // Arrange
        string v1 = "zebra;dots";
        string v2 = "dots;zebra";

        // Act
        bool matches = TagUtils.ValuesMatch(v1, v2);

        // Assert
        Assert.That(matches, Is.True);
    }

    [Test]
    public void TestValuesMatch_RepeatsIgnored()
    {
        // Arrange
        string v1 = "a;a;b";
        string v2 = "b;a";

        // Act
        bool matches = TagUtils.ValuesMatch(v1, v2);

        // Assert
        Assert.That(matches, Is.True);
    }

    [Test]
    public void TestValuesMatch_CaseSensitive()
    {
        // Arrange
        string v1 = "A;b";
        string v2 = "a;b";

        // Act
        bool matches = TagUtils.ValuesMatch(v1, v2);

        // Assert
        Assert.That(matches, Is.False);
    }

    [Test]
    public void TestValuesMatch_DifferentTokens_False()
    {
        // Arrange
        string v1 = "a;b";
        string v2 = "a;c";

        // Act
        bool matches = TagUtils.ValuesMatch(v1, v2);

        // Assert
        Assert.That(matches, Is.False);
    }

    [Test]
    public void TestValuesMatch_OnlyOneHasSemicolons_False()
    {
        // Arrange
        string v1 = "a;b";
        string v2 = "a b";

        // Act
        bool matches = TagUtils.ValuesMatch(v1, v2);

        // Assert
        Assert.That(matches, Is.False);
    }

    [Test]
    public void TestValuesMatch_EmptyAndWhitespaceTokensIgnored()
    {
        // Arrange
        string v1 = "a; ;b";
        string v2 = "a;b";

        // Act
        bool matches = TagUtils.ValuesMatch(v1, v2);

        // Assert
        Assert.That(matches, Is.True);
    }

    [Test]
    public void TestValuesMatchOrderSensitive_ExactWhitespaceDifferences_IgnoredAroundTokens()
    {
        // Arrange
        string v1 = "a; b";
        string v2 = "a;b";

        // Act
        bool matches = TagUtils.ValuesMatchOrderSensitive(v1, v2);

        // Assert
        Assert.That(matches, Is.True);
    }

    [Test]
    public void TestValuesMatchOrderSensitive_OrderMatters()
    {
        // Arrange
        string v1 = "a;b";
        string v2 = "b;a";

        // Act
        bool matches = TagUtils.ValuesMatchOrderSensitive(v1, v2);

        // Assert
        Assert.That(matches, Is.False);
    }

    [Test]
    public void TestValuesMatchOrderSensitive_RepeatsPreserved()
    {
        // Arrange
        string v1 = "a;a;b";
        string v2 = "a;b";

        // Act
        bool matches = TagUtils.ValuesMatchOrderSensitive(v1, v2);

        // Assert
        Assert.That(matches, Is.False);
    }

    [Test]
    public void TestValuesMatchOrderSensitive_CaseSensitive()
    {
        // Arrange
        string v1 = "A;b";
        string v2 = "a;b";

        // Act
        bool matches = TagUtils.ValuesMatchOrderSensitive(v1, v2);

        // Assert
        Assert.That(matches, Is.False);
    }

    [Test]
    public void TestValuesMatchOrderSensitive_EmptyToken_PreservedAndEqual()
    {
        // Arrange
        string v1 = "hi;;bye";
        string v2 = "hi; ;bye";

        // Act
        bool matches = TagUtils.ValuesMatchOrderSensitive(v1, v2);

        // Assert
        Assert.That(matches, Is.True);
    }

    [Test]
    public void TestValuesMatchOrderSensitive_EmptyToken_MismatchWhenMissing()
    {
        // Arrange
        string v1 = "hi;;bye";
        string v2 = "hi;bye";

        // Act
        bool matches = TagUtils.ValuesMatchOrderSensitive(v1, v2);

        // Assert
        Assert.That(matches, Is.False);
    }
}
