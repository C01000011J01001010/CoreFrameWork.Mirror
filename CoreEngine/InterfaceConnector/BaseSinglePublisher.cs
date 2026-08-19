using UnityEngine;
using CoreEngine.Interface; // InterfacePublisher가 있는 곳

namespace CoreEngine
{
    /// <summary>
    /// 오직 1개의 인터페이스만 외부로 제공하는 단순한 MonoBehaviour를 위한 자동화 부모 클래스
    /// </summary>
    public abstract class BaseSinglePublisher<TInterface> : MonoBehaviour
        where TInterface : class
    {
        private InterfacePublisher<TInterface> _publisher;

        protected virtual void Awake()
        {
            // 나 자신(this)이 TInterface를 구현했는지 확인하고, 안전하게 부품을 생성합니다.
            TInterface provider = this as TInterface;
            if (provider != null)
            {
                _publisher = new InterfacePublisher<TInterface>(provider);
            }
            else
            {
                Debug.LogError($"[BaseSinglePublisher] {gameObject.name}이(가) {typeof(TInterface).Name} 인터페이스를 상속받지 않았습니다!");
            }
        }

        // 자식 클래스에서 재정의(override)할 일이 생길 수 있으므로 virtual로 열어둡니다.
        protected virtual void OnEnable() => _publisher?.Bind();
        protected virtual void OnDisable() => _publisher?.Unbind();
    }
}