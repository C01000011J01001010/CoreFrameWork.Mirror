using UnityEngine;

namespace CoreEngine.GameData
{
    // 툴 없이 수정이 불가능하도록 막는 Attribute
    // 인스펙터에서 데이터 조작을 막음
    public class ReadOnlyAttribute : PropertyAttribute { }
}
