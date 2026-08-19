// 씬 전환을 지시하는 이벤트 (싱글톤 호출 대신 사용)
using CoreEngine.EventBus;

public struct SceneLoadRequestEvent : IEvent
{
    public string TargetSceneName;
    public SceneLoadRequestEvent(string targetSceneName)
    {
        TargetSceneName = targetSceneName;
    }
}