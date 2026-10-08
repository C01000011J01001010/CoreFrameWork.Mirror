using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using CoreEngine.GameData;
using CoreEditor.Helpers;
using CoreEngine.Helpers;
using CoreEditor.GameData.Validation;

namespace CoreEditor.GameData
{
    public static class CsvRecordParser
    {
        internal static List<IRecord> Parse<TRecord>(string csvText, TableMetaData metaData, out List<string> extractedKeys)
            where TRecord : BaseRecord, IRecord, IEditorRecordSetup, new()
        {
            string[] lines = csvText.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');

            var headers = ExtractSchemaHeaders(lines, out int schemaRowIndex);
            ValidateAndMapFields<TRecord>(headers);

            List<IRecord> temporaryRecords = new List<IRecord>();
            Dictionary<ulong, string> usedIdsMap = new Dictionary<ulong, string>();
            extractedKeys = new List<string>();

            for (int row = schemaRowIndex + 1; row < lines.Length; row++)
            {
                string line = lines[row];
                if (string.IsNullOrWhiteSpace(line)) continue;

                string[] columns = CsvParserHelper.ParseLine(line);
                TRecord record = new TRecord();
                Dictionary<string, string> rowRawValues = new Dictionary<string, string>();

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

                    if (typedValue != null && schema.CheckExpressions.Count > 0)
                    {
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
                    rowRawValues[schema.FieldName] = rawValue;
                }

                List<string> pkParts = new List<string>();
                foreach (string pkName in metaData.primaryKey)
                {
                    if (rowRawValues.TryGetValue(pkName, out string val)) pkParts.Add(val);
                    else throw new Exception($"[행: {row + 1}] PK 컬럼('{pkName}')을 찾을 수 없습니다.");
                }

                // ASCII 31 (Unit Separator)를 내부 구분자로 사용
                string rawKey = string.Join("\x1F", pkParts);

                ulong recordId = HashHelper.StringToId(rawKey);

                if (usedIdsMap.TryGetValue(recordId, out string existingKey))
                {
                    if (existingKey != rawKey)
                        throw new Exception($"[CoreEngine FATAL] 해시 충돌! Key A: '{existingKey}', Key B: '{rawKey}' (ID: {recordId})");
                    else
                        throw new Exception($"[행: {row + 1}] 중복된 기본키 데이터('{rawKey}')가 존재합니다.");
                }

                usedIdsMap.Add(recordId, rawKey);
                extractedKeys.Add(rawKey);

                ((IEditorRecordSetup)record).EditorSetupId(recordId, rawKey);
                temporaryRecords.Add(record);
            }
            return temporaryRecords;
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

        private static void ValidateAndMapFields<TRecord>(List<CsvColumnSchema> csvHeaders)
        {
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;
            List<FieldInfo> csFields = new List<FieldInfo>();
            Type currentType = typeof(TRecord);

            while (currentType != null && currentType != typeof(object))
            {
                csFields.AddRange(currentType.GetFields(flags).Where(f => f.GetCustomAttribute<TableColumnAttribute>() != null));
                currentType = currentType.BaseType;
            }

            foreach (var header in csvHeaders)
            {
                FieldInfo matchedField = csFields.FirstOrDefault(f => string.Equals(f.Name, header.FieldName, StringComparison.OrdinalIgnoreCase));
                if (matchedField == null) throw new Exception($"C# 클래스에 '{header.FieldName}' 필드가 없습니다.");
                header.Field = matchedField;
                header.FieldType = matchedField.FieldType;
            }
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
    }
}