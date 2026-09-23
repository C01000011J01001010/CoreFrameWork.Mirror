namespace CoreEngine
{
    internal enum ExecutionOrder
    {
        //단독씬 테스트 구동
        TestDriver = -100,

        //게임 시작을 위한 인프라(뼈와 살)
        UpdateDirector = -90,
        TimeDirector = -80,
        SceneFlowDirector = -70,

        //게임에 옷입히기
        Loading = -60,

        //게임 구동 시작
        ProjectContext = -40,
        SceneContext = -20,
        Hub = -10,
    }
}