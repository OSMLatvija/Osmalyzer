using NUnit.Framework;

namespace Osmalyzer.Tests;

[TestFixture]
public class VdbAltNamesParserTests
{
    [Test]
    public void TestParseSingleNameWithSingleSquareBracketQualifier()
    {
        // Arrange
        string input = "Name1 [q1]";
        
        // Act
        List<VdbAltName> result = VdbAnalysisData.ParseAltNamesWithQualifiersPublic(input);
        
        // Assert
        Assert.That(result, Has.Count.EqualTo(1));
        Assert.That(result[0].Name, Is.EqualTo("Name1"));
        Assert.That(result[0].Qualifiers, Has.Count.EqualTo(1));
        Assert.That(result[0].Qualifiers[0].Type, Is.EqualTo(VdbAltNameQualifierType.Pronunciation));
        Assert.That(result[0].Qualifiers[0].Content, Is.EqualTo("q1"));
    }

    [Test]
    public void TestParseSingleNameWithSingleRoundBracketQualifier()
    {
        // Arrange
        string input = "Name1 (q1)";
        
        // Act
        List<VdbAltName> result = VdbAnalysisData.ParseAltNamesWithQualifiersPublic(input);
        
        // Assert
        Assert.That(result, Has.Count.EqualTo(1));
        Assert.That(result[0].Name, Is.EqualTo("Name1"));
        Assert.That(result[0].Qualifiers, Has.Count.EqualTo(1));
        Assert.That(result[0].Qualifiers[0].Type, Is.EqualTo(VdbAltNameQualifierType.Comment));
        Assert.That(result[0].Qualifiers[0].Content, Is.EqualTo("q1"));
    }

    [Test]
    public void TestParseSingleNameWithCommaInSquareBracket()
    {
        // Arrange
        string input = "Name1 [q1, q2]";
        
        // Act
        List<VdbAltName> result = VdbAnalysisData.ParseAltNamesWithQualifiersPublic(input);
        
        // Assert
        Assert.That(result, Has.Count.EqualTo(1));
        Assert.That(result[0].Name, Is.EqualTo("Name1"));
        Assert.That(result[0].Qualifiers, Has.Count.EqualTo(1));
        Assert.That(result[0].Qualifiers[0].Type, Is.EqualTo(VdbAltNameQualifierType.Pronunciation));
        Assert.That(result[0].Qualifiers[0].Content, Is.EqualTo("q1, q2"));
    }

    [Test]
    public void TestParseSingleNameWithCommaInRoundBracket()
    {
        // Arrange
        string input = "Name1 (q1, q2, q3)";
        
        // Act
        List<VdbAltName> result = VdbAnalysisData.ParseAltNamesWithQualifiersPublic(input);
        
        // Assert
        Assert.That(result, Has.Count.EqualTo(1));
        Assert.That(result[0].Name, Is.EqualTo("Name1"));
        Assert.That(result[0].Qualifiers, Has.Count.EqualTo(1));
        Assert.That(result[0].Qualifiers[0].Type, Is.EqualTo(VdbAltNameQualifierType.Comment));
        Assert.That(result[0].Qualifiers[0].Content, Is.EqualTo("q1, q2, q3"));
    }

    [Test]
    public void TestParseSingleNameWithBothBracketTypes()
    {
        // Arrange
        string input = "Name1 [q1] (q2)";
        
        // Act
        List<VdbAltName> result = VdbAnalysisData.ParseAltNamesWithQualifiersPublic(input);
        
        // Assert
        Assert.That(result, Has.Count.EqualTo(1));
        Assert.That(result[0].Name, Is.EqualTo("Name1"));
        Assert.That(result[0].Qualifiers, Has.Count.EqualTo(2));
        Assert.That(result[0].Qualifiers[0].Type, Is.EqualTo(VdbAltNameQualifierType.Pronunciation));
        Assert.That(result[0].Qualifiers[0].Content, Is.EqualTo("q1"));
        Assert.That(result[0].Qualifiers[1].Type, Is.EqualTo(VdbAltNameQualifierType.Comment));
        Assert.That(result[0].Qualifiers[1].Content, Is.EqualTo("q2"));
    }

    [Test]
    public void TestParseSingleNameWithBothBracketTypesReversedOrder()
    {
        // Arrange
        string input = "Name1 (q1) [q2]";
        
        // Act
        List<VdbAltName> result = VdbAnalysisData.ParseAltNamesWithQualifiersPublic(input);
        
        // Assert
        Assert.That(result, Has.Count.EqualTo(1));
        Assert.That(result[0].Name, Is.EqualTo("Name1"));
        Assert.That(result[0].Qualifiers, Has.Count.EqualTo(2));
        Assert.That(result[0].Qualifiers[0].Type, Is.EqualTo(VdbAltNameQualifierType.Comment));
        Assert.That(result[0].Qualifiers[0].Content, Is.EqualTo("q1"));
        Assert.That(result[0].Qualifiers[1].Type, Is.EqualTo(VdbAltNameQualifierType.Pronunciation));
        Assert.That(result[0].Qualifiers[1].Content, Is.EqualTo("q2"));
    }

    [Test]
    public void TestParseSingleNameWithMultipleSquareBracketQualifiers()
    {
        // Arrange
        string input = "Name1 [q1] [q2] [q3]";
        
        // Act
        List<VdbAltName> result = VdbAnalysisData.ParseAltNamesWithQualifiersPublic(input);
        
        // Assert
        Assert.That(result, Has.Count.EqualTo(1));
        Assert.That(result[0].Name, Is.EqualTo("Name1"));
        Assert.That(result[0].Qualifiers, Has.Count.EqualTo(3));
        Assert.That(result[0].Qualifiers[0].Type, Is.EqualTo(VdbAltNameQualifierType.Pronunciation));
        Assert.That(result[0].Qualifiers[0].Content, Is.EqualTo("q1"));
        Assert.That(result[0].Qualifiers[1].Type, Is.EqualTo(VdbAltNameQualifierType.Pronunciation));
        Assert.That(result[0].Qualifiers[1].Content, Is.EqualTo("q2"));
        Assert.That(result[0].Qualifiers[2].Type, Is.EqualTo(VdbAltNameQualifierType.Pronunciation));
        Assert.That(result[0].Qualifiers[2].Content, Is.EqualTo("q3"));
    }

    [Test]
    public void TestParseSingleNameWithMultipleRoundBracketQualifiers()
    {
        // Arrange
        string input = "Name1 (q1) (q2) (q3)";
        
        // Act
        List<VdbAltName> result = VdbAnalysisData.ParseAltNamesWithQualifiersPublic(input);
        
        // Assert
        Assert.That(result, Has.Count.EqualTo(1));
        Assert.That(result[0].Name, Is.EqualTo("Name1"));
        Assert.That(result[0].Qualifiers, Has.Count.EqualTo(3));
        Assert.That(result[0].Qualifiers[0].Type, Is.EqualTo(VdbAltNameQualifierType.Comment));
        Assert.That(result[0].Qualifiers[0].Content, Is.EqualTo("q1"));
        Assert.That(result[0].Qualifiers[1].Type, Is.EqualTo(VdbAltNameQualifierType.Comment));
        Assert.That(result[0].Qualifiers[1].Content, Is.EqualTo("q2"));
        Assert.That(result[0].Qualifiers[2].Type, Is.EqualTo(VdbAltNameQualifierType.Comment));
        Assert.That(result[0].Qualifiers[2].Content, Is.EqualTo("q3"));
    }

