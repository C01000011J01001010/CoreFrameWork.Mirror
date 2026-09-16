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

        public bool IsInit { get; private set; } = false;

        /// <summary>
        /// <see cref="IActorHost"/>의 Awake에서 1번만 실행
        /// </summary>
        public void Initialize(IActorHost host)
        {
            if (IsInit) return;
            _host = host;
            OnInitialized(); // 자식 클래스에서 필요한 추가 초기화 진행
            IsInit = true;
        }
        /// <summary>
        /// <see cref="Initialize"/>에 의해 실행될 커스텀 초기화 로직
        /// </summary>
        protected virtual void OnInitialized() { }
    }
}