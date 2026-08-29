using System;
using System.Linq;
using System.Reflection;
using UnityEngine;
using CoreEngine.Extentions;

namespace CoreEngine.Helpers
{
    /// <summary>
    /// 구조체 <-> 바이트 배열 변환을 전담하는 직렬화 유틸리티
    /// </summary>
    public static class BinarySerializerHelper
    {
        public static byte[] Struct2ByteArray<T>(this T instance)
        where T : struct
        {
            // 리플렉션으로 구조체 뜯어버리기
            Type resultType = typeof(T);
            if (resultType.IsStruct())
            {
                /* Json에서는 불러오는 정보와 불러오지 않는 정보를 구분하는 방법이 있음
                 * [System.Serializable]이 불러오는 정보는 public이다.
                 * struct에서 불러오지 않는 정보를 구분하려면 nonPulbic은 빼야한다.
                 * result에서 멤버변수 다 꺼내기(public | private or protected | !static
                 */

                /* FieldInfo : 런타임에 클래스나 구조체 내에 정의된 "필드(Field)"에 대한 메타데이터(정보)를 나타내는 객체
                 *             메타데이터를 저장한다는 점에서 DB의 스키마와 유사함 
                 *             FieldInfo는 다음의 정보를 갖고있음
                 *                 필드의 이름: (예: _myPrivateField, publicData)
                 *                 필드의 타입: (예: int, string, MyClass)
                 *                 필드의 접근 한정자: (예: public, private, protected)
                 *                 필드가 정적인지(static) 또는 인스턴스 멤버인지:
                 *                 필드가 읽기 전용인지(readonly) 여부:
                 */
                FieldInfo[] fields = resultType.GetFields(
                                                BindingFlags.NonPublic |
                                                BindingFlags.Public |
                                                BindingFlags.Instance);

                fields = fields.SortFiledInfoArrAscend(resultType);

                // Linq가 제일 빠른 것은 아님
                // Array.Sort(fields);
                // 제일 빠른건 상황에 맞는 알고리즘을 사용한 sort
                // Array.Sort는 메모리 자체를 섞어버리는 것 => 결과를 참조한 메모리에 저장
                // OrderBy => 메모리를 복제해서 따로 돌리기 => 결과를 반환하고 원본 데이터는 보호됨

                // fileds 안의 내용을 모두 더할건데 뭘 더할 것인지를 지정해줌
                int totalLength = fields.Sum(
                    (currentField) =>
                    {
                        int size = currentField.FieldType.GetSize();
                        if (currentField.FieldType == typeof(string))
                        {
                            // instance안에 들어있는 글자 길이를 저장하기 위한 공간 + 실제 글자를 저장할 공간(byte)
                            return size + currentField.GetValue(instance).ToByteArray().Length;
                        }
                        else
                        {
                            return size;
                        }

                    });

                byte[] result = new byte[totalLength];
                int offset = 0; // 지금 몇 번째 칸에 넣으면 좋을까?
                foreach (FieldInfo current in fields)
                {
                    // 바이트 배열로 바꾸려고 했을 때 타입에 따른 크기를 정확히 알아야함
                    // C#에서는 기본적으로 사이즈를 알기 어려움

                    // 현재 멤버변수의 값을 바이트 배열로 가져와서
                    // current.GetValue(Instance) : 변수로 주어진 객체에서 특정 컬럼값을 가져옴
                    byte[] buffer = current.GetValue(instance).ToByteArray();

                    if (current.FieldType == typeof(string))
                    {
                        // string이라면, 버퍼의 사이즈를 2바이트로 계산해서
                        // 이걸 토대로 buffer를 넣기 전에 2바이트로 길이를 쓰기
                        int fieldSizeForString = current.FieldType.GetSize();

                        // 문자열의 바이트 길이를 넣어줌 2바이트 안에 넣어줌
                        // littleEndian이니 short로 바꿀 필요는 없음
                        Array.Copy(buffer.Length.ToByteArray(), 0, result, offset, fieldSizeForString);
                        offset += fieldSizeForString;

                        // 그 다음은 문자열의 바이트 를 넣어줌
                    }
                    // 결과물의 다음 위치에 넣어주고
                    Array.Copy(buffer, 0, result, offset, buffer.Length);
                    // 다음 위치를 정해놓음
                    offset += buffer.Length;
                }

                return result;
            }

            return null;
        }

        public static TargetStruct ByteArray2Struct<TargetStruct>(this byte[] originArr)
        where TargetStruct : struct
        {
            // Throw로 던지면 유니티 console에서 보인다.
            // Throw는 try-catch문 안에서 쓸 수 있다.
            // 유니티에서는 자체적으로 유니티 에디터로 게임을 돌릴때에는 try - catch를 사용
            // 실행을 시키고 있고 Throw가 된다면 현재 실행중이던 함수만 끝내고 console에 exception을 보여줌
            if (originArr is null) throw new Exception("[B2S Error] originArr is null");
            if (originArr.Length == 0) throw new Exception("[B2S Error] originArr Has Not Value");

            Type resultType = typeof(TargetStruct);
            if (!resultType.IsStruct())
            {
                Debug.LogError($"Type [{resultType.Name}] is not struct");
                return default;
            }

            FieldInfo[] fields = resultType.GetFields(
                                        BindingFlags.NonPublic |
                                        BindingFlags.Public |
                                        BindingFlags.Instance);

            fields = fields.SortFiledInfoArrAscend(resultType);
            //fields = fields.OrderBy((currentField) => Marshal.OffsetOf(resultType, currentField.Name).ToInt32())
            //        .ToArray();


            // 1.번 객체를 직접 만든 후 박싱하여 건내준다. -> 박싱을 위한 오버헤드 발생
            // TargetStruct targetStruct = default;
            // object Instance = targetStruct;

            // 2번 Activator로 힙영역에 직접 객체를 만들고(박싱) 그 주소를 전달한다. -> 박싱보다 비교적 오버헤드 적음
            object instance = Activator.CreateInstance<TargetStruct>();

            int offset = 0;
            foreach (FieldInfo current in fields)
            {
                int size = current.FieldType.GetSize();
                byte[] buffer = new byte[size];
                Array.Copy(originArr, offset, buffer, 0, size);
                offset += size;

                if (current.FieldType == typeof(string))
                {
                    // 2칸짜리 바이트 배열에서 길이를 가져옴
                    short stringLength = (short)buffer.FromByteArray(typeof(short));

                    // 알려준 길이만큼 버퍼를 생성
                    buffer = new byte[stringLength];

                    // 그 길이만큼 원본 메모리에서 꺼내오기
                    Array.Copy(originArr, offset, buffer, 0, stringLength);
                    offset += stringLength;

                    // 그 다음은 문자열의 바이트 를 넣어줌
                }

                object value = buffer.FromByteArray(current.FieldType);

                // 값타입을 전달하면 복사전달되어 결과를 받을 수 없음
                // 때문에 전달할 대상을 박싱하여 참조가 가능하도록 만듬
                current.SetValue(instance, value);
            }

            return (TargetStruct)instance;
        }
    }
}