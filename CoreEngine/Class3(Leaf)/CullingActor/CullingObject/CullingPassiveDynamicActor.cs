using CoreEngine.Manager.Culling;
using UnityEngine;

namespace CoreEngine
{
    internal class CullingPassiveDynamicActor : BaseCullingDynamicActor
    {
        public override CullingType cullingType => CullingType.PassiveDynamic;

        protected override void OnEnable()
        {
            base.OnEnable();
        }

        public override void SetVisualActive(bool isActive)
        {
            SetFlagVisualActive(isActive);

            if (gameObject.activeSelf != isActive)
            {
                gameObject.SetActive(isActive);
            }
        }

        public override void SetPhysicsActive(bool isActive)
        {
            SetFlagPhysicsActive(isActive);

            // 콜라이더 끄기/켜기
            for (int i = 0; i < _colliders.Length; i++)
            {
                if (_colliders[i] != null && _colliders[i].enabled != isActive)
                {
                    _colliders[i].enabled = isActive;
                }
            }

            // Rigidbody 얼리기/녹이기 (지하로 추락 방지 및 CPU 최적화)
            for (int i = 0; i < _rigidbodies.Length; i++)
            {
                if (_rigidbodies[i] != null)
                {
                    // 물리가 꺼지면(isActive=false) Kinematic을 켜서(true) 허공에 고정
                    bool targetKinematicState = !isActive;

                    if (_rigidbodies[i].isKinematic != targetKinematicState)
                    {
                        _rigidbodies[i].isKinematic = targetKinematicState;
                    }
                }
            }
        }
    }
}