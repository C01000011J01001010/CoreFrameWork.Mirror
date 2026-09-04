using UnityEngine;
using CoreEngine.Extensions;

namespace CoreEngine
{
    public abstract class CoreMonoBehaviour : MonoBehaviour
    {
        // 확장 메서드를 통해 기존과 똑같은 문법으로 사용 가능
        protected virtual void OnEnable() => this.RegisterTick();
        protected virtual void OnDisable() => this.UnregisterTick();
    }
}