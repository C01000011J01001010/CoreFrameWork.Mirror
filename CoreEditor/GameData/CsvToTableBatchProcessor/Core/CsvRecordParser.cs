using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using CoreEngine.GameData;
using CoreEditor.Helpers;
using CoreEngine.Helpers;
using CoreEditor.GameData.Validation;
using UnityEngine;

namespace CoreEditor.GameData
{
    public static class CsvRecordParser
    {
        internal static List<IRecord> Parse<TRecord>(string csvText, TableMetaData metaData, out List<string> extractedKeys)
            where TRecord : BaseRecord, IRecord, IEditorRecordSetup, new()
        {
            string[] lines = csvText.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');

            // 1. 헤더 추출 및 C# 필드 분리 캐싱 (전체 필드 vs [TableColumn] 필드)
            var headers = ExtractSchemaHeaders(lines, out int schemaRowIndex);
            var allFields = GetAllRecordFields<TRecord>();
            var tableColumnFields = allFields.Where(f => f.GetCustomAttribute<TableColumnAttribute>() != null).ToList();

            // CSV 컬럼은 [TableColumn] 필드와 매핑, FK는 전체 필드와 매핑
            ValidateAndMapFields(headers, tableColumnFields);
            var fkFieldMap = ValidateAndMapForeignKeys(metaData.foreignKeys, allFields);

            List<IRecord> temporaryRecords = new List<IRecord>();
            Dictionary<ulong, string> usedIdsMap = new Dictionary<ulong, string>();
            extractedKeys = new List<string>();

            for (int row = schemaRowIndex + 1; row < lines.Length; row++)
            {
                string line = lines[row];
                if (string.IsNullOrWhiteSpace(line)) continue;

                string[] columns = CsvParserHelper.ParseLine(line);
                TRecord record = new TRecord();

                // 2. 일반 컬럼 데이터 파싱, 필드 주입 및 제약조건 검사
                var rowRawValues = ParseRowFields(record, headers, columns, row);

                // 3. 다중 컬럼 기본키(PK) 조합 및 해싱
                (ulong recordId, string rawKey) = GeneratePrimaryKey(metaData.primaryKey, rowRawValues, row);

                // 4. ID 해시 충돌 및 중복 검사
                ValidateKeyCollision(usedIdsMap, recordId, rawKey, row);

                // 5. 다중 컬럼 외래키(FK) 조합 및 주입
                InjectForeignKeys(record, metaData.foreignKeys, fkFieldMap, rowRawValues, row);

                // 6. 최종 등록 및 캐싱
                usedIdsMap.Add(recordId, rawKey);
                extractedKeys.Add(rawKey);

                ((IEditorRecordSetup)record).EditorSetupId(recordId, rawKey);
                temporaryRecords.Add(record);
            }
            return temporaryRecords;
        }

        #region 파싱 헬퍼 메서드

        /// <summary>
        /// 레코드 타입의 모든 인스턴스 필드를 리플렉션으로 가져옵니다. ([TableColumn] 유무 무관)
        /// </summary>
        private static List<FieldInfo> GetAllRecordFields<TRecord>()
        {
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;
            List<FieldInfo> allFields = new List<FieldInfo>();
            Type currentType = typeof(TRecord);

            while (currentType != null && currentType != typeof(BaseRecord) && currentType != typeof(object))
            {
                allFields.AddRange(currentType.GetFields(flags));
                currentType = currentType.BaseType;
            }
            return allFields;
        }

        private static void ValidateAndMapFields(List<CsvColumnSchema> csvHeaders, List<FieldInfo> tableColumnFields)
        {
            foreach (var header in csvHeaders)
            {
                FieldInfo matchedField = tableColumnFields.FirstOrDefault(f => string.Equals(f.Name, header.FieldName, StringComparison.OrdinalIgnoreCase));
                if (matchedField == null)
                    throw new Exception($"C# 클래스에 [TableColumn]이 선언된 '{header.FieldName}' 필드가 없습니다.");

                header.Field = matchedField;
                header.FieldType = matchedField.FieldType;
            }
        }

        /// <summary>
        /// 메타데이터에 정의된 외래키(FK) 이름이 C# 레코드 클래스의 필드와 일치하는지 사전 검증하고 캐싱합니다.
        /// </summary>
        private static Dictionary<string, FieldInfo> ValidateAndMapForeignKeys(
            Dictionary<string, ForeignKeyMeta> foreignKeysMeta, List<FieldInfo> allFields)
        {
            var fkFieldMap = new Dictionary<string, FieldInfo>();
            if (foreignKeysMeta == null || foreignKeysMeta.Count == 0) return fkFieldMap;

            foreach (var kvp in foreignKeysMeta)
            {
                string fkFieldName = kvp.Key;
                FieldInfo fkField = allFields.FirstOrDefault(f => f.Name == fkFieldName);

                if (fkField == null)
                {
                    throw new Exception($"[FK 매핑 오류] 메타데이터의 FK 이름('{fkFieldName}')과 일치하는 C# 레코드 클래스의 필드가 없습니다! 파일명과 필드명을 확인해주세요.");
                }

                if (!fkField.IsPublic && fkField.GetCustomAttribute<SerializeField>() == null)
                {
                    throw new Exception($"[FK 직렬화 오류] C# 레코드의 '{fkFieldName}' 필드에 [SerializeField] 어트리뷰트가 누락되어 SO에 저장할 수 없습니다.");
                }

                fkFieldMap[fkFieldName] = fkField;
            }

            return fkFieldMap;
        }

        private static Dictionary<string, string> ParseRowFields<TRecord>(
            TRecord record, List<CsvColumnSchema> headers, string[] columns, int row)
        {
            var rowRawValues = new Dictionary<string, string>();

            for (int i = 0; i < headers.Count; i++)
            {
                var schema = headers[i];
                string rawValue = schema.ColumnIndex < columns.Length ? columns[schema.ColumnIndex].Trim() : string.Empty;

                if (string.IsNullOrEmpty(rawValue) && schema.HasDefault) rawValue = schema.DefaultValue;
                if (string.IsNullOrEmpty(rawValue) && schema.IsNotNull)
                    throw new Exception($"[행: {row + 1}] '{schema.FieldName}' 컬럼(NOT NULL)이 비어있습니다.");

                object typedValue = null;
                try
                {
                    typedValue = ConvertValue(rawValue, schema.FieldType, row, schema.FieldName);
                    schema.Field.SetValue(record, typedValue);
                }
                catch (Exception ex)
                {
                    throw new Exception($"[행: {row + 1}, 열: '{schema.FieldName}'] 값 변환 실패: {ex.Message}");
                }

                EvaluateConstraints(schema, typedValue, row);
                rowRawValues[schema.FieldName] = rawValue;
            }

            return rowRawValues;
        }

        private static void EvaluateConstraints(CsvColumnSchema schema, object typedValue, int row)
        {
            if (typedValue == null || schema.CheckExpressions.Count == 0) return;

            Array checkArray = schema.IsArray ? (Array)typedValue : new object[] { typedValue };
            foreach (var item in checkArray)
            {
                foreach (var expr in schema.CheckExpressions)
                {
                    if (!ConstraintEvaluator.EvaluateCheck(expr, item))
                        throw new Exception($"[행: {row + 1}] '{schema.FieldName}' 값이 CHECK({expr}) 조건을 위반했습니다.");
                }
            }
        }

        private static (ulong id, string rawKey) GeneratePrimaryKey(string[] primaryKeyMeta, Dictionary<string, string> rowRawValues, int row)
        {
            List<string> pkParts = new List<string>();
            foreach (string pkName in primaryKeyMeta)
            {
                if (rowRawValues.TryGetValue(pkName, out string val)) pkParts.Add(val);
                else throw new Exception($"[행: {row + 1}] PK 컬럼('{pkName}')을 찾을 수 없습니다.");
            }

            string rawKey = string.Join(Constants.KeySeparator, pkParts);
            ulong recordId = HashHelper.StringToId(rawKey);

            return (recordId, rawKey);
        }

        private static void ValidateKeyCollision(Dictionary<ulong, string> usedIdsMap, ulong recordId, string rawKey, int row)
        {
            if (usedIdsMap.TryGetValue(recordId, out string existingKey))
            {
                if (existingKey != rawKey)
                    throw new Exception($"[CoreEngine FATAL] 해시 충돌! Key A: '{existingKey}', Key B: '{rawKey}' (ID: {recordId})");
                else
                    throw new Exception($"[행: {row + 1}] 중복된 기본키 데이터('{rawKey}')가 존재합니다.");
            }
        }

        private static void InjectForeignKeys<TRecord>(
            TRecord record,
            Dictionary<string, ForeignKeyMeta> foreignKeysMeta,
            Dictionary<string, FieldInfo> fkFieldMap,
            Dictionary<string, string> rowRawValues,
            int row)
        {
            if (foreignKeysMeta == null || foreignKeysMeta.Count == 0) return;

            foreach (var kvp in foreignKeysMeta)
            {
                string fkFieldName = kvp.Key;
                string[] fkColumns = kvp.Value.columns;

                List<string> fkParts = new List<string>();
                foreach (string colName in fkColumns)
                {
                    if (rowRawValues.TryGetValue(colName, out string val)) fkParts.Add(val);
                    else throw new Exception($"[행: {row + 1}] FK 조합용 컬럼('{colName}')을 찾을 수 없습니다.");
                }

                if (fkParts.All(string.IsNullOrEmpty)) continue;

                string rawFkKey = string.Join(Constants.KeySeparator, fkParts);
                ulong targetFkId = HashHelper.StringToId(rawFkKey);

                FieldInfo fkField = fkFieldMap[fkFieldName];
                object fkInstance = Activator.CreateInstance(fkField.FieldType);

                FieldInfo[] internalFields = fkField.FieldType.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                foreach (var field in internalFields)
                {
                    if (field.FieldType == typeof(ulong))
                    {
                        field.SetValue(fkInstance, targetFkId);
                    }
                    else if (field.FieldType == typeof(string))
                    {
                        field.SetValue(fkInstance, rawFkKey);
                    }
                }

                fkField.SetValue(record, fkInstance);
            }
        }

        private static List<CsvColumnSchema> ExtractSchemaHeaders(string[] lines, out int schemaRowIndex)
        {
            schemaRowIndex = -1;
            string[] schemaColumns = null;

            for (int r = 0; r < lines.Length; r++)
            {
                if (string.IsNullOrWhiteSpace(lines[r])) continue;
                string[] cols = CsvParserHelper.ParseLine(lines[r]);

                if (cols.Any(c => c.Trim().Length >= 2 && c.Trim()[0] == '{' && c.Trim()[^1] == '}'))
                {
                    schemaRowIndex = r;
                    schemaColumns = cols;
                    break;
                }
            }

            if (schemaRowIndex < 0) throw new Exception("CSV 헤더 영역을 찾지 못했습니다.");

            var csvHeaders = new List<CsvColumnSchema>();
            for (int col = 0; col < schemaColumns.Length; col++)
            {
                string value = schemaColumns[col].Trim();
                if (value.Length >= 2 && value[0] == '{' && value[^1] == '}')
                {
                    csvHeaders.Add(CsvSchemaParser.ParseHeader(col, value));
                }
            }
            return csvHeaders;
        }

        private static object ConvertValue(string value, Type targetType, int row, string columnName)
        {
            if (string.IsNullOrEmpty(value))
                return targetType.IsArray ? Array.CreateInstance(targetType.GetElementType(), 0) : (targetType.IsValueType ? Activator.CreateInstance(targetType) : null);

            if (targetType.IsGenericType && targetType.GetGenericTypeDefinition() == typeof(List<>))
                throw new Exception($"List<T> 타입은 사용할 수 없습니다. 배열(T[])을 사용하세요.");

            if (targetType.IsArray)
            {
                Type elementType = targetType.GetElementType();
                string[] parts = value.Split(new char[] { '|' }, StringSplitOptions.RemoveEmptyEntries);
                Array array = Array.CreateInstance(elementType, parts.Length);
                for (int i = 0; i < parts.Length; i++)
                    array.SetValue(ConvertValue(parts[i].Trim(), elementType, row, columnName), i);
                return array;
            }

            if (targetType == typeof(string)) return value;

            Type nullableType = Nullable.GetUnderlyingType(targetType);
            if (nullableType != null) targetType = nullableType;

            if (targetType.IsEnum)
            {
                if (Enum.TryParse(targetType, value, true, out object enumValue)) return enumValue;
                throw new FormatException($"'{value}'은(는) 유효한 열거형 값이 아닙니다.");
            }

            ConstructorInfo stringConstructor = targetType.GetConstructor(new Type[] { typeof(string) });
            if (stringConstructor != null) return stringConstructor.Invoke(new object[] { value });

            return Convert.ChangeType(value, targetType, CultureInfo.InvariantCulture);
        }

        #endregion
    }
}