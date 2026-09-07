using CoreEngine.Helpers;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace CoreEngine.StaticData
{
    /// <summary>A concrete non-generic subclass (for example ItemTable) is the Unity asset.</summary>
    public abstract class StaticDataTable<TRow> : ScriptableObject, ISerializationCallbackReceiver
        where TRow : class, IStaticDataRow
    {
        [SerializeField] private TRow[] rows = Array.Empty<TRow>();
        [NonSerialized] private Dictionary<int, TRow> cache;
        [NonSerialized] private string initializationError;
        public IReadOnlyList<TRow> Rows => rows;
        public bool IsInitialized => cache != null;
        public string InitializationError => initializationError;

        /// <summary>Builds an all-or-nothing runtime cache; duplicate or invalid IDs produce no cache.</summary>
        public bool TryInitialize(out string error)
        {
            var nextCache = new Dictionary<int, TRow>(rows.Length);
            for (var index = 0; index < rows.Length; index++)
            {
                var row = rows[index];
                if (row == null) return Fail($"Row {index} is null.", out error);
                if (!StaticDataId.IsValid(row.Id)) return Fail($"Row {index} has invalid ID {row.Id}.", out error);
                if (!nextCache.TryAdd(row.Id, row)) return Fail($"Duplicate row ID {row.Id}.", out error);
            }
            cache = nextCache; initializationError = null; error = null; return true;
        }

        public void Initialize() { if (!TryInitialize(out var error)) throw new InvalidOperationException($"Could not initialize {name}: {error}"); }
        public bool TryGet(int id, out TRow row) { row = null; return cache != null && StaticDataId.IsValid(id) && cache.TryGetValue(id, out row); }
        public TRow Get(int id)
        {
            if (cache == null) throw new InvalidOperationException($"{name} has not been initialized.");
            if (!TryGet(id, out var row)) throw new KeyNotFoundException($"{name} does not contain row ID {id}.");
            return row;
        }

        /// <summary>For a future importer, after its entire input batch has validated.</summary>
        protected void ReplaceRows(IReadOnlyList<TRow> importedRows)
        {
            if (importedRows == null) throw new ArgumentNullException(nameof(importedRows));
            var replacement = new TRow[importedRows.Count];
            for (var index = 0; index < importedRows.Count; index++) replacement[index] = importedRows[index];
            Array.Sort(replacement, CompareRowsById); rows = replacement; cache = null; initializationError = null;
        }

#if UNITY_EDITOR
        /// <summary>
        /// Replaces the serialized table only after every imported row has validated. Editor importers
        /// call this method; it intentionally does not save the asset so callers control persistence.
        /// </summary>
        public bool TryReplaceRowsFromImport(IReadOnlyList<TRow> importedRows, out string error)
        {
            if (importedRows == null)
            {
                error = "Imported rows are null.";
                return false;
            }

            var candidate = new TRow[importedRows.Count];
            for (var index = 0; index < importedRows.Count; index++) candidate[index] = importedRows[index];

            if (!TryValidateRows(candidate, out error)) return false;

            Array.Sort(candidate, CompareRowsById);
            rows = candidate;
            cache = null;
            initializationError = null;
            return true;
        }
#endif
        public void OnBeforeSerialize() { }
        public void OnAfterDeserialize() { cache = null; initializationError = null; }
        private bool Fail(string message, out string error) { cache = null; initializationError = message; error = message; return false; }

        private static bool TryValidateRows(IReadOnlyList<TRow> candidateRows, out string error)
        {
            var ids = new HashSet<int>();
            for (var index = 0; index < candidateRows.Count; index++)
            {
                var row = candidateRows[index];
                if (row == null) { error = $"Row {index} is null."; return false; }
                if (!StaticDataId.IsValid(row.Id)) { error = $"Row {index} has invalid ID {row.Id}."; return false; }
                if (!ids.Add(row.Id)) { error = $"Duplicate row ID {row.Id}."; return false; }
            }
            error = null;
            return true;
        }
        private static int CompareRowsById(TRow left, TRow right)
        {
            if (ReferenceEquals(left, right)) return 0;
            if (left == null) return 1;
            if (right == null) return -1;
            return left.Id.CompareTo(right.Id);
        }
    }
}
