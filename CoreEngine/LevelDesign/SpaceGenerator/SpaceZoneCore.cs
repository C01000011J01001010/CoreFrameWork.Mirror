using CoreEngine.LevelDesign;
using System;
using UnityEngine;

namespace CoreEngine.LevelDesign
{
    /// <summary>
    /// [모듈 조립 시스템의 코어]
    /// Transform의 변화를 감지하여 부착된 모든 하위 모듈들에게 이벤트를 방송(Broadcast)합니다.
    /// </summary>
    [ExecuteAlways]
    [RequireComponent(typeof(BoxCollider))]
    public class SpaceZoneCore : BaseSpace
    {
        [HideInInspector] public Vector3 zoneSize = new Vector3(10f, 10f, 10f);

        // 🌟 핵심: 하위 모듈들이 구독할 수 있는 C# 이벤트 (관찰자 패턴)
        public event Action OnZoneModified;

        protected override string FolderName => "";

        protected override void Awake()
        {
            base.Awake();
            if (TryGetComponent(out BoxCollider mainCollider))
            {
                mainCollider.isTrigger = true;
                mainCollider.size = Vector3.one;
            }
        }

        private void Update()
        {
            // 여기서 transform.hasChanged 플래그를 단 한 번만! 독점해서 소모합니다.
            if (!Application.isPlaying && transform.hasChanged)
            {
                if (transform.localScale != zoneSize)
                {
                    // scale변경을 감지
                    zoneSize = transform.localScale;
                }
                transform.hasChanged = false;

                // 크기가 변했으니 구독 중인 모든 모듈들에게 갱신 명령을 하달합니다!
                TriggerRebuild();
            }
        }

        // 외부(에디터 등)에서 수동으로 갱신을 트리거할 때 호출하는 헬퍼 메서드
        public void TriggerRebuild()
        {
            OnZoneModified?.Invoke();
        }
    }
}