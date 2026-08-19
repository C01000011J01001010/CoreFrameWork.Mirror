using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using UnityEditor;
using UnityEditor.VersionControl;
using UnityEngine;
public static class Extensions
{

    public static T AsOrThrow<T>(this object obj)
    {
        if (obj == null) throw new ArgumentNullException(obj.GetType().Name);

        if (obj is T value)
            return value;

        throw new InvalidCastException(
            $"Cannot cast '{obj.GetType().FullName}' to '{typeof(T).FullName}'.");
    }


    // 절대 주소를 유니티 상대주소로 변경
    public static string ToUnityPath(this string origin)
    {
        // 절대 주소를 상대 주소로 변경
        origin = origin.Replace(Application.dataPath, "").Replace('\\', '/');
        
        // 이미 상대주소였다면 이 부분을 패스함
        if (!origin.Contains("Assets"))
        {
            origin = "Assets" + origin;
        }
        return origin;
    }

    public static bool IsNullOrEmpty<T>(this ICollection<T> collection)
    {
        return collection == null || collection.Count == 0;
    }

    public static string ToReflectionText<T>(this string originString, T Owner)
    {
        string result = originString;

        // 중괄호 체크
        Stack<int/*index*/> leftBraceStack = new();
        int index = 0;
        while (index < result.Length)
        {
            char currentCharacter = result[index];
            switch (currentCharacter)
            {
                // 중괄호가 열리면 깊이 +1
                case '{':
                    leftBraceStack.Push(index);
                    break;

                // 중괄호가 닫히면 깊이 -1
                case '}':
                    int lastBrace = leftBraceStack.Pop();

                    string varName = result.Substring(lastBrace, index - lastBrace + 1);
                    Debug.Log(varName);
                    result = result.Replace(varName, varName.GetValueFromBracedText(Owner)?.ToString() ?? varName.Trim('{', '}'));

                    // 스트링의 길이가 바뀌었으니 앞으로 돌아가서 다시 탐색
                    index = lastBrace;
                    continue;
            }
            ++index;
        }

        return result;
    }

    public static object GetValueFromBracedText<T>(this string bracedText, T from)
    {
        string fieldName = bracedText.Trim('{', '}', ' ');

        // .을 기준으로 나눈 뒤에 오른쪽 필드명 왼쪽은 소유자
        string[] splitByDot = fieldName.Split('.');

        if (splitByDot.Length > 1)
        {
            object currentObject = from;

            int index = 0;
            foreach (string currentString in splitByDot)
            {
                // 이 안의 대상을 가져와야할 수 있으니
                // 지금 대상에서 필드를 가져오기
                object nextObject = currentObject.GetField(currentString);

                if (nextObject is null) return null;

                index++;
                // 마지막 칸이다
                if (index == splitByDot.Length)
                {
                    return nextObject;
                }
                else
                {
                    currentObject = nextObject;
                }
            }
        }
        else
        {
            return from.GetField(fieldName);
        }
        return null;
    }

    // 객체와 필드이름으로 객체의 필드를 가져오기
    public static object GetField<T>(this T from, string fieldName)
    {
        Type fromType = from.GetType();

        // 두 가지로 나누기
        // 배열인지 체크하기
        // 배열의 기준점인 [를 처리
        string[] splitBySquareBrackets = fieldName.Split('[');

        // 대괄호가 없으면 그냥 필드네임, 대괄호가 있으면 대괄호 좌측의 필드네임
        FieldInfo field = fromType.GetField(splitBySquareBrackets[0]);

        // 필드 아니면 프로퍼티
        PropertyInfo property = fromType.GetProperty(splitBySquareBrackets[0]);
        object result = field?.GetValue(from) ?? property?.GetValue(from);


        if (splitBySquareBrackets.Length > 1)
        {
            string parseTarget = splitBySquareBrackets[1].Trim(']', ' ');


            // 오른쪽에 있는 친구가 배열의 내용을 포함하고 있음
            if (int.TryParse(parseTarget, out int asInt))
            {
                if (result is Array asArray) return asArray.GetValue(asInt);
                else if (result is List<object> asList) return asList[asInt];
                else if (result is Dictionary<int, object> asDict) return asDict[asInt];
                else if (result is Stack<object> asStack) return asStack.ToArray()[asInt];
                else if (result is Queue<object> asQueue) return asQueue.ToArray()[asInt];
                else
                {
                    Debug.LogError($"{result}[{asInt}] is not valid type");
                    return (IEnumerable)result;
                }

            }
        }
        return result;
    }

    /// <summary>
    /// 싱글톤 만들때 수백번 필요한지 생각할것
    /// </summary>
    /// <typeparam name="T">클래스명</typeparam>
    /// <param name="target">클래스 객체</param>
    /// <param name="slot">Instance</param>
    /// <returns></returns>
    public static bool TryMakeSingleton<T>(this T target, ref T slot)
    {
        if (target is null)
        {
            return false;
        }
        else if (target.Equals(slot))//(target == slot)
        {
            return true; // 동일한 객체가 이미 싱글톤인 경우
        }
        else if (slot is null)
        {
            slot = target;
            return true;
        }
        else // 이미 싱글톤으로 된 다른 객체가 존재하는 경우
        {
            Debug.LogWarning($"Object {target} is already registered as a Singleton.");
            return false;
        }
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
    public static FieldInfo[] SortFiledInfoArrAscend(this FieldInfo[] fields, Type structType)
    {
        // fileds 안에 있는 내용을 쭉 훑어보면서
        // type 에 따라 사이즈를 알 수 있게 되었다
        // 구조체를 뜯었을 때 나오는 전체 사이즈도?
        // LinQ => Query 언어 => DataBase, SQL
        // C#내부에서 자료구조를 네이티브 언어로 뜯어보고 조정하는 역할
        // C#에서 아무리 열심히 만들어도 LinQ보다 느림

        // Order By : ~을 기준으로 한 정렬 (오름차순)
        // 만약 내림차순으로 하고싶다면 [OrderByDescending] 를 사용
        // Marshal.OffsetOf
        //      -> 구조체의 시작주소로부터 멤버가 선언된 상대적주소를 반환
        //      -> 반환타입 IntPtr(포인터 타입 주소)
        //      -> 여기서 ToInt64()는 주소를 숫자로 보여주는 역할

        // 이 코드는 구조체의 상대적 순서를 기준으로 필드를 오름차순 정렬함
        return fields.OrderBy(
            (currentField) =>
            {
                return Marshal.OffsetOf(structType, currentField.Name).ToInt64();
            }
            ).ToArray(); // 00타입의 xx변수 이름을 기준으로
    }
    /// <summary>
    /// 값타입이면서 원시타입이 아니고(사용자정의 타입) 열거형이 아닌 것
    /// </summary>
    /// <param name="checkType"></param>
    /// <returns></returns>
    public static bool IsStruct(this Type checkType) =>
        checkType.IsValueType &&
        !checkType.IsPrimitive &&
        !checkType.IsEnum;

    public static bool IsStruct<T>() => typeof(T).IsStruct();

    public static int GetSize(this Type checkType)
    {
        if (checkType == typeof(int) ||
            checkType == typeof(float) ||
            checkType.IsEnum)
            return 4;

        else if (checkType == typeof(long) ||
                 checkType == typeof(double))
            return 8;

        else if (checkType == typeof(short))
            return 2;

        else if (checkType == typeof(bool) ||
                 checkType == typeof(byte))
            return 1;

        else if (checkType == typeof(string))
        {
            // string은 정해진 크기가 있는게 아니기 때문에
            // 전체 길이가 어느정도인지를 나타내는 공간이 추가로 필요
            return 2;
        }


        // 시간효율을 높이기 위해 간단한 것은 미리 걸러냄
        else
        {
            try
            {
                // 메모리 참조에서 miss가 나올 수도 있음
                return Marshal.SizeOf(checkType);
            }
            catch (Exception e)
            {
                Debug.LogError(e);
                return 0;
            }
        }
    }

    public static byte[] ToByteArray(this object target)
    {
        // object를 포함한 참조타입은 힙에 데이터를 저장할때 객체의 실제 런타임 Type도 함께 저장됨
        // GetType() 메서드는 변수의 선언 타입(여기서는 object)이 무엇이든 상관없이,
        // 해당 변수가 현재 참조하고 있는 객체의 실제 런타임 타입을 정확하게 반환
        Type targetType = target.GetType();// = typeof(T_Enum);


        if (target is int ||
            targetType.IsEnum)
        {
            Unions.instance.int0 = (int)target;
            return Unions.instance.byteArr;
        }

        else if (target is float)
        {
            Unions.instance.float0 = (float)target;
            return Unions.instance.byteArr;
        }

        else if (target is short)
        {
            Unions.instance.short0 = (short)target;
            return new byte[] { Unions.instance.byte0, Unions.instance.byte1 };
        }

        else if (target is byte)
        {
            return new byte[] { (byte)target };
        }

        else if (target is bool)
        {
            return new byte[] { (bool)target ? (byte)1 : (byte)0 };
        }

        else if (target is string)
        {
            return Encoding.UTF8.GetBytes((string)target);
        }

        else if (target is double)
        {
            return BitConverter.GetBytes((double)target);
        }

        else if (target is long)
        {
            return BitConverter.GetBytes((long)target);
        }

        else
        {
            Debug.LogError($"{targetType}은 정의되지 않은 자료형입니다.");
            return null;
        }
    }

    public static object FromByteArray(this byte[] originArray, Type targetType)
    {
        if (targetType == typeof(int) ||
            targetType.IsEnum)
        {
            Unions.instance.byteArr = originArray;
            return Unions.instance.int0;
        }

        else if (targetType == typeof(float))
        {
            Unions.instance.byteArr = originArray;
            return Unions.instance.float0;
        }

        else if (targetType == typeof(short))
        {
            Unions.instance.byteArr = originArray;
            return Unions.instance.short0;
        }

        else if (targetType == typeof(byte))
        {
            Unions.instance.byteArr = originArray;
            return Unions.instance.byte0;
        }

        else if (targetType == typeof(bool))
        {
            Unions.instance.byteArr = originArray;
            return Unions.instance.byte0 == 1;
        }

        else if (targetType == typeof(string))
        {
            return Encoding.UTF8.GetString(originArray);
        }

        else if (targetType == typeof(double))
        {
            return BitConverter.ToDouble(originArray);
        }

        else if (targetType == typeof(long))
        {
            return BitConverter.ToInt64(originArray);
        }

        else
        {
            return null;
        }
    }

    public static GameObject FindObjectInChildrenWithTag(this Transform target, string tag)
    {
        foreach (Transform child in target.transform)
        {
            if (child.CompareTag(tag))
                return child.gameObject;

            if (child.childCount > 0)
            {
                GameObject result = child.FindObjectInChildrenWithTag(tag);
                if (result != null)
                    return result;
            }
        }
        return null;
    }

    public static T GetOrAddComponent<T>(this GameObject target) where T : Component
    {
        // GetComponent 시도하고 null이면 AddComponent 수행
        return target.GetComponent<T>() ?? target.AddComponent<T>();
    }

    public static T[] GetComponent<T>(this GameObject[] from)
        where T : Component
    {
        /* 길이를 최대치로 만들어놓는 이유
         * 
         * 컴포넌트가 안들어있을 수도 있는데, 왜 항상 최대치로 만드는걸까
         * 1. 기존 배열과 인덱스 매칭이 가능함
         * 2. 몇번째 오브젝트에 없었는가를 표시하는 수단이기도 함
         * 3. 빈 공간도 정보이기 때문에!
         */
        T[] result = new T[from.Length];

        for (int i = 0; i < result.Length; i++)
        {
            result[i] = from[i]?.GetComponent<T>();
        }

        /* ++ -- 연산자 언어에 따른 정리
         * C#이나 Java : 메모리에 접근하고 접근을 해제할때마다 처리
         * C++ : 해당 줄이 끝나고 나서 증감 확인
         */

        /* ++a, a++ 누가 더 빠를까?
         * a++ <- 복사연산
         * ++a 는 메모리에 가서 즉시 1더하기, 결과를 반환
         * a++ 는 메모리에 가서 temp변수에 저장하고 그 다음 1더하기
         * 
         * 그러면 for문에서 i++하는 것은..?
         * for(int i = 0; i < max ; ++i) 이렇게 해야하는 것 아닌가?
         * => 그냥 for문 처음 쓸 때 i++라고 시작했기 때문이다
         * 성능의 문제가  아니라 "관습"적인 문제
         * 그럼 ++i라고 쓰면 성능적 이득이 있는가? NO!
         * 컴파일러가 i의 반환값이 사용되지 않는 경우 i++나 ++i를 같은 코드로 변환해서 사용함
         * 
         * 단, 컴파일러가 최적화 못하는 경우가 있을 수도 있음
         * => 사용자 정의 연산자로 사용하는 경우, 특히 구조체에서 -- ++를 연산자 오버로딩한 경우 문제가됨
         * 이럴 경우는 ++i나 --i를 쓰는 것이 옳은 방식
         */

        /* xOr 비트연산으로 사용하는 Swap
         * a ^= b;
         * b ^= a;
         * a ^= b;
         * 
         * 컴파일러가 이걸 반복문에서 최적화한다고 중간을 빼먹기도 함 => 에러의 가능성이 생김
         * 컴파일러는 기본적으로 최적화를 열심히 함
         */

        return result;
    }

    public static void AddComponentsFromGameObjects<T>(this Dictionary<string, T[]> target,
        params GameObject[] objects)
        where T : Component
    {
        foreach (var currentObject in objects)
        {
            T currentComponent;

            // 일단 오브젝트가 있는지? 컴포넌트가 있는지?
            // currentObject가 null일 경우 ?? 우측의 값을 반환
            if (currentObject?.TryGetComponent(out currentComponent) ?? false)
            {
                string currentName = currentObject.name; // ex)SP_0

                /* 딕셔너리에 내용을 넣기 위해 제일 먼저 체크할 것!
                 * : 해당 키를
                 */

                if (target.ContainsKey(currentName))
                {
                    //T_Enum[] newArray = new T_Enum[ComponentArr.Length + 1];
                    //Array.Copy(ComponentArr, newArray, ComponentArr.Length);
                    //newArray[^1] = currentComponent;

                    /* IEnumerable : 반복자를 생성할 수 있는 자료구조를 뜻함
                     *               자료구조 안쪽을 돌아다니는 스캐너, 자료구조의 현재 데이터 위치를 나타내는 포인터
                     */
                    //List<T_Enum> tempList = ComponentArr.ToList();
                    //List<T_Enum> tempList = new(ComponentArr);

                    /* 만약에 들어온 배열에 여러개의 똑같은 이름을 가진 오브젝트가 있을 때
                     * 배열의 길이를 수시로 늘리는 현상황보다 한 번에 배열의 길이를 늘리는게
                     * 이득일까?
                     * 한번에 늘린다고 가정
                     * 1. 같은 이름이 몇개 있는지 확인 (이중 반복문)
                     *      => 배열 안에 문자열을 비교하는 것 또한 반복문이기 때문
                     * 2. 같은 이름을 가진 오브젝트들 중에서 컴포넌트 가져오기 (이중 반복문)
                     *      => 오브젝트 배열 안에서 컴포넌트 배열
                     * 3. 처리한 애들을 원본에서 제거해줌 (이중 반복문)
                     *      => 오브젝트 배열의 내용이 상대 배열에 있는지 체크
                     * 4. 가져온 컴포넌트들의 리스트를 정리해서 배열 (반복문)
                     * 5. 원본 배열이 있는지 체크한 뒤, 있으면 append
                     * 6. 그 와중에, 원본 배열에 있는짖 체크해야함 (이중 반복문)
                    */


                    T[] currentArr = target[currentName];

                    // 이미 들어있는 대상이면 넘어가기
                    if (currentArr.Contains(currentComponent)) continue;


                    // 1.
                    //List<T_Enum> asList = new(target[currentName]);
                    //asList.Add(currentComponent);

                    //target[currentName] = asList.ToArray();

                    // 2.
                    T[] temp = new T[target[currentName].Length + 1];
                    Array.Copy(target[currentName], temp, target[currentName].Length);
                    temp[^1] = currentComponent; // ^a == Length - a => 끝에서 a번째 인덱스, Length - a를 직접 사용하는 것이 더 빠름
                    target[currentName] = temp;

                    // 배열 뒤에 새로운 내용을 덧붙이기
                    //currentArr.Append(currentComponent);
                }
                else
                {
                    /* 처음으로 이 땅을 밟는다! 일단 중요한 것
                     * 딕셔너리 안쪽에 있는 List나 배열처럼
                     * 자료구조 안에 있는 자료구조를 new 하는 것을 잊는 사람들이 많다.
                     */
                    target.Add(currentName, new T[] { currentComponent });
                }
            }

        }

    }

    public static Canvas FindParentCanvas(this Transform target)
    {
        Transform current = target;

        while (current != null)
        {
            Canvas canvas = current.GetComponent<Canvas>();
            if (canvas != null)
                return canvas;

            current = current.parent;
        }

        Debug.LogWarning("캔버스를 찾지 못함");
        return null; // 못 찾았을 경우
    }

    public static T Get<T>(this T[] target, int index)
    {
        T result;
        target.TryGet(index, out result);
        return result;
    }

    // 인덱스가 범위 내에 있을 경우에만 원소를 반환
    public static bool TryGet<T>(this T[] target, int index, out T result)
    {
        if (index >= target.Length ||index < 0)
        {
            result = default;
            return false;
        }

        result = target[index];
        return true;
    }

    public static bool TryAdd<T>(this List<T> target, T obj)
    {
        if(target is null)
        {
            Debug.LogWarning($"List is null");
            return false;
        }
        if (obj != null)
        {
            target.Add(obj);
            return true;
        }
        else
        {
            Debug.LogWarning($"don't try to add null");
            return false;
        }
    }

    public static T? int2Enum<T>(this int value)
        where T : struct, Enum
    {
        return Enum.IsDefined(typeof(T), value) ? (T)(object)value : null;
    }


    public static float ClampAngle(this float target, float min, float max)
    {
        if (target < -360f) target += 360f;
        if (target > 360f) target -= 360f;
        return Mathf.Clamp(target, min, max);
    }

    // Quaternion끼리 더하지만 pitch에는 각도 제한을 줌
    public static Quaternion Add(this Quaternion target, Quaternion value, float minPitch = -89.99f, float maxPitch = 89.99f)
    {
        Vector3 euler = target.eulerAngles + value.eulerAngles;
        euler.x = euler.x.ClampAngle(minPitch, maxPitch); // 90도를 찍으면 안됨
        return Quaternion.Euler(euler);
    }

    public static void Deg2Rad_sinCos(this float target, out float sin, out float cos)
    {
        float horRadian = target * Mathf.Deg2Rad;
        sin = Mathf.Sin(horRadian);
        cos = Mathf.Cos(horRadian);
    }

    public static float GetHorisontalDegreeAngle(this Vector3 target)
    {
        float aTan = Mathf.Atan2(target.x, target.z);
        return aTan * Mathf.Rad2Deg;
    }

    public static float GetVerticalDegreeAngle(this Vector3 target)
    {
        // 수직축은 y, 수평축은 xz원의 크기
        float aTan = Mathf.Atan2(target.y, Mathf.Sqrt(target.x * target.x + target.z * target.z));
        return aTan * Mathf.Rad2Deg;
    }

    public static float HorisontalSqrMagnitude(this Vector3 target)
        => target.x * target.x + target.z + target.z;

    public static float HorisontalMagnitude(this Vector3 target)
        => Mathf.Sqrt(target.HorisontalSqrMagnitude());

    public static Vector3 HorisontalNormalize(this Vector3 target)
    {
        target.y = 0; // 위 아래로는 움직이지 않도록
        target.Normalize(); // 제거되는 성분에 상관없이 전체 크기(속도)가 일정하도록 정규화
        return target;
    }

    public static Vector3 RotationClamped(this Vector3 target, float hor, float ver, float min, float max)
    {
        float origin = target.GetVerticalDegreeAngle();
        float wanted = origin + ver;
        float clamped = Mathf.Clamp(wanted, min, max);

        return Rotation(target, hor, ver + (clamped - wanted)); // clamped - wanted : 막힌 정도
    }

    public static Vector3 Rotation(this Vector3 target, float hor, float ver)
    {
        hor.Deg2Rad_sinCos(out float horSinTheta, out float horCosTheta);
        ver.Deg2Rad_sinCos(out float verSinTheta, out float verCosTheta);

        Vector3 result = new Vector3(
            target.z * horSinTheta + target.x * horCosTheta,
            0,
            target.z * horCosTheta - target.x * horSinTheta
            );

        // 수평방향 길이
        float hr = result.magnitude;
        float hr_after = hr * verCosTheta - target.y * verSinTheta; // hr*cosθ - y'*sinθ
        float radiusRatio = hr != 0 ? hr_after / hr : 1.0f; // 분모가 0인 경우 배제

        result *= radiusRatio;
        result.y = hr * verSinTheta + target.y * verCosTheta;

        return result;
    }


    /* 2차원 벡터의 회전 행렬
     * (x, y) -> [cosθ, -sinθ][x ] -> (x*cosθ - y*sinθ, x*sinθ + y*cosθ)
     *           [sinθ, cosθ] [y ]
     */

    /*수평 회전(z, x)
     * (z, x) -> [cosθ, -sinθ][z ] -> (z*cosθ - x*sinθ, z*sinθ + x*cosθ)
     *           [sinθ, cosθ] [x ]
     */
    // 
    public static Vector3 RotationHorizontal(this Vector3 target, float angle)
    {
        // angle 360도 => 라디안 2ㅠ
        float radian = angle * Mathf.Deg2Rad;
        float cosTheta = Mathf.Cos(radian);
        float sinTheta = Mathf.Sin(radian);
        return new Vector3(
            target.x * cosTheta + target.z * sinTheta,
            target.y,
            target.z * cosTheta - target.x * sinTheta
            );
    }

    public static Vector3 RotationVertical(this Vector3 target, float angle)
    {
        // 아까 계산은
        // z'의 길이를 x값으로 x'의 길이를 y값으로

        // 지금 계산
        // 수평의 길이(==hr)를 x값으로 y'의 길이를 y값으로


        float radian = angle * Mathf.Deg2Rad;
        float cosTheta = Mathf.Cos(radian);
        float sinTheta = Mathf.Sin(radian);

        // hr == vector(x, z)의 길이 == 수평방향의 길이
        float hr = MathF.Sqrt(target.x * target.x + target.z * target.z);

        // hr을 x값으로 y'의 길이를 y값으로
        // (hr*cosθ - y'*sinθ, hr*sinθ + y'*cosθ)

        //                  변환전길이   변환후길이
        // Vector(x, z)의 길이를 hr에서 hr_after로 바꿀 수 있는 방법!
        float hr_after = hr * cosTheta - target.y * sinTheta; // hr*cosθ - y'*sinθ

        // 길이가 n인 a를 m으로 바꾸려면 n으로 나누어 정규화 후 m을 곱하면 됨
        float radiusRatio = hr != 0 ? hr_after / hr : 1.0f;
        return new Vector3(
            target.x * radiusRatio,
            hr * sinTheta + target.y * cosTheta, // hr*sinθ + y'*cosθ
            target.z * radiusRatio
            );
    }

    public static bool RayCastWithDebug(this Ray ray, out RaycastHit hit, float distance, LayerMask mask, float duration)
    {
        // 실제 게임에 들어온다면, 무언가 측정하는 것이 등장
        // 선, 구, 캡슐, 큐브로 지역을 탐색하는 것이 필요
        /* Ray cast (Ray : 선) (cast : 던지다) == 선을 던짐 -> 낚시하는 것
         * Sphere cast = 구 캐스팅      -> Physics에서 선택 가능
         * Capsule cast = 캡슐 캐스팅    -> Physics에서 선택 가능
         * Box cast = 사각형 캐스팅       -> Physics에서 선택 가능
         */

        /* cast 종류
         * Cast : 선에 닿는 제일 첫번째 한놈만 잡는 것
         * Cast All : 이 선 길이에 닿는 모든 애들을 잡는 것
         * Cast None Allocate : 메모리 할당 하지 않음
         *                      RayCast를 Update에서 실행하지 마세요! -> 무거우니까
         *                      RayCastAll은 실행하면 내부에서 RaycastHit[] 를 동적할당 받음! <-> Physics.Raycast은 해당 안함
         *                                                      Grabage Collection
         *                      NonAlloc은 배열을 만들지 않고 밖에서 준 배열을 씀
         *                      그럼 GC가 안도는 대신에 단점은? 배열 크기가 정해져 있음
         *                                                   -> 크기가 넘게 되는 순간 못 받음
         *                      최대 개수를 정하는 대신 최적화 or Update같은 데에서 사용하는 경우
         *                      배열을 미리 멤버변수같은 데에 놓고 사용하기
         */
        //Physics.Raycast
        //Physics.RaycastAll
        //RaycastHit[] hits = new RaycastHit[10];
        //int _amount = Physics.RaycastNonAlloc(new Ray(), hits); // 할당되지 않은 RaycastHit[]를 전달할 수 없음
        //Physics.Raycast(new Ray(), out hit);
        //hits = Physics.RaycastAll(new Ray());

        /* Layer : 32개로 되어있음 <-> 32비트 : int
         * Layer :                                     : Inteager
         * 0     : 00000000 00000000 00000000 00000001 : 1      (1 << 0)
         * 1     : 00000000 00000000 00000000 00000010 : 2      (1 << 1)
         * 2     : 00000000 00000000 00000000 00000100 : 4      (1 << 2)
         * 3     : 00000000 00000000 00000000 00001000 : 8      (1 << 3)
         * 4     : 00000000 00000000 00000000 00010000 : 16     (1 << 4)
         * 5     : 00000000 00000000 00000000 00100000 : 32     (1 << 5)
         * 6     : 00000000 00000000 00000000 01000000 : 64     (1 << 6)
         * 7     : 00000000 00000000 00000000 10000000 : 128    (1 << 7)
         * 
         * 0 + 4 : 00000000 00000000 00000000 00010001 : 17     (1 << 0 | 1 << 4) -> 부울대수의 "or 연산" 을 빠르게 하기 위함
        */
        /* QueryTriggerInteraction :기본값 == UseGlobal -> 트리거는 원래 체크 x
         *                          Collide -> 트리거도 체크
         *                          Ignore  -> 트리거 체크 안함
         */

        bool result = Physics.Raycast(ray, out hit, distance, mask, QueryTriggerInteraction.UseGlobal);
        if (result)
        {
            float hitDistance = hit.distance;
            // 그림 그리기 -> 원점, 방향*길이
            Debug.DrawRay(ray.origin, ray.direction * hitDistance, Color.red, duration); // hit.distance: 맞은 친구와의 거리

            // 어디까지 갈 수 있었는지 남은 거리 그리기
            Debug.DrawRay(hit.point, ray.direction * (distance - hitDistance), Color.green, duration);
            return true;
        }
        else
        {
            Debug.DrawRay(ray.origin, ray.direction * distance, Color.red, duration);
            return false;
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="ray"></param>
    /// <param name="distance">거리</param>
    /// <param name="mask">누굴 때리는지</param>
    /// <param name="gravity"></param>
    /// <param name="time">도착하는데 걸리는 시간</param>
    /// <param name="resolution">몇개의 선으로 나눌 것인지</param>
    /// <param name="duration">디버그를 얼마나 보여줄 것인지</param>
    /// <returns></returns>
    public static bool ParabolaCastWithDebug(this Ray ray, out RaycastHit hit, float distance, int mask, Vector3 gravity,
        float time = 0.5f, int resolution = 256, float duration = 1.0f)
    {
        hit = default;

        // 원본 방향을 기억해놓음
        Vector3 originDirection = ray.direction;
        // 해상도 크기로 전체를 쪼개야함
        float eachDistance = distance / resolution;
        float eachTime = time / resolution;
        bool result = false;
        float accumulateTime = 0;
        for (int i = 0; i < resolution; i++) // Ray == 선 -> 곡선을 그리는 방법 : 짧은 선을 여러개 준비 : 해상도
        {
            // 현재 받고있는 중력만큼 떨어지기
            // 시간에 따른 방향의 변화
            // 중력에 의한 현재 속도
            // gravity = 중력가속도 : 속도가 가면 갈수록 빨라짐!
            // 9.80665 m/s^2
            // 속도 = 중력가속도 * 소요된 시간

            // 중력가속도 g가 10이라 가정하면
            // 1초 지났다 -> 속도 : 10    -> 평균속도 : 5    -> 움직인 거리 : 5 * 1 = 5
            // 2초 지났다 -> 속도 : 20    -> 평균속도 : 10   -> 움직인 거리 : 5 * 2 = 20
            // 3초 지났다 -> 속도 : 30    -> 평균속도 : 15   -> 움직인 거리 : 5 * 3 = 45
            //                     gt                 gt/2                  (1/2) gt^2

            accumulateTime += eachTime;
            ray.direction = originDirection + (accumulateTime * accumulateTime / 2 * gravity);

            if (result)
            {
                Debug.DrawRay(ray.origin, ray.direction * eachDistance, Color.green, duration);
            }
            else if (ray.RayCastWithDebug(out hit, eachDistance, mask, duration))
            {
                result = true;
            }
            ray.origin += eachDistance * ray.direction;

        }

        return result;
    }

    public static bool ParabolaCast(this Ray ray, out RaycastHit hit, float distance, int mask, Vector3 gravity,
         float time = 0.5f, int resolution = 256)
    {
        // 원본 방향을 기억해놓음
        Vector3 originDirection = ray.direction;
        // 해상도 크기로 전체를 쪼개야함
        float eachDistance = distance / resolution;
        float eachTime = time / resolution;
        float accumulateTime = 0;
        for (int i = 0; i < resolution; i++) // Ray == 선 -> 곡선을 그리는 방법 : 짧은 선을 여러개 준비 : 해상도
        {
            accumulateTime += eachTime;
            ray.direction = (originDirection + (accumulateTime * accumulateTime / 2 * gravity)).normalized;

            if (Physics.Raycast(ray, out hit, eachDistance, mask))
            {
                return true;
            }
            ray.origin += eachDistance * ray.direction;
        }

        hit = default;
        return false;
    }

    public static bool CurveCastWithDebug(this Ray ray, out RaycastHit hit, float distance, int mask,
        AnimationCurve verticalCurve, int resolution = 256, float duration = 1.0f)
    {
        // 해상도가 충분치 않으면 그냥 함
        if (resolution < 2) { return Physics.Raycast(ray, out hit, distance, mask); }

        float eachDistance = distance / resolution;

        //Vector3 originDirection = ray.direction;

        //Direction을 기준으로 "위쪽"방향을 얻는 방법
        Vector3 upDirection = ray.direction.RotationVertical(90.0f);

        // 점들의 좌표를 미리 찍어놓기!
        // 선을 n개 그리기 위해 필요한 좌표의 개수 -> n+1
        Vector3[] points = new Vector3[resolution + 1];
        points[0] = ray.origin;
        for (int i = 1; i < points.Length; i++)
        {
            float currentTime = (i + 1) / (float)resolution;
            points[i] = points[i - 1]
                      + ray.direction * eachDistance
                      + upDirection * verticalCurve.Evaluate(currentTime);
        }

        hit = default;
        bool result = false;
        for (int i = 0; i < resolution; i++)
        {
            ray.origin = points[i];
            Vector3 direction = points[i + 1] - points[i];
            ray.direction = direction; // 자동으로 정규화됨
            if (result)
            {
                Debug.DrawRay(ray.origin, direction, Color.green, duration);
            }
            else if (ray.RayCastWithDebug(out hit, direction.magnitude, mask, duration))
            {
                result = true;
            }
        }
        return result;
    }

    public static bool CurveCast(this Ray ray, out RaycastHit hit, float distance, int mask,
        AnimationCurve verticalCurve, int resolution = 256)
    {
        // 해상도가 충분치 않으면 그냥 함
        if (resolution < 2) { return Physics.Raycast(ray, out hit, distance, mask); }

        float eachDistance = distance / resolution;

        //Vector3 originDirection = ray.direction;

        //Direction을 기준으로 "위쪽"방향을 얻는 방법
        Vector3 upDirection = ray.direction.RotationVertical(90.0f);

        // 점들의 좌표를 미리 찍어놓기!
        // 선을 n개 그리기 위해 필요한 좌표의 개수 -> n+1
        Vector3[] points = new Vector3[resolution + 1];
        points[0] = ray.origin;
        for (int i = 1; i < points.Length; i++)
        {
            float currentTime = (i + 1) / (float)resolution;
            points[i] = points[i - 1] // 이전 점에서
                      + ray.direction * eachDistance // (방향 * 크기) -> 로컬 x축 값(== 직선방향값) 
                      + upDirection * verticalCurve.Evaluate(currentTime); // (방향 * 크기) -> 로컬 y축 값(== 커브되는값) 
        }

        for (int i = 0; i < resolution; i++)
        {
            ray.origin = points[i];
            Vector3 direction = points[i + 1] - points[i];
            ray.direction = direction; // 자동으로 정규화됨
            if (Physics.Raycast(ray, out hit, direction.magnitude, mask))
            {
                return true;
            }
        }

        hit = default;
        return false;
    }
}

