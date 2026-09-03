using UnityEngine;
using UnityEngine.UI;
using CoreEngine.Facades;
using CoreEngine.Helpers;

namespace CoreEngine.Manager.Pool.Test
{
    [RequireComponent(typeof(Button))]
    public class TestSpawnButton : MonoBehaviour
    {
        public TestPoolType targetPoolType = TestPoolType.Poolable;
        private Button _button;

        private void Awake()
        {
            _button = GetComponent<Button>();
            _button.onClick.AddListener(OnClickSpawn);
        }

        private void OnClickSpawn()
        {
            // 3계층 규칙에 따라 인터페이스가 아닌 구체 클래스 타입으로 호출[cite: 1]
            var poolManager = CoreFacade.GetManager<TestObjectPoolManager>();

            if (poolManager == null)
            {
                LogHelper.Log("[TestSpawnButton] TestObjectPoolManager가 Hub에 등록되지 않았습니다.");
                return;
            }

            // 구별하기 쉽도록 랜덤한 좌표에 스폰
            Vector3 randomPos = new Vector3(Random.Range(-3f, 3f), Random.Range(-3f, 3f), 0);
            Quaternion randomRot = new Quaternion(Random.Range(0, 360), Random.Range(0, 360), Random.Range(0, 360),1);
            IPoolable spawnedObj = poolManager.Spawn(targetPoolType, randomPos, randomRot);

            if (spawnedObj != null)
            {
                TestPoolTracker.SpawnedObjects.Push(spawnedObj);
            }
        }
    }
}