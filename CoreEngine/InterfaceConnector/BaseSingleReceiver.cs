using System;
using UnityEngine;
using CoreEngine.Interface; // InterfaceReceiver가 있는 곳

namespace CoreEngine
{
    /// <summary>
    /// 오직 1개의 인터페이스만 수신하여 사용하는 단순한 MonoBehaviour를 위한 자동화 부모 클래스
    /// </summary>
    public abstract class BaseSingleReceiver<TInterface> : MonoBehaviour
        where TInterface : class
    {
        private InterfaceReceiver<TInterface> _receiver = new();

        // 자식 클래스에서 맘편히 사용할 수 있는 타겟 데이터
        protected TInterface Target => _receiver.Target;

        protected virtual void OnEnable()
        {
            _receiver.OnTargetSet += OnTargetSet;
            _receiver.Bind();
        }

        protected virtual void OnDisable()
        {
            _receiver.OnTargetSet -= OnTargetSet;
            _receiver.Unbind();
        }

        // 타겟이 수신되었을 때 자식 클래스가 원한다면 추가 로직을 작성할 수 있는 콜백
        protected virtual void OnTargetSet(TInterface target) { }
    }
}