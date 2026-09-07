using System.Globalization;
using CoreEngine.StaticData.Samples;
using UnityEditor;
using UnityEngine;
using CoreEngine.StaticData;

namespace CoreEditor.StaticData
{
    /// <summary>Reference importer. Required CSV headers: Id, DisplayName, Price.</summary>
    public static class ItemTableCsvImporter
    {
        [MenuItem("CoreFramework/Static Data/Import Selected Sample Item CSV")]
        private static void ImportSelected()
        {
            TextAsset csv = null;
            ItemTable table = null;
            foreach (var selected in Selection.objects)
            {
                if (selected is TextAsset textAsset) csv = textAsset;
                if (selected is ItemTable itemTable) table = itemTable;
            }

            if (!StaticDataCsvImporter.TryImport(csv, table, new ItemRowFactory(), out var error))
            {
                EditorUtility.DisplayDialog("Item CSV Import Failed", error, "OK");
                return;
            }
            EditorUtility.DisplayDialog("Item CSV Import", $"Imported {csv.name} into {table.name}.", "OK");
        }

        private sealed class ItemRowFactory : IStaticDataCsvRowFactory<ItemRow>
        {
            public bool TryCreate(CsvRow source, out ItemRow row, out string error)
            {
                row = null;
                error = null;
                if (!int.TryParse(source.GetRequired("Id"), NumberStyles.None, CultureInfo.InvariantCulture, out var id)) { error = "Id must be an integer."; return false; }
                if (!StaticDataId.IsValid(id)) { error = $"Id must be {StaticDataId.MinValue}..{StaticDataId.MaxValue}."; return false; }
                var displayName = source.GetRequired("DisplayName");
                if (string.IsNullOrWhiteSpace(displayName)) { error = "DisplayName is required."; return false; }
                if (!int.TryParse(source.GetRequired("Price"), NumberStyles.Integer, CultureInfo.InvariantCulture, out var price)) { error = "Price must be an integer."; return false; }
                row = new ItemRow { id = id, displayName = displayName, price = price };
                return true;
            }
        }
    }
}