    [Test]
    public void TestParseSingleNameWithMixedMultipleQualifiers()
    {
        // Arrange
        string input = "Name1 [q1] (q2) [q3] (q4)";
        
        // Act
        List<VdbAltName> result = VdbAnalysisData.ParseAltNamesWithQualifiersPublic(input);
        
        // Assert
        Assert.That(result, Has.Count.EqualTo(1));
        Assert.That(result[0].Name, Is.EqualTo("Name1"));
        Assert.That(result[0].Qualifiers, Has.Count.EqualTo(4));
        Assert.That(result[0].Qualifiers[0].Type, Is.EqualTo(VdbAltNameQualifierType.Pronunciation));
        Assert.That(result[0].Qualifiers[0].Content, Is.EqualTo("q1"));
        Assert.That(result[0].Qualifiers[1].Type, Is.EqualTo(VdbAltNameQualifierType.Comment));
        Assert.That(result[0].Qualifiers[1].Content, Is.EqualTo("q2"));
        Assert.That(result[0].Qualifiers[2].Type, Is.EqualTo(VdbAltNameQualifierType.Pronunciation));
        Assert.That(result[0].Qualifiers[2].Content, Is.EqualTo("q3"));
        Assert.That(result[0].Qualifiers[3].Type, Is.EqualTo(VdbAltNameQualifierType.Comment));
        Assert.That(result[0].Qualifiers[3].Content, Is.EqualTo("q4"));
    }

    [Test]
    public void TestParseSingleNameWithoutQualifiers()
    {
        // Arrange
        string input = "Name1";
        
        // Act
        List<VdbAltName> result = VdbAnalysisData.ParseAltNamesWithQualifiersPublic(input);
        
        // Assert
        Assert.That(result, Has.Count.EqualTo(1));
        Assert.That(result[0].Name, Is.EqualTo("Name1"));
        Assert.That(result[0].Qualifiers, Has.Count.EqualTo(0));
    }

    [Test]
    public void TestParseTwoNamesWithoutQualifiers()
    {
        // Arrange
        string input = "Name1, Name2";
        
        // Act
        List<VdbAltName> result = VdbAnalysisData.ParseAltNamesWithQualifiersPublic(input);
        
        // Assert
        Assert.That(result, Has.Count.EqualTo(2));
        
        Assert.That(result[0].Name, Is.EqualTo("Name1"));
        Assert.That(result[0].Qualifiers, Has.Count.EqualTo(0));
        
        Assert.That(result[1].Name, Is.EqualTo("Name2"));
        Assert.That(result[1].Qualifiers, Has.Count.EqualTo(0));
    }

    [Test]
    public void TestParseTwoNamesWithSquareBracketQualifiers()
    {
        // Arrange
        string input = "Name1 [q1], Name2 [q2]";
        
        // Act
        List<VdbAltName> result = VdbAnalysisData.ParseAltNamesWithQualifiersPublic(input);
        
        // Assert
        Assert.That(result, Has.Count.EqualTo(2));
        
        Assert.That(result[0].Name, Is.EqualTo("Name1"));
        Assert.That(result[0].Qualifiers, Has.Count.EqualTo(1));
        Assert.That(result[0].Qualifiers[0].Type, Is.EqualTo(VdbAltNameQualifierType.Pronunciation));
        Assert.That(result[0].Qualifiers[0].Content, Is.EqualTo("q1"));
        
        Assert.That(result[1].Name, Is.EqualTo("Name2"));
        Assert.That(result[1].Qualifiers, Has.Count.EqualTo(1));
        Assert.That(result[1].Qualifiers[0].Type, Is.EqualTo(VdbAltNameQualifierType.Pronunciation));
        Assert.That(result[1].Qualifiers[0].Content, Is.EqualTo("q2"));
    }

    [Test]
    public void TestParseTwoNamesWithRoundBracketQualifiers()
    {
        // Arrange
        string input = "Name1 (q1), Name2 (q2)";
        
        // Act
        List<VdbAltName> result = VdbAnalysisData.ParseAltNamesWithQualifiersPublic(input);
        
        // Assert
        Assert.That(result, Has.Count.EqualTo(2));
        
        Assert.That(result[0].Name, Is.EqualTo("Name1"));
        Assert.That(result[0].Qualifiers, Has.Count.EqualTo(1));
        Assert.That(result[0].Qualifiers[0].Type, Is.EqualTo(VdbAltNameQualifierType.Comment));
        Assert.That(result[0].Qualifiers[0].Content, Is.EqualTo("q1"));
        
        Assert.That(result[1].Name, Is.EqualTo("Name2"));
        Assert.That(result[1].Qualifiers, Has.Count.EqualTo(1));
        Assert.That(result[1].Qualifiers[0].Type, Is.EqualTo(VdbAltNameQualifierType.Comment));
        Assert.That(result[1].Qualifiers[0].Content, Is.EqualTo("q2"));
    }

    [Test]
    public void TestParseThreeNamesWithMixedQualifiers()
    {
        // Arrange
        string input = "Name1 [q1], Name2 (q2), Name3 [q3] (q4)";
        
        // Act
        List<VdbAltName> result = VdbAnalysisData.ParseAltNamesWithQualifiersPublic(input);
        
        // Assert
        Assert.That(result, Has.Count.EqualTo(3));
        
        Assert.That(result[0].Name, Is.EqualTo("Name1"));
        Assert.That(result[0].Qualifiers, Has.Count.EqualTo(1));
        Assert.That(result[0].Qualifiers[0].Type, Is.EqualTo(VdbAltNameQualifierType.Pronunciation));
        Assert.That(result[0].Qualifiers[0].Content, Is.EqualTo("q1"));
        
        Assert.That(result[1].Name, Is.EqualTo("Name2"));
        Assert.That(result[1].Qualifiers, Has.Count.EqualTo(1));
        Assert.That(result[1].Qualifiers[0].Type, Is.EqualTo(VdbAltNameQualifierType.Comment));
        Assert.That(result[1].Qualifiers[0].Content, Is.EqualTo("q2"));
        
        Assert.That(result[2].Name, Is.EqualTo("Name3"));
        Assert.That(result[2].Qualifiers, Has.Count.EqualTo(2));
        Assert.That(result[2].Qualifiers[0].Type, Is.EqualTo(VdbAltNameQualifierType.Pronunciation));
        Assert.That(result[2].Qualifiers[0].Content, Is.EqualTo("q3"));
        Assert.That(result[2].Qualifiers[1].Type, Is.EqualTo(VdbAltNameQualifierType.Comment));
        Assert.That(result[2].Qualifiers[1].Content, Is.EqualTo("q4"));
    }

    [Test]
    public void TestParseMultipleNamesSomeWithoutQualifiers()
    {
        // Arrange
        string input = "Name1, Name2 (q1), Name3, Name4 [q2]";
        
        // Act
        List<VdbAltName> result = VdbAnalysisData.ParseAltNamesWithQualifiersPublic(input);
        
        // Assert
        Assert.That(result, Has.Count.EqualTo(4));
        
        Assert.That(result[0].Name, Is.EqualTo("Name1"));
        Assert.That(result[0].Qualifiers, Has.Count.EqualTo(0));
        
        Assert.That(result[1].Name, Is.EqualTo("Name2"));
        Assert.That(result[1].Qualifiers, Has.Count.EqualTo(1));
        Assert.That(result[1].Qualifiers[0].Type, Is.EqualTo(VdbAltNameQualifierType.Comment));
        Assert.That(result[1].Qualifiers[0].Content, Is.EqualTo("q1"));
        
        Assert.That(result[2].Name, Is.EqualTo("Name3"));
        Assert.That(result[2].Qualifiers, Has.Count.EqualTo(0));
        
        Assert.That(result[3].Name, Is.EqualTo("Name4"));
        Assert.That(result[3].Qualifiers, Has.Count.EqualTo(1));
        Assert.That(result[3].Qualifiers[0].Type, Is.EqualTo(VdbAltNameQualifierType.Pronunciation));
        Assert.That(result[3].Qualifiers[0].Content, Is.EqualTo("q2"));
    }

    [Test]
    public void TestParseNamesWithExtraWhitespace()
    {
        // Arrange
        string input = "Name1 [q1]  ,  Name2 (q2)  ,  Name3";
        
        // Act
        List<VdbAltName> result = VdbAnalysisData.ParseAltNamesWithQualifiersPublic(input);
        
        // Assert
        Assert.That(result, Has.Count.EqualTo(3));
        
        Assert.That(result[0].Name, Is.EqualTo("Name1"));
        Assert.That(result[0].Qualifiers, Has.Count.EqualTo(1));
        Assert.That(result[0].Qualifiers[0].Type, Is.EqualTo(VdbAltNameQualifierType.Pronunciation));
        Assert.That(result[0].Qualifiers[0].Content, Is.EqualTo("q1"));
        
        Assert.That(result[1].Name, Is.EqualTo("Name2"));
        Assert.That(result[1].Qualifiers, Has.Count.EqualTo(1));
        Assert.That(result[1].Qualifiers[0].Type, Is.EqualTo(VdbAltNameQualifierType.Comment));
        Assert.That(result[1].Qualifiers[0].Content, Is.EqualTo("q2"));
        
        Assert.That(result[2].Name, Is.EqualTo("Name3"));
        Assert.That(result[2].Qualifiers, Has.Count.EqualTo(0));
    }

    [Test]
    public void TestParseNameWithComplexQualifierContent()
    {
        // Arrange
        string input = "Name1 (qualifier with spaces, punctuation! and 123)";
        
        // Act
        List<VdbAltName> result = VdbAnalysisData.ParseAltNamesWithQualifiersPublic(input);
        
        // Assert
        Assert.That(result, Has.Count.EqualTo(1));
        Assert.That(result[0].Name, Is.EqualTo("Name1"));
        Assert.That(result[0].Qualifiers, Has.Count.EqualTo(1));
        Assert.That(result[0].Qualifiers[0].Type, Is.EqualTo(VdbAltNameQualifierType.Comment));
        Assert.That(result[0].Qualifiers[0].Content, Is.EqualTo("qualifier with spaces, punctuation! and 123"));
    }

    [Test]
    public void TestParseFiveNamesWithVariedCombinations()
    {
        // Arrange
        string input = "Name1, Name2 [q1], Name3 (q2), Name4 [q3] (q4), Name5 [q5] [q6] (q7)";
        
        // Act
        List<VdbAltName> result = VdbAnalysisData.ParseAltNamesWithQualifiersPublic(input);
        
        // Assert
        Assert.That(result, Has.Count.EqualTo(5));
        
        Assert.That(result[0].Name, Is.EqualTo("Name1"));
        Assert.That(result[0].Qualifiers, Has.Count.EqualTo(0));
        
        Assert.That(result[1].Name, Is.EqualTo("Name2"));
        Assert.That(result[1].Qualifiers, Has.Count.EqualTo(1));
        Assert.That(result[1].Qualifiers[0].Type, Is.EqualTo(VdbAltNameQualifierType.Pronunciation));
        Assert.That(result[1].Qualifiers[0].Content, Is.EqualTo("q1"));
        
        Assert.That(result[2].Name, Is.EqualTo("Name3"));
        Assert.That(result[2].Qualifiers, Has.Count.EqualTo(1));
        Assert.That(result[2].Qualifiers[0].Type, Is.EqualTo(VdbAltNameQualifierType.Comment));
        Assert.That(result[2].Qualifiers[0].Content, Is.EqualTo("q2"));
        
        Assert.That(result[3].Name, Is.EqualTo("Name4"));
        Assert.That(result[3].Qualifiers, Has.Count.EqualTo(2));
        Assert.That(result[3].Qualifiers[0].Type, Is.EqualTo(VdbAltNameQualifierType.Pronunciation));
        Assert.That(result[3].Qualifiers[0].Content, Is.EqualTo("q3"));
        Assert.That(result[3].Qualifiers[1].Type, Is.EqualTo(VdbAltNameQualifierType.Comment));
        Assert.That(result[3].Qualifiers[1].Content, Is.EqualTo("q4"));
        
        Assert.That(result[4].Name, Is.EqualTo("Name5"));
        Assert.That(result[4].Qualifiers, Has.Count.EqualTo(3));
        Assert.That(result[4].Qualifiers[0].Type, Is.EqualTo(VdbAltNameQualifierType.Pronunciation));
        Assert.That(result[4].Qualifiers[0].Content, Is.EqualTo("q5"));
        Assert.That(result[4].Qualifiers[1].Type, Is.EqualTo(VdbAltNameQualifierType.Pronunciation));
        Assert.That(result[4].Qualifiers[1].Content, Is.EqualTo("q6"));
        Assert.That(result[4].Qualifiers[2].Type, Is.EqualTo(VdbAltNameQualifierType.Comment));
        Assert.That(result[4].Qualifiers[2].Content, Is.EqualTo("q7"));
    }

    [Test]
    public void TestParseEmptyQualifiers()
    {
        // Arrange
        string input = "Name1 [] ()";
        
        // Act
        List<VdbAltName> result = VdbAnalysisData.ParseAltNamesWithQualifiersPublic(input);
        
        // Assert
        Assert.That(result, Has.Count.EqualTo(1));
        Assert.That(result[0].Name, Is.EqualTo("Name1"));
        Assert.That(result[0].Qualifiers, Has.Count.EqualTo(2));
        Assert.That(result[0].Qualifiers[0].Type, Is.EqualTo(VdbAltNameQualifierType.Pronunciation));
        Assert.That(result[0].Qualifiers[0].Content, Is.EqualTo(""));
        Assert.That(result[0].Qualifiers[1].Type, Is.EqualTo(VdbAltNameQualifierType.Comment));
        Assert.That(result[0].Qualifiers[1].Content, Is.EqualTo(""));
    }

    [Test]
    public void TestParseAlternatingBracketTypes()
    {
        // Arrange
        string input = "Name1 [q1] (q2) [q3], Name2 (q4) [q5] (q6)";
        
        // Act
        List<VdbAltName> result = VdbAnalysisData.ParseAltNamesWithQualifiersPublic(input);
        
        // Assert
        Assert.That(result, Has.Count.EqualTo(2));
        
        Assert.That(result[0].Name, Is.EqualTo("Name1"));
        Assert.That(result[0].Qualifiers, Has.Count.EqualTo(3));
        Assert.That(result[0].Qualifiers[0].Type, Is.EqualTo(VdbAltNameQualifierType.Pronunciation));
        Assert.That(result[0].Qualifiers[0].Content, Is.EqualTo("q1"));
        Assert.That(result[0].Qualifiers[1].Type, Is.EqualTo(VdbAltNameQualifierType.Comment));
        Assert.That(result[0].Qualifiers[1].Content, Is.EqualTo("q2"));
        Assert.That(result[0].Qualifiers[2].Type, Is.EqualTo(VdbAltNameQualifierType.Pronunciation));
        Assert.That(result[0].Qualifiers[2].Content, Is.EqualTo("q3"));
        
        Assert.That(result[1].Name, Is.EqualTo("Name2"));
        Assert.That(result[1].Qualifiers, Has.Count.EqualTo(3));
        Assert.That(result[1].Qualifiers[0].Type, Is.EqualTo(VdbAltNameQualifierType.Comment));
        Assert.That(result[1].Qualifiers[0].Content, Is.EqualTo("q4"));
        Assert.That(result[1].Qualifiers[1].Type, Is.EqualTo(VdbAltNameQualifierType.Pronunciation));
        Assert.That(result[1].Qualifiers[1].Content, Is.EqualTo("q5"));
        Assert.That(result[1].Qualifiers[2].Type, Is.EqualTo(VdbAltNameQualifierType.Comment));
        Assert.That(result[1].Qualifiers[2].Content, Is.EqualTo("q6"));
    }

    [Test]
    public void TestParseQualifiersWithNestedCommas()
    {
        // Arrange
        string input = "Name1 [a, b, c], Name2 (x, y, z)";
        
        // Act
        List<VdbAltName> result = VdbAnalysisData.ParseAltNamesWithQualifiersPublic(input);
        
        // Assert
        Assert.That(result, Has.Count.EqualTo(2));
        
        Assert.That(result[0].Name, Is.EqualTo("Name1"));
        Assert.That(result[0].Qualifiers, Has.Count.EqualTo(1));
        Assert.That(result[0].Qualifiers[0].Type, Is.EqualTo(VdbAltNameQualifierType.Pronunciation));
        Assert.That(result[0].Qualifiers[0].Content, Is.EqualTo("a, b, c"));
        
        Assert.That(result[1].Name, Is.EqualTo("Name2"));
        Assert.That(result[1].Qualifiers, Has.Count.EqualTo(1));
        Assert.That(result[1].Qualifiers[0].Type, Is.EqualTo(VdbAltNameQualifierType.Comment));
        Assert.That(result[1].Qualifiers[0].Content, Is.EqualTo("x, y, z"));
    }

    [Test]
    public void TestParseSixNamesComplexCombination()
    {
        // Arrange
        string input = "Name1, Name2 [q1], Name3 (q2, q3), Name4 [q4] (q5), Name5 [q6, q7] [q8], Name6 (q9) (q10) [q11]";
        
        // Act
        List<VdbAltName> result = VdbAnalysisData.ParseAltNamesWithQualifiersPublic(input);
        
        // Assert
        Assert.That(result, Has.Count.EqualTo(6));
        
        Assert.That(result[0].Name, Is.EqualTo("Name1"));
        Assert.That(result[0].Qualifiers, Has.Count.EqualTo(0));
        
        Assert.That(result[1].Name, Is.EqualTo("Name2"));
        Assert.That(result[1].Qualifiers, Has.Count.EqualTo(1));
        Assert.That(result[1].Qualifiers[0].Type, Is.EqualTo(VdbAltNameQualifierType.Pronunciation));
        Assert.That(result[1].Qualifiers[0].Content, Is.EqualTo("q1"));
        
        Assert.That(result[2].Name, Is.EqualTo("Name3"));
        Assert.That(result[2].Qualifiers, Has.Count.EqualTo(1));
        Assert.That(result[2].Qualifiers[0].Type, Is.EqualTo(VdbAltNameQualifierType.Comment));
        Assert.That(result[2].Qualifiers[0].Content, Is.EqualTo("q2, q3"));
        
        Assert.That(result[3].Name, Is.EqualTo("Name4"));
        Assert.That(result[3].Qualifiers, Has.Count.EqualTo(2));
        Assert.That(result[3].Qualifiers[0].Type, Is.EqualTo(VdbAltNameQualifierType.Pronunciation));
        Assert.That(result[3].Qualifiers[0].Content, Is.EqualTo("q4"));
        Assert.That(result[3].Qualifiers[1].Type, Is.EqualTo(VdbAltNameQualifierType.Comment));
        Assert.That(result[3].Qualifiers[1].Content, Is.EqualTo("q5"));
        
        Assert.That(result[4].Name, Is.EqualTo("Name5"));
        Assert.That(result[4].Qualifiers, Has.Count.EqualTo(2));
        Assert.That(result[4].Qualifiers[0].Type, Is.EqualTo(VdbAltNameQualifierType.Pronunciation));
        Assert.That(result[4].Qualifiers[0].Content, Is.EqualTo("q6, q7"));
        Assert.That(result[4].Qualifiers[1].Type, Is.EqualTo(VdbAltNameQualifierType.Pronunciation));
        Assert.That(result[4].Qualifiers[1].Content, Is.EqualTo("q8"));
        
        Assert.That(result[5].Name, Is.EqualTo("Name6"));
        Assert.That(result[5].Qualifiers, Has.Count.EqualTo(3));
        Assert.That(result[5].Qualifiers[0].Type, Is.EqualTo(VdbAltNameQualifierType.Comment));
        Assert.That(result[5].Qualifiers[0].Content, Is.EqualTo("q9"));
        Assert.That(result[5].Qualifiers[1].Type, Is.EqualTo(VdbAltNameQualifierType.Comment));
        Assert.That(result[5].Qualifiers[1].Content, Is.EqualTo("q10"));
        Assert.That(result[5].Qualifiers[2].Type, Is.EqualTo(VdbAltNameQualifierType.Pronunciation));
        Assert.That(result[5].Qualifiers[2].Content, Is.EqualTo("q11"));
    }
}
