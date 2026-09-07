using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using CoreEngine.StaticData;

namespace CoreEditor.StaticData
{
    public interface IStaticDataCsvRowFactory<TRow> where TRow : class, IStaticDataRow
    {
        bool TryCreate(CsvRow source, out TRow row, out string error);
    }

    public static class StaticDataCsvImporter
    {
        /// <summary>Parses and validates all rows before changing the target Table asset.</summary>
        public static bool TryImport<TRow>(TextAsset source, StaticDataTable<TRow> table, IStaticDataCsvRowFactory<TRow> factory, out string error)
            where TRow : class, IStaticDataRow
        {
            error = null;
            if (source == null) { error = "CSV source is null."; return false; }
            if (table == null) { error = "Target Table is null."; return false; }
            if (factory == null) { error = "Row factory is null."; return false; }
            if (!CsvDocumentParser.TryParse(source.text, out var parsedRows, out error)) return false;

            var candidates = new List<TRow>(parsedRows.Count);
            foreach (var parsedRow in parsedRows)
            {
                if (!factory.TryCreate(parsedRow, out var candidate, out var rowError))
                {
                    error = $"Line {parsedRow.LineNumber}: {rowError}";
                    return false;
                }
                candidates.Add(candidate);
            }

            if (!table.TryReplaceRowsFromImport(candidates, out error)) return false;
            EditorUtility.SetDirty(table);
            AssetDatabase.SaveAssets();
            return true;
        }
    }
}
