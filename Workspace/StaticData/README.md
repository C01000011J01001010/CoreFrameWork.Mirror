# Static Data — first implementation slice

This isolated draft does not modify the legacy CSV converter, which creates one ScriptableObject per row.

- `StaticDataId`: ID range `1..999999999` and a nine-digit file-name prefix (`42` → `000000042`).
- `IStaticDataRow`: row primary-key contract.
- `StaticDataTable<TRow>`: serialized rows, all-or-nothing cache initialization, and ID lookup.
- `ItemRow` / `ItemTable`: Unity sample for the first validation pass.

## CSV importer

The Editor slice parses quoted CSV values, validates headers and column counts, creates every candidate row in memory, then replaces the Table only when the complete batch is valid. The sample importer expects this header row:

```csv
Id,DisplayName,Price
1001,Iron Sword,1500
1002,"Shield, Heavy",2500
```

Select both the CSV TextAsset and an `ItemTable` asset, then run **CoreFramework > Static Data > Import Selected Sample Item CSV**.

Next: parse CSV into temporary rows, validate the entire batch, then atomically replace the table. `AssetId<T>` is deferred until Unity generic serialization is verified.
