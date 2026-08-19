using System.Collections;
using System.Collections.Generic;
using CoreEngine;


public abstract class TickableModuleHub: BaseModuleHub, IModuleHub, ITickable, IFixedTickable
{

    // 모듈 업데이트를 허브에서 처리하기 위해 ITickable, IFixedTickable 모듈을 별도로 관리
    private List<ITickModule> moduleTick = new();
    private List<IFixedTickModule> moduleFixedTick = new();

    public abstract TickGroup TickGroup {  get;}

    public abstract FixedTickGroup FixedTickGroup {  get;}


    // 1차 초기화: 하드코딩된 모듈 우선으로 구성하고, 탐색된 모듈을 뒤에 붙여 전체 리스트 구성 후 Initialize 호출
    public override IEnumerator Initialize()
    {
        yield return base.Initialize();

        // Tick 실행할 객체 선별
        // 미리 리스트에 넣어도 IsActive가 true여야 업데이트 시작
        foreach (var module in initializationOrder)
        {
            if (module is ITickModule tickable)
            {
                moduleTick.Add(tickable);
            }
            if (module is IFixedTickModule fixedTickable)
            {
                moduleFixedTick.Add(fixedTickable);
            }
        }
    }


    public virtual void Tick(float deltaTime)
    {
        foreach (ITickModule module in moduleTick)
        {
            if (module.IsActive)
            {
                module.Tick(deltaTime);
            }
        }
    }

    public virtual void FixedTick(float deltaTime)
    {
        foreach (IFixedTickModule module in moduleFixedTick)
        {
            if (module.IsActive)
            {
                module.FixedTick(deltaTime);
            }
        }
    }
}
