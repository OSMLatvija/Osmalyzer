using Osmalyzer;

namespace CoreTests;

[TestFixture]
public class OsmOpeningHoursHelperTests
{
    // Middle of week
    [TestCase(new[] { "Tu 08:00-12:00", "We 08:00-12:00" }, new[] { "Tu-We 08:00-12:00" })]
    // Start of week
    [TestCase(new[] { "Mo 08:00-12:00", "Tu 08:00-12:00" }, new[] { "Mo-Tu 08:00-12:00" })]
    // End of week
    [TestCase(new[] { "Sa 08:00-12:00", "Su 08:00-12:00" }, new[] { "Sa-Su 08:00-12:00" })]
    // Three in a row
    [TestCase(new[] { "Tu 08:00-12:00", "We 08:00-12:00", "Th 08:00-12:00" }, new[] { "Tu-Th 08:00-12:00" })]
    // Non-matching times
    [TestCase(new[] { "Tu 08:00-12:00", "We 09:00-13:00" }, new[] { "Tu 08:00-12:00", "We 09:00-13:00" })]
    // Non-sequential days
    [TestCase(new[] { "Tu 08:00-12:00", "Th 08:00-12:00" }, new[] { "Tu 08:00-12:00", "Th 08:00-12:00" })]
    // First two matching
    [TestCase(new[] { "Tu 08:00-12:00", "We 08:00-12:00", "Th 09:00-13:00" }, new[] { "Tu-We 08:00-12:00", "Th 09:00-13:00" })]
    // Last two matching
    [TestCase(new[] { "Tu 08:00-12:00", "We 09:00-13:00", "Th 09:00-13:00" }, new[] { "Tu 08:00-12:00", "We-Th 09:00-13:00" })]
    // Last similar but with gap
    [TestCase(new[] { "Tu 08:00-12:00", "We 08:00-12:00", "Fr 08:00-12:00" }, new[] { "Tu-We 08:00-12:00", "Fr 08:00-12:00" })]
    // Only one input
    [TestCase(new[] { "Tu 08:00-12:00" }, new[] { "Tu 08:00-12:00" })]
    // Same day twice
    [TestCase(new[] { "Tu 08:00-12:00", "Tu 08:00-12:00" }, new[] { "Tu 08:00-12:00", "Tu 08:00-12:00" })]
    // Inputs too short
    [TestCase(new[] { "Tu", "We" }, new[] { "Tu", "We" })]
    // First input too short
    [TestCase(new[] { "Tu", "We 08:00-12:00" }, new[] { "Tu", "We 08:00-12:00" })]
    // Second input too short
    [TestCase(new[] { "Tu 08:00-12:00", "We" }, new[] { "Tu 08:00-12:00", "We" })]
    // First weekday invalid
    [TestCase(new[] { "Xx 08:00-12:00", "We 08:00-12:00" }, new[] { "Xx 08:00-12:00", "We 08:00-12:00" })]
    // Second weekday invalid
    [TestCase(new[] { "Tu 08:00-12:00", "Xx 08:00-12:00" }, new[] { "Tu 08:00-12:00", "Xx 08:00-12:00" })]
    public void MergeSequentialWeekdaysWithSameTimes_MergesCorrectly(string[] input, string[] expected)
    {
        List<string> result = OsmOpeningHoursHelper.MergeSequentialWeekdaysWithSameTimes(input);
        Assert.That(result, Is.EqualTo(expected));
    }
}