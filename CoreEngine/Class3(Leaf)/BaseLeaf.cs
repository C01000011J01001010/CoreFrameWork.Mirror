using CoreEngine;
using CoreEngine.EventBus;
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
            // [방어막 1] 유니티 에디터가 코드를 막 컴파일 중이거나, 객체가 파괴되는 중일 때 접근 방지
            if (this == null) return;

            // [방어막 2] 이 객체가 씬에 존재하는 객체가 아니라 '프리팹 에셋' 파일 자체라면 로직 실행 중단
            if (!gameObject.scene.IsValid()) return;

            // 아직 스코프가 None(미지정) 상태일 때만 자동 추론을 작동시킵니다.
            if (myScope == ContextScope.None)
            {
                // 현재 이 스크립트가 물리적으로 배치된 씬의 이름을 확인합니다.
                // 하이라키에 올려두는 순간 즉시 판별됩니다.
                string mySceneName = gameObject.scene.name;

                if (mySceneName == ProjectContext.GlobalScene)
                {
                    myScope = ContextScope.Project;
                    // 에디터 인스펙터 창의 값을 강제로 갱신하고 저장 상태로 만듭니다.
                    UnityEditor.EditorUtility.SetDirty(this);
                }
                else if (!string.IsNullOrEmpty(mySceneName))
                {
                    // 글로벌 씬이 아닌 일반 씬에 배치되었다면 안전하게 Scene 소속으로 고정해 줍니다.
                    myScope = ContextScope.Scene;
                    UnityEditor.EditorUtility.SetDirty(this);
                }
            }
        }
#endif
    }
}
