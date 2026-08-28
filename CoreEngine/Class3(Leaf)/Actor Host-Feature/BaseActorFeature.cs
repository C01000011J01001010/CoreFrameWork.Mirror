namespace CoreEngine.Actor
{
    /// <summary>
    /// 모든 Feature가 상속받아 사용할 기본 뼈대 클래스
    /// </summary>
    public abstract class BaseActorFeature : IActorFeature
    {
        protected IActorHost _host;

        // 외부(인터페이스)에서는 읽기만 가능하도록 제한
        public IActorHost Host => _host;

        public void Initialize(IActorHost host)
        {
            _host = host;
            OnInitialized(); // 자식 클래스에서 필요한 추가 초기화 진행
        }

        // Feature가 조립된 직후 실행될 커스텀 초기화 로직
        protected virtual void OnInitialized() { }
    }
}