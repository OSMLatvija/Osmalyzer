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

    /// <summary>
    /// Parse full CSV file content into records, handling quoted fields that span multiple lines
    /// </summary>
    public static List<string[]> ParseAll(string content, char separator, char quoteChar = '"')
    {
        List<string[]> records = [];
        List<string> fields = [];
        StringBuilder currentField = new StringBuilder();
        bool inQuotes = false;

        for (int i = 0; i < content.Length; i++)
        {
            char c = content[i];

            if (c == quoteChar)
            {
                if (inQuotes && i + 1 < content.Length && content[i + 1] == quoteChar)
                {
                    // escaped quote
                    currentField.Append(quoteChar);
                    i++;
                }
                else
                {
                    inQuotes = !inQuotes;
                }
            }
            else if (c == separator && !inQuotes)
            {
                fields.Add(currentField.ToString());
                currentField.Clear();
            }
            else if ((c == '\n' || (c == '\r' && i + 1 < content.Length && content[i + 1] == '\n')) && !inQuotes)
            {
                if (c == '\r') i++; // skip \n of \r\n

                fields.Add(currentField.ToString());
                currentField.Clear();

                if (fields.Count > 0)
                    records.Add(fields.ToArray());

                fields.Clear();
            }
            else if (c == '\r' && !inQuotes)
            {
                // lone \r, treat as record end
                fields.Add(currentField.ToString());
                currentField.Clear();

                if (fields.Count > 0)
                    records.Add(fields.ToArray());

                fields.Clear();
            }
            else
            {
                currentField.Append(c);
            }
        }

        // last record if file doesn't end with newline
        fields.Add(currentField.ToString());

        if (fields.Any(f => f.Length > 0))
            records.Add(fields.ToArray());

        return records;
    }
}