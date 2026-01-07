using NUnit.Framework;

namespace Osmalyzer.Tests;

[TestFixture]
public class CsvParserTests
{
    [Test]
    public void TestParseLine_Simple()
    {
        string line = "field1,field2,field3";
        string[] expected = [ "field1", "field2", "field3" ];
        string[] result = CsvParser.ParseLine(line, ',');
        Assert.That(expected, Is.EquivalentTo(result));
    }

    [Test]
    public void TestParseLine_QuotedFields()
    {
        string line = "\"field1, with comma\",\"field2\",\"field3\"";
        string[] expected = [ "field1, with comma", "field2", "field3" ];
        string[] result = CsvParser.ParseLine(line, ',');
        Assert.That(expected, Is.EquivalentTo(result));
    }

    [Test]
    public void TestParseLine_EscapedQuotes()
    {
        string line = "\"field1 with \"\"escaped quotes\"\"\",\"field2\",\"field3\"";
        string[] expected = [ "field1 with \"escaped quotes\"", "field2", "field3" ];
        string[] result = CsvParser.ParseLine(line, ',');
        Assert.That(expected, Is.EquivalentTo(result));
    }

    [Test]
    public void TestParseLine_Mixed()
    {
        string line = "simpleField,\"quoted, field with comma\",\"field with \"\"escaped quotes\"\"\",anotherSimpleField";
        string[] expected = [ "simpleField", "quoted, field with comma", "field with \"escaped quotes\"", "anotherSimpleField" ];
        string[] result = CsvParser.ParseLine(line, ',');
        Assert.That(expected, Is.EquivalentTo(result));
    }
}