using CoreEngine.Manager.Culling;
using UnityEngine;

namespace CoreEngine
{
    internal class CullingActiveDynamicActor : BaseCullingDynamicActor
    {
        public override CullingType cullingType => CullingType.ActiveDynamic;

        public override void SetVisualActive(bool isActive)
        {
            SetFlagVisualActive(isActive);

            // 본체(Root)는 살려두고 시각적 모델(Renderer)만 On/Off
            for (int i = 0; i < _renderers.Length; i++)
            {
                if (_renderers[i] != null && _renderers[i].enabled != isActive)
                {
                    _renderers[i].enabled = isActive;
                }
            }
        }

        public override void SetPhysicsActive(bool isActive)
        {
            // 능동적 객체(몬스터)는 화면 밖에서도 중력을 받아야 하므로 물리는 항상 켜둡니다.
            SetFlagPhysicsActive(true);
            return;
        }
    }
}