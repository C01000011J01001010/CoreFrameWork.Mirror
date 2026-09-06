using System;
using System.Collections.Generic;

namespace CoreEngine.StaticData.Editor
{
    /// <summary>One validated CSV record addressed by its header names.</summary>
    public sealed class CsvRow
    {
        private readonly IReadOnlyDictionary<string, string> values;

        internal CsvRow(int lineNumber, IReadOnlyDictionary<string, string> values)
        {
            LineNumber = lineNumber;
            this.values = values;
        }

        public int LineNumber { get; }
        public bool TryGet(string header, out string value) => values.TryGetValue(header, out value);
        public string GetRequired(string header)
        {
            if (!values.TryGetValue(header, out var value))
                throw new InvalidOperationException($"Missing required CSV header '{header}'.");
            return value;
        }
    }
}
