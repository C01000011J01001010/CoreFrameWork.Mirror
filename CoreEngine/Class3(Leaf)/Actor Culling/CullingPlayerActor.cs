using CoreEngine.EventBus;
using CoreEngine.Hub;
using CoreEngine.Facades;
using CoreEngine.Manager.Culling;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace CoreEngine
{
    public class CullingPlayerActor : BaseActor, ITickable
    {
        // 플레이어 컨트롤러 또는 위치 추적 스크립트 내부
        private Vector3Int _lastGridKey;

        public TickGroup TickGroup => TickGroup.Object;

        // 플레이어가 씬에 로드/스폰되는 즉시 이벤트 발생
        protected override void OnEnable()
        {
            base.OnEnable();

            // 내 현재 격자를 구해서
            Vector3Int currentGrid = CoreFacade.GetGridKey(transform.position);
            _lastGridKey = currentGrid;

            // 매니저에게 즉시 "나 여기 스폰됐어! 주변 애들 켜줘!" 라고 방송
            EventBus<CullingPlayerGridChangedEvent>.Publish(new CullingPlayerGridChangedEvent(currentGrid));
        }

        // UpdateManager에 등록된 Tick 등에서 실행
        public void Tick(float deltaTime)
        {
            Vector3Int currentGrid = CoreFacade.GetGridKey(transform.position);

            // 격자가 이전과 달라졌을 때만! (c거리 트리거의 정수화 버전)
            if (currentGrid != _lastGridKey)
            {
                _lastGridKey = currentGrid;

                // 허공에 "나 격자 바뀌었다!" 방송 (수백 개의 객체 Culling 로직이 단 한 번 실행됨)
                EventBus<CullingPlayerGridChangedEvent>.Publish(new CullingPlayerGridChangedEvent(currentGrid));
            }
        }
    }
}
