using System.Runtime.InteropServices;
using UnityEngine;

namespace CoreEngine
{
    // Union 공용체
    // 수학의 합집합과 같은 개념
    // 자료형에 상관없이 하나의 메모리를 사용함
    [StructLayout(LayoutKind.Explicit)]
    public class Unions
    {
        /*
         *  32bit 64bit -> 포인터의 크기가 데이터 쓰기, 읽기의 단위가 됨
         *  한번에 읽는 메모리의 크기!
         *  32bit == 4byte -> 이론상 최대 4GB-1의 램의 주소를 나타낼 수 있음
         *  64bit == 8byte -> 이론상 최대 16EB-1까지 주소 지정 가능
         *  -> 64bit 프로세서는 항상 32bit와 호환되도록 만든다
         * 
         *  int int bool float bool이 있으면 총 20byte 사용
         *  [int][int][int][int]
         *  [int][int][int][int] 
         *  [bool][][][]
         *  [float][float][float][float]
         *  [bool][][][]
         *  -> 컴퓨터는 1개의 bool을 저장하고 읽기 위해 4byte를 사용게됨
         *  
         *  0   4   8     12   13
         *  int int float bool bool이 있으면 총 16byte 사용
         *  [int][int][int][int]
         *  [int][int][int][int] 
         *  [float][float][float][float]
         *  [bool][bool][][]
         *  -> 컴퓨터는 2개의 bool을 저장하고 읽기 위해 4byte를 사용게됨
         *  
         *  
         *  
         *  컴퓨터는 전가산기로 이루어진 전자 계산기다
         *  가산기는 가장 낮은 자리수부터 연산을 시작한다.(올림수를 처리하기 위함)
         *  즉, 컴퓨터는 낮은자리수부터 비트를 읽고 연산을 실행한다. -> endian
         *  그래서 little endian이 기본값이다. (작은 수를 먼저 쓰는 것)
         *  bit endian과 혼용을 방지하기 위해 이를 확인하기 위한 키워드를 사용함
         */


        /*
         * 4byte에 메모리로 모든 자료형을 쓰는경우
         *  int     0
         *  float   0
         *  [2][0][1][0]    int ->  65538
         *  [2][0]/[1][0]   short자료형 2개 -> 2, 1
         *  [2]/[0]/[1]/[0] byte -> 2, 0, 1, 0
         *  [f][f][f][f]
         *  
         */
        [FieldOffset(0)]
        public int int0;

        [FieldOffset(0)]
        public float float0;

        [FieldOffset(0)]
        public short short0; // 0 ~ 1
        [FieldOffset(2)]
        public short short1; // 2 ~ 3

        [FieldOffset(0)]
        public byte byte0;
        [FieldOffset(1)]
        public byte byte1;
        [FieldOffset(2)]
        public byte byte2;
        [FieldOffset(3)]
        public byte byte3;

        // 연산자 오버로딩
        public byte this[int index]
        {
            get
            {
                /*
                 * if문 vs switch문 누가 더 빠르냐 -> 똑같이 특정값(매칭)을 찾을 때
                 * switch문은 case를 쭉 훑어가면서 다 확인!
                 * if문은 조건을 하나씩 else if 따라가면서 확인!
                 * 개수에 따라 다름
                 * 
                 * switch는 개수가 늘어나면 처리가 변한다.
                 * 해당하는 값이 들어왔을 때 처리할 내용의 "바로가기"를 만든다.
                 * 개수가 엄청 많이 늘어나면 값마다 바로가기를 직접 이동하게 되어있다.
                 * 대략 4~5개 이상 되면 인덱싱을 시작함
                 */
                switch (index)
                {
                    case 0: return byte0;
                    case 1: return byte1;
                    case 2: return byte2;
                    case 3: return byte3;

                    default:
                        Debug.LogError("인덱스 범위 오류");
                        return (byte)0;
                }
            }
            set
            {
                switch (index)
                {
                    case 0: byte0 = value; break;
                    case 1: byte1 = value; break;
                    case 2: byte2 = value; break;
                    case 3: byte3 = value; break;

                    default:
                        Debug.LogError("인덱스 범위 오류");
                        return;
                }
            }
        }

        public short[] shortArr
        {
            get => new short[] { short0, short1 };
            set
            {
                {
                    /*
                     * Reflection => C#을 쓰는 이유
                     * 이름으로 메소드를 찾거나 변수를 찾는 경우
                     * SendMessage, BroadcastMessage
                     * 인스펙터창에서 변수를 조정하는 것 등
                     */
                    //Type type = GetType();
                    //for (int i = 0; i < 2; i++)
                    //{
                    //    FieldInfo field = type.GetField($"short{i}");
                    //    field.SetValue(this, value[i]);
                    //}
                }// 리플렉션
                if (value.Length > 0) short0 = value[0];
                if (value.Length > 1) short1 = value[1];
            }
        }


        public byte[] byteArr
        {
            get => new byte[] { byte0, byte1, byte2, byte3 };
            set
            {
                int maxLength = Mathf.Min(4, value.Length);
                for (int i = 0; i < maxLength; i++) this[i] = value[i];
            }
        }



        public static Unions instance = new();
    }
}



