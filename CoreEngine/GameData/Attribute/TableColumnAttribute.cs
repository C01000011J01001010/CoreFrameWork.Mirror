using System;

namespace CoreEngine.GameData
{
    /// <summary>
    /// 이 속성이 부여된 필드만 CSV의 데이터 컬럼으로 인식되며, 
    /// 툴은 CSV 헤더의 개수와 이름이 이 속성이 부여된 필드들과 1:1로 완벽히 일치하는지 엄격하게 검사합니다.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
    public class TableColumnAttribute : Attribute
    {
    }
}