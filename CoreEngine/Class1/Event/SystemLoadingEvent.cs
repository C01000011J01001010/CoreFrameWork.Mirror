// 로딩 진행 상황을 중계하는 이벤트
using CoreEngine.EventBus;

public struct SystemLoadingEvent : IEvent
{
    public enum State { Start, Progress, Complete }

    public State LoadingState;
    public string Message;
    public float Progress; // 0.0f ~ 1.0f

    public SystemLoadingEvent(State state, string message, float progress)
    {
        LoadingState = state;
        Message = message;
        Progress = progress;
    }
}