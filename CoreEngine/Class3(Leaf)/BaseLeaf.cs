using CoreEngine;
using CoreEngine.EventBus;
using CoreEngine.Extensions;
using UnityEngine;

namespace CoreEngine
{
    public abstract class BaseLeaf : CoreMonoBehaviour
    {
        // 어느 Context 산하로 들어갈지 결정
        [SerializeField] protected ContextScope myScope;

        public void SetScope(ContextScope scope)
        {
            myScope = scope;
            OnSetScope(scope);
        }

        protected virtual void OnSetScope(ContextScope scope)
        {

        }
#if UNITY_EDITOR
        // 유니티 에디터에서 값이 변경되거나, 씬에 배치될 때 자동 호출되는 함수
        protected virtual void OnValidate()
        {
            this.AutoSetupScope(ref myScope);
        }
#endif
    }
}
