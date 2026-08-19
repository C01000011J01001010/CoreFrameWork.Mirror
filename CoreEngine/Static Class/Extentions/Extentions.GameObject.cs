using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace CoreEngine
{
    public static partial class Extentions
    {
        public static T GetOrAddComponent<T>(this GameObject target) where T : Component
        {
            // GetComponent 시도하고 null이면 AddComponent 수행
            T targetComponent = target.GetComponent<T>();
            if(targetComponent == null) targetComponent = target.AddComponent<T>();
            return targetComponent;
        }

        // 객체 할당(GC) 방지를 위한 정적(Static) 큐 재사용
        private static readonly Queue<Transform> s_queue = new Queue<Transform>(64);

        public static T GetComponentInChildren_BFS<T>(this GameObject target, bool includeInactive = false) where T : Component
        {
            if (target == null) return null;

            // 혹시 모를 이전 호출의 잔여 데이터를 지웁니다.
            s_queue.Clear();
            s_queue.Enqueue(target.transform);

            while (s_queue.Count > 0)
            {
                Transform current = s_queue.Dequeue();

                // 2. 비활성화 체크 최적화 (가장 큰 성능 향상 포인트)
                if (!includeInactive && !current.gameObject.activeSelf)
                {
                    // 부모가 꺼져있으면 자식들도 무조건 꺼져있으므로, 
                    // 자식들을 큐에 넣을 필요 없이 바로 다음으로 넘어갑니다(트리 가지치기).
                    continue;
                }

                if (current.TryGetComponent(out T component))
                {
                    s_queue.Clear(); // 메모리 릭(참조 유지) 방지를 위해 큐를 비워줍니다.
                    return component;
                }

                // 3. 프로퍼티 접근 최소화
                // current.childCount는 C++ 엔진 영역을 호출하므로 변수에 캐싱해둡니다.
                int childCount = current.childCount;
                for (int i = 0; i < childCount; i++)
                {
                    s_queue.Enqueue(current.GetChild(i));
                }
            }

            s_queue.Clear();
            return null;
        }
    }
}
