using CoreEngine.EventBus;
using CoreEngine.Manager.Culling;
using UnityEngine;

namespace CoreEngine
{
    
    public abstract class BaseCullingDynamicActor : BaseCullingObjectActor, ITickable
    {
        private Vector3Int _lastGridKey;

        // 최초 스폰과 컬링에 의한 재활성화를 구별하기 위한 방어막 플래그
        private bool _isFirstSpawn = true;

        // 몬스터나 일반 오브젝트 그룹에 맞게 설정 (UpdateManager 연동)
        public virtual TickGroup TickGroup => TickGroup.Object;

        protected override void OnEnable()
        {
            base.OnEnable();

            // 현재 상자의 실제 물리적 위치 격자 계산
            Vector3Int currentGrid = CoreFacade.GetGridKey(transform.position);

            if (_isFirstSpawn)
            {
                // 맵에 처음 스폰되었을 때는 이동한 게 아니므로 현재 위치를 기억
                _lastGridKey = currentGrid;
                _isFirstSpawn = false;
            }
            else
            {
                // 유저님이 찾으신 버그 방어막: 꺼져있는 동안 인위적으로 위치가 바뀌었다면
                if (currentGrid != _lastGridKey)
                {
                    // 매니저에게 "나 꺼져있는 동안 몰래 방 옮겼어! 옛날 딕셔너리방({_lastGridKey})에서 빼고 새 방({currentGrid})으로 옮겨줘!" 라고 이벤트를
                    EventBus<CullingObjectMovedEvent>.Publish(new CullingObjectMovedEvent(this, _lastGridKey, currentGrid));

                    // 그리고 내 최신 좌표 기억 장치를 동기화
                    _lastGridKey = currentGrid;
                }
            }
        }

        public void Tick(float deltaTime)
        {
            // 매 프레임(또는 일정 틱마다) 현재 격자 계산
            Vector3Int currentGrid = CoreFacade.GetGridKey(transform.position);

            // 격자 선을 넘어갔다면
            if (currentGrid != _lastGridKey)
            {
                // Manager에게 "나 옛날 격자에서 빼고 새 격자에 넣어줘!" 라고 방송
                EventBus<CullingObjectMovedEvent>.Publish(new CullingObjectMovedEvent(this, _lastGridKey, currentGrid));

                _lastGridKey = currentGrid;
            }
        }
    }
}