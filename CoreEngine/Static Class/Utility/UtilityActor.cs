using System;
using UnityEngine;
using CoreEngine.Hub;

namespace CoreEngine
{
    public static class UtilityActor
    {
        public static GameObject Spawn<ActorGroup>(GameObject prefab)
           where ActorGroup : Enum
        {
            return Spawn<ActorGroup>(prefab, Vector3.zero, Quaternion.identity);
        }

        public static GameObject Spawn<ActorGroup>(GameObject prefab, Vector3 spawnPosition)
           where ActorGroup : Enum
        {
            return Spawn<ActorGroup>(prefab, spawnPosition, Quaternion.identity);
        }
        public static GameObject Spawn<ActorGroup>(GameObject prefab, Vector3 spawnPosition, Quaternion rotation)
            where ActorGroup : Enum
        {
            GameObject inst = UnityEngine.Object.Instantiate(prefab, spawnPosition, rotation);

            // 2. 컴포넌트 가져와서 인터페이스 확인 후 OnSpawn() 호출
            IActorSpawn actor = inst.GetComponent<IActorSpawn>();
            if (UtilitySystem.isUnityNull(actor))
            {
                actor.OnSpawn();
            }
            return inst;
        }
    }
}