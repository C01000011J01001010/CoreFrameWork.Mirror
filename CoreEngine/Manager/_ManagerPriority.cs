namespace CoreEngine.Manager
{
    /// <summary>
    /// ManagerHub 내부에서 매니저(Leaf)들의 초기화 순서를 결정하는 우선순위
    /// 숫자가 낮을수록 먼저 초기화(Initialize)됩니다.
    /// </summary>
    public enum ManagerPriority
    {
        // [1단계] 최하단 인프라: 외부 자원과 파일을 게임으로 끌어오는 역할
        Infrastructure = 0,

        // [2단계] 정적 데이터: 변경되지 않는 원본 데이터(DB) 세팅
        StaticData = 100,

        // [3단계] 런타임 코어: 게임을 굴러가게 하는 기반 엔진
        RuntimeCore = 200,

        // [4단계] 비즈니스 로직: 유저의 플레이와 직결되는 인게임 로직
        BusinessLogic = 300,

        // [5단계] 최상위 시스템: 모든 준비가 끝난 후 흐름을 통제
        TopLevel = 400
    }
}