using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using CoreEngine.GameData;
using UnityEditor;
using UnityEngine;

namespace CoreEditor.GameData
{
    public class CsvTableConverter : EditorWindow
    {
        private TextAsset _csv;
        private BaseTable _table;

        private string _status;
        private MessageType _statusType;

        [MenuItem("CoreEngine/GameData/CSV Table Converter")]
        private static void Open()
        {
            GetWindow<CsvTableConverter>("CSV Table Converter");
        }

        private void OnGUI()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("CSV Table Converter", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            DrawObjectFields();
            EditorGUILayout.Space();

            DrawTableInfo();
            EditorGUILayout.Space();

            DrawConvertButton();
            EditorGUILayout.Space();

            DrawStatusMessage();
        }

        private void DrawObjectFields()
        {
            EditorGUI.BeginChangeCheck();
            TextAsset selectedCsv = (TextAsset)EditorGUILayout.ObjectField("CSV", _csv, typeof(TextAsset), false);

            if (EditorGUI.EndChangeCheck())
            {
                if (selectedCsv != null)
                {
                    // 할당된 에셋의 실제 프로젝트 내 경로를 가져옵니다
                    string assetPath = AssetDatabase.GetAssetPath(selectedCsv);

                    // 경로가 .csv로 끝나지 않는다면 할당을 거부합니다
                    if (!assetPath.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
                    {
                        SetStatus("CSV 확장자를 가진 파일만 할당할 수 있습니다", MessageType.Warning);
                        selectedCsv = null;
                    }
                    else
                    {
                        ClearStatus();
                    }
                }

                _csv = selectedCsv;
            }

            // 에디터에서는 BaseTable을 상속받은 객체만 슬롯에 넣을 수 있도록 강제합니다
            _table = (BaseTable)EditorGUILayout.ObjectField("Table", _table, typeof(BaseTable), false);
        }

        private void DrawConvertButton()
        {
            using (new EditorGUI.DisabledScope(_csv == null || _table == null))
            {
                if (GUILayout.Button("Convert", GUILayout.Height(30)))
                {
                    ExecuteConvert();
                }
            }
        }

        private void DrawStatusMessage()
        {
            if (!string.IsNullOrEmpty(_status))
            {
                EditorGUILayout.HelpBox(_status, _statusType);
            }
        }

        private void DrawTableInfo()
        {
            if (_table == null)
                return;

            try
            {
                Type recordType = GetRecordType(_table.GetType());
                EditorGUILayout.LabelField("Table Type", _table.GetType().FullName);
                EditorGUILayout.LabelField("Record Type", recordType.FullName);
            }
            catch (Exception exception)
            {
                EditorGUILayout.HelpBox(exception.Message, MessageType.Error);
            }
        }

        private void ExecuteConvert()
        {
            ClearStatus();

            if (!TryValidateInputs(out ITableSetter tableSetter, out Type recordType))
                return;

            try
            {
                // 리플렉션을 사용하여 런타임에 알아낸 레코드 타입으로 제네릭 메서드를 동적 호출합니다
                MethodInfo convertMethod = typeof(CsvTableConverter).GetMethod(
                    nameof(ConvertCsvGeneric),
                    BindingFlags.NonPublic | BindingFlags.Instance);

                MethodInfo genericMethod = convertMethod.MakeGenericMethod(recordType);

                // 인스턴스 메서드이므로 첫 번째 인자로 자신을 넘겨줍니다
                genericMethod.Invoke(this, new object[] { tableSetter, _csv.text });
            }
            catch (TargetInvocationException exception)
            {
                // 리플렉션 호출 내부에서 발생한 실제 예외를 추출하여 출력합니다
                SetStatus($"변환 중 예외가 발생했습니다\n{exception.InnerException}", MessageType.Error);
            }
            catch (Exception exception)
            {
                SetStatus($"변환 중 예외가 발생했습니다\n{exception}", MessageType.Error);
            }
        }

        private bool TryValidateInputs(out ITableSetter tableSetter, out Type recordType)
        {
            tableSetter = null;
            recordType = null;

            if (_csv == null)
            {
                SetStatus("CSV 파일이 선택되지 않았습니다", MessageType.Error);
                return false;
            }

            if (_table == null)
            {
                SetStatus("테이블 객체가 선택되지 않았습니다", MessageType.Error);
                return false;
            }

            tableSetter = _table as ITableSetter;
            if (tableSetter == null)
            {
                SetStatus("선택한 객체가 데이터를 삽입할 수 있는 인터페이스를 구현하지 않았습니다", MessageType.Error);
                return false;
            }

            try
            {
                recordType = GetRecordType(_table.GetType());
                return true;
            }
            catch (Exception exception)
            {
                SetStatus(exception.Message, MessageType.Error);
                return false;
            }
        }

        // 제약 조건에 명시된 기본 생성자와 인터페이스를 활용하여 인스턴스를 직접 생성하고 매핑합니다
        private void ConvertCsvGeneric<TRecord>(ITableSetter tableSetter, string csvText)
            where TRecord : BaseRecord, IRecord, new()
        {
            string[] lines = SplitLines(csvText);

            if (lines.Length < 2)
                throw new InvalidOperationException("데이터가 부족합니다 최소 헤더와 하나의 데이터 행이 필요합니다");

            // 첫 번째 줄을 헤더로 간주하고 필드 정보를 순서대로 배열에 준비합니다
            string[] headers = lines[0].Split(',');
            FieldInfo[] columnFields = new FieldInfo[headers.Length];

            for (int i = 0; i < headers.Length; i++)
            {
                string headerName = headers[i].Trim();

                if (string.IsNullOrEmpty(headerName))
                    continue;

                FieldInfo field = FindField(typeof(TRecord), headerName);

                if (field != null && !field.IsStatic && !field.IsInitOnly)
                {
                    columnFields[i] = field;
                }
            }

            List<IRecord> temporaryRecords = new List<IRecord>();

            // 두 번째 줄부터 순회하며 데이터를 추출합니다
            for (int row = 1; row < lines.Length; row++)
            {
                string line = lines[row];

                if (string.IsNullOrWhiteSpace(line))
                    continue;

                string[] columns = line.Split(',');

                // 제약 조건에 명시된 new를 호출하여 타입 변수로 인스턴스를 직접 만듭니다
                TRecord record = new TRecord();

                for (int col = 0; col < columns.Length; col++)
                {
                    if (col >= columnFields.Length || columnFields[col] == null)
                        continue;

                    string rawValue = columns[col].Trim();
                    object value = ConvertValue(rawValue, columnFields[col].FieldType, row);

                    columnFields[col].SetValue(record, value);
                }

                temporaryRecords.Add(record);
            }

            // 원본 데이터를 백업한 뒤 새로운 데이터로 교체를 시도합니다
            List<IRecord> backup = tableSetter.GetListCopy();

            if (!Commit(tableSetter, temporaryRecords, backup, out string commitError))
            {
                SetStatus(commitError, MessageType.Error);
                return;
            }

            SaveAssets();
            SetStatus($"변환 완료\n레코드 타입 {typeof(TRecord).Name}\n변환 개수 {temporaryRecords.Count}", MessageType.Info);
        }

        private void SaveAssets()
        {
            EditorUtility.SetDirty(_table);
            AssetDatabase.SaveAssets();
        }

        private static bool Commit(ITableSetter tableSetter, List<IRecord> records, List<IRecord> backup, out string errorMessage)
        {
            errorMessage = null;

            try
            {
                tableSetter.Clear();
                tableSetter.SetCapacity(records.Count);

                for (int i = 0; i < records.Count; i++)
                {
                    IRecord record = records[i];

                    // 테이블 객체 내부에서 중복 인덱스 삽입을 거부할 경우 원상태로 복구합니다
                    if (tableSetter.Add(record))
                        continue;

                    Restore(tableSetter, backup);
                    errorMessage = $"레코드 반영에 실패했습니다 동일한 고유 인덱스가 존재할 수 있습니다 인덱스 {record.Index}";
                    return false;
                }

                return true;
            }
            catch (Exception exception)
            {
                Restore(tableSetter, backup);
                errorMessage = $"테이블 반영 중 예외가 발생했습니다\n{exception}";
                return false;
            }
        }

        private static bool Restore(ITableSetter tableSetter, List<IRecord> backup)
        {
            try
            {
                tableSetter.Clear();
                tableSetter.SetCapacity(backup.Count);

                for (int i = 0; i < backup.Count; i++)
                {
                    if (tableSetter.Add(backup[i]))
                        continue;

                    throw new InvalidOperationException($"기존 레코드 복구 실패 인덱스 {backup[i].Index}");
                }

                return true;
            }
            catch (Exception exception)
            {
                Debug.LogError($"테이블 복구에 실패했습니다\n{exception}");
                return false;
            }
        }

        private static object ConvertValue(string value, Type targetType, int row)
        {
            if (string.IsNullOrEmpty(value))
                return GetDefaultValue(targetType);

            if (targetType == typeof(string))
                return ConvertSpecialString(value);

            Type nullableType = Nullable.GetUnderlyingType(targetType);
            if (nullableType != null)
            {
                targetType = nullableType;
                if (string.IsNullOrEmpty(value))
                    return null;
            }

            if (targetType.IsEnum)
            {
                if (Enum.TryParse(targetType, value, true, out object enumValue))
                    return enumValue;

                throw new FormatException($"행 {row + 1}의 값을 열거형으로 변환할 수 없습니다");
            }

            try
            {
                return Convert.ChangeType(value, targetType, CultureInfo.InvariantCulture);
            }
            catch (Exception exception)
            {
                throw new FormatException($"행 {row + 1}의 값을 변환할 수 없습니다", exception);
            }
        }

        private static object GetDefaultValue(Type type)
        {
            return type.IsValueType ? Activator.CreateInstance(type) : null;
        }

        private static string ConvertSpecialString(string value)
        {
            // 문자열 내부의 쉼표 처리를 위한 특수 토큰 치환 로직입니다
            return value.Replace("$(0)", ",");
        }

        // 부모 클래스의 보호된 필드까지 찾기 위해 상속 구조를 따라 올라가며 탐색합니다
        private static FieldInfo FindField(Type type, string fieldName)
        {
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

            while (type != null)
            {
                FieldInfo field = type.GetField(fieldName, flags | BindingFlags.DeclaredOnly);
                if (field != null)
                    return field;

                type = type.BaseType;
            }

            return null;
        }

        private static Type GetRecordType(Type tableType)
        {
            Type current = tableType;

            while (current != null)
            {
                if (current.IsGenericType && current.GetGenericTypeDefinition() == typeof(BaseTable<>))
                {
                    return current.GetGenericArguments()[0];
                }
                current = current.BaseType;
            }

            throw new InvalidOperationException("베이스 테이블 형식을 찾을 수 없습니다");
        }

        private static string[] SplitLines(string text)
        {
            return text.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
        }

        private void SetStatus(string message, MessageType type)
        {
            _status = message;
            _statusType = type;
            Repaint();
        }

        private void ClearStatus()
        {
            _status = null;
        }
    }
}