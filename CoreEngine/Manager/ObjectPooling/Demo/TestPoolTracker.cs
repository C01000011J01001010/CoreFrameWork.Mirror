using System.Collections.Generic;
using UnityEngine;

namespace CoreEngine.Pool.Test
{
    /// <summary>
    /// 스폰 버튼이 생성한 객체를 디스폰 버튼이 순차적으로 찾아서 꺼낼 수 있도록 돕는 테스트 전용 메모장
    /// </summary>
    public static class TestPoolTracker
    {
        // LIFO(후입선출) 구조를 사용하여 가장 마지막에 생성된 객체부터 반환
        public static Stack<GameObject> SpawnedObjects = new Stack<GameObject>();
    }
}