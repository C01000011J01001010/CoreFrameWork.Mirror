using System;
using System.Collections.Generic;
using System.Text;

namespace CoreEngine.StaticData.Editor
{
    /// <summary>Small RFC-4180-style parser supporting commas, quotes, escaped quotes, and CRLF/LF lines.</summary>
    public static class CsvDocumentParser
    {
        public static bool TryParse(string source, out List<CsvRow> rows, out string error)
        {
            rows = new List<CsvRow>();
            error = null;
            if (string.IsNullOrWhiteSpace(source)) { error = "CSV is empty."; return false; }

            var records = new List<List<string>>();
            var recordLines = new List<int>();
            var record = new List<string>();
            var value = new StringBuilder();
            var line = 1;
            var recordLine = 1;
            var quoted = false;

            for (var index = 0; index < source.Length; index++)
            {
                var character = source[index];
                if (quoted)
                {
                    if (character != '"') { value.Append(character); continue; }
                    if (index + 1 < source.Length && source[index + 1] == '"') { value.Append('"'); index++; continue; }
                    quoted = false;
                    continue;
                }

                if (character == '"')
                {
                    if (value.Length != 0) { error = $"Unexpected quote at line {line}."; return false; }
                    quoted = true;
                    continue;
                }
                if (character == ',') { record.Add(value.ToString()); value.Clear(); continue; }
                if (character == '\r' || character == '\n')
                {
                    if (character == '\r' && index + 1 < source.Length && source[index + 1] == '\n') index++;
                    record.Add(value.ToString()); value.Clear();
                    if (!IsEmptyRecord(record)) { records.Add(record); recordLines.Add(recordLine); }
                    record = new List<string>();
                    line++;
                    recordLine = line;
                    continue;
                }
                value.Append(character);
            }

            if (quoted) { error = $"Unclosed quoted field starting at line {recordLine}."; return false; }
            record.Add(value.ToString());
            if (!IsEmptyRecord(record)) { records.Add(record); recordLines.Add(recordLine); }
            if (records.Count < 2) { error = "CSV requires one header row and at least one data row."; return false; }

            var headers = new List<string>(records[0].Count);
            var headerSet = new HashSet<string>(StringComparer.Ordinal);
            for (var index = 0; index < records[0].Count; index++)
            {
                var header = records[0][index].Trim();
                if (string.IsNullOrEmpty(header)) { error = $"Header column {index + 1} is empty."; return false; }
                if (!headerSet.Add(header)) { error = $"Duplicate header '{header}'."; return false; }
                headers.Add(header);
            }

            for (var recordIndex = 1; recordIndex < records.Count; recordIndex++)
            {
                var recordValues = records[recordIndex];
                if (recordValues.Count != headers.Count)
                {
                    error = $"Line {recordLines[recordIndex]} has {recordValues.Count} columns; expected {headers.Count}.";
                    return false;
                }
                var values = new Dictionary<string, string>(headers.Count, StringComparer.Ordinal);
                for (var column = 0; column < headers.Count; column++) values.Add(headers[column], recordValues[column].Trim());
                rows.Add(new CsvRow(recordLines[recordIndex], values));
            }
            return true;
        }

        private static bool IsEmptyRecord(IReadOnlyList<string> record)
        {
            for (var index = 0; index < record.Count; index++) if (!string.IsNullOrWhiteSpace(record[index])) return false;
            return true;
        }
    }
}
