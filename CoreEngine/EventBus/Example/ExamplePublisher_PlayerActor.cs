using CoreEngine.EventBus;
using UnityEngine;

namespace CoreEngine.EventBus.Example
{
    // 명세서를 들고 이벤트를 알리는 객체의 예시
    public class ExamplePublisher_PlayerActor : MonoBehaviour
    {
        [SerializeField] private int _playerId = 1;
        private int _maxHp = 100;
        private int _currentHp = 100;

        // 적에게 맞았을 때 호출되는 함수
        public void TakeDamage(int damage)
        {
            _currentHp -= damage;
            if (_currentHp < 0) _currentHp = 0;

            Debug.Log($"[PlayerActor] 데미지 {damage} 받음. 허공에 이벤트 발행!");

            // 🎯 특정 객체를 찾지 않고 허공(EventBus)에 데이터만 던집니다.
            EventBus<ExampleEvent_PlayerDamaged>.Publish(new ExampleEvent_PlayerDamaged
            {
                PlayerId = _playerId,
                CurrentHp = _currentHp,
                MaxHp = _maxHp
            });
        }
    }
}


