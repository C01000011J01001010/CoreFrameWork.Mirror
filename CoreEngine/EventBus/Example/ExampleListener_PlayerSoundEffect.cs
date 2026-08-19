using CoreEngine.EventBus;
using UnityEngine;

namespace CoreEngine.EventBus.Example
{
    /// <summary>
    /// 메모리 해제 수동조작 예시
    /// </summary>
    public class ExampleListener_PlayerSoundEffect : MonoBehaviour
    {
        [SerializeField] private int _targetPlayerId = 1;
        [SerializeField] private AudioSource _audioSource;
        [SerializeField] private AudioClip _hitSound;

        private void OnEnable()
        {
            EventBus<ExampleEvent_PlayerDamaged>.Subscribe(PlayHitSound);

        }
        private void OnDisable()
        {
            EventBus<ExampleEvent_PlayerDamaged>.Unsubscribe(PlayHitSound);
        }

        private void PlayHitSound(ExampleEvent_PlayerDamaged evt)
        {
            if (evt.PlayerId != _targetPlayerId) return;

            // 체력이 깎였을 때 피격 사운드 재생
            if (_audioSource != null && _hitSound != null)
            {
                _audioSource.PlayOneShot(_hitSound);
                Debug.Log($"[PlayerSoundEffect] 플레이어 {evt.PlayerId} 피격 사운드 재생!");
            }
        }
    }
}
