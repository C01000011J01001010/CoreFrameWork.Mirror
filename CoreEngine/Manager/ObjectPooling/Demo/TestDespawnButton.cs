using UnityEngine;
using UnityEngine.UI;
using CoreEngine.Helpers;

namespace CoreEngine.Manager.Pool.Test
{
    [RequireComponent(typeof(Button))]
    public class TestDespawnButton : MonoBehaviour
    {
        private Button _button;

        private void Awake()
        {
            _button = GetComponent<Button>();
            _button.onClick.AddListener(OnClickDespawn);
        }

        private void OnClickDespawn()
        {
            if (TestPoolTracker.SpawnedObjects.Count == 0)
            {
                LogHelper.Log("[TestDespawnButton] 풀에 반환할 활성화된 객체가 없습니다.");
                return;
            }

            // 스택에서 가장 마지막에 생성된 객체를 꺼냄
            GameObject targetObj = TestPoolTracker.SpawnedObjects.Pop();

            // 유니티 씬 전환 중 파괴(Fake Null)되지 않고 온전히 살아있는 경우에만 접근[cite: 13]
            if (targetObj != null && targetObj.TryGetComponent(out IPoolable poolableItem))
            {
                // Manager를 거치지 않고 IObjectPool 참조를 이용해 즉각 Release 처리!
                poolableItem.RootPool.Release(targetObj);
            }
            else
            {
                LogHelper.Log("[TestDespawnButton] 풀에 반환할 객체가 이미 파괴되었거나 IPoolable을 구현하지 않은 객체입니다.");
            }    
        }
    }
}