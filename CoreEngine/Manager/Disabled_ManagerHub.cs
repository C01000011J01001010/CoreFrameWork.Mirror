using System;
using CoreEngine;
/// <summary>
/// 하이라키에서 부모객체로 GameManager 또는 WorldManager 필요
/// </summary>
public abstract class Disabled_ManagerHub : BaseModuleHub
{
    // 로딩 진행률 계산용 총 작업 수
    private int totalInitializeTaskCount;

    /// <summary>
    /// Initialize + LateInitialize 작업 수
    /// </summary>
    public int GetInitializeTask() => totalInitializeTaskCount;

    protected override void Awake()
    {
        base.Awake();
        // 총 초기화 작업 수 계산
        totalInitializeTaskCount = InitializeQueue.Count;

        foreach (var module in InitializeQueue)
        {
            if (module is ILateInitialize)
            {
                totalInitializeTaskCount++;
            }
        }
    }

    protected override bool TryInputModule(IModule module)
    {
        if (module is not IManager)
        {
            throw new InvalidOperationException(
                "module is not IManager");
        }
        return base.TryInputModule(module);
    }

    protected override void ValidateModule(IModule module)
    {
        CheckPolicy<IManager>(module);
    }
}