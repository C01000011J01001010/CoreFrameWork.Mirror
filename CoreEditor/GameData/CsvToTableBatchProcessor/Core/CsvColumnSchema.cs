using System;
using System.Collections.Generic;
using System.Reflection;

namespace CoreEditor.GameData.Validation
{
    public class CsvColumnSchema
    {
        // CSV 파일 내에서의 열 인덱스
        public int ColumnIndex;

        // 💡 기본 매핑 정보
        public string FieldName;
        public FieldInfo Field;
        public Type FieldType;
        public Type ElementType; // 배열일 경우 원소의 타입, 아니면 FieldType과 동일

        // 💡 제약조건 플래그 및 데이터
        public bool IsPK = false;
        public bool IsUnique = false;
        public bool IsNotNull = false;

        public bool HasDefault = false;
        public string DefaultValue = null;

        // 다중 조건을 지원하기 위해 컬렉션으로 관리
        public List<string> CheckExpressions = new List<string>();
        public HashSet<string> InValues = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        public HashSet<string> NotInValues = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // 💡 편의성 프로퍼티
        public bool IsArray => FieldType != null && FieldType.IsArray;
    }
}