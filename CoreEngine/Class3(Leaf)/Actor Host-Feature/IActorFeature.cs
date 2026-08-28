namespace CoreEngine.Actor
{
    /// <summary>
    /// IActorHost에 조립될 수 있는 모든 순수 C#을 위한 인터페이스
    /// </summary>
    public interface IActorFeature
    {
        IActorHost Host { get; }
        // 생성 될 때 자신을 담고 있는 host를 주입받는 유일한 통로
        void Initialize(IActorHost host);
    }
}