using CoreEngine.Helpers;
using System.Text;
using UnityEngine;

namespace CoreEngine.GameData.Test
{
    [System.Serializable]
    public class TestRecord : BaseRecord
    {
        [TableColumn, SerializeField] private int intTest;
        [TableColumn, SerializeField] private int[] intArrayTest;

        [TableColumn, SerializeField] private float floatTest;
        [TableColumn, SerializeField] private float[] floatArrayTest;

        [TableColumn, SerializeField] private bool boolTest;
        [TableColumn, SerializeField] private bool[] boolArrayTest;

        [TableColumn, SerializeField] private string stringTest;
        [TableColumn, SerializeField] private string[] stringArrayTest;

        protected override void BakeID()
        {
#if UNITY_EDITOR
            // 1. 문자열 조합을 위한 StringBuilder 생성 (에디터 전용이므로 GC 걱정 없음!)
            StringBuilder sb = new StringBuilder();

            // 2. 단일 필드 조합 (구분자 '_' 사용)
            sb.Append(intTest).Append("_");
            sb.Append(floatTest).Append("_");
            sb.Append(boolTest).Append("_");
            sb.Append(stringTest).Append("_");

            // 3. 배열 필드 조합 (Null 방어 및 요소별 구분자 ',' 사용)
            if (intArrayTest != null)
            {
                foreach (var item in intArrayTest) sb.Append(item).Append(",");
            }
            sb.Append("_");

            if (floatArrayTest != null)
            {
                foreach (var item in floatArrayTest) sb.Append(item).Append(",");
            }
            sb.Append("_");

            if (boolArrayTest != null)
            {
                foreach (var item in boolArrayTest) sb.Append(item).Append(",");
            }
            sb.Append("_");

            if (stringArrayTest != null)
            {
                foreach (var item in stringArrayTest) sb.Append(item).Append(",");
            }

            // 4. 최종 조합된 고유 문자열(Composite Key) 확인
            string compositeKey = sb.ToString();

            // 5. 앞서 만든 64비트 해싱 헬퍼를 통해 ulong으로 변환 후 할당
            this.HashCode = HashHelper.GetHash64(compositeKey);

            // (선택) 디버깅용: 에디터에서 어떤 문자열이 어떤 ID로 구워졌는지 확인하고 싶다면
            // Debug.Log($"[TestRecord] 구워진 키: {compositeKey} -> 해시 ID: {this.hashedId}");
#endif
        }
    }

    public class TestTable : BaseTable<TestRecord> { }
}

