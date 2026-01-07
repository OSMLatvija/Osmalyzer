namespace Osmalyzer;

public static class CsvParser
{
    /// <summary>
    /// Parse a CSV line handling quoted fields properly
    /// </summary>
    public static string[] ParseLine(string line, char separator, char quoteChar = '"')
    {
        List<string> fields = [ ];
        bool inQuotes = false;
        StringBuilder currentField = new StringBuilder();

        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];

            if (c == quoteChar)
            {
                if (inQuotes && i + 1 < line.Length && line[i + 1] == quoteChar)
                {
                    // escaped quote
                    currentField.Append(quoteChar);
                    i++; // skip next quote
                }
                else
                {
                    // toggle quote mode
                    inQuotes = !inQuotes;
                }
            }
            else if (c == separator && !inQuotes)
            {
                // field separator
                fields.Add(currentField.ToString());
                currentField.Clear();
            }
            else
            {
                currentField.Append(c);
            }
        }

        // add last field
        fields.Add(currentField.ToString());

        return fields.ToArray();
    }
}