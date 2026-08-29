using CoreEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CoreEngine.Extentions;

public abstract class BaseModuleHub : MonoBehaviour, IModuleHub
{
    // Init -> LateInit 순서를 제어
    public bool NeedLateInit => LateInitializeQueue.Count > 0;

    // 초기화된 순서를 기억하는 전체 모듈 리스트
    protected readonly List<IModule> initializationOrder = new();

    // 초기화 순서대로 호출하기 위한 큐
    protected readonly Queue<IModule> InitializeQueue = new();
    protected readonly Queue<ILateInitialize> LateInitializeQueue = new();

    // 중복 모듈 방지
    protected readonly HashSet<Type> moduleTypeSet = new();

    // 사용 가능한 모듈 접근
    protected readonly Dictionary<Type, IModule> moduleDict = new();



    public virtual void Exit()
    {
        for (int i = initializationOrder.Count - 1; i >= 0; i--)
        {
            initializationOrder[i].Exit();
        }

        initializationOrder.Clear();
    }

    protected virtual void Awake()
    {
        RegisterPreset();
        RegisterDiscovered();
    }

    public virtual IEnumerator Initialize()
    {
        while (InitializeQueue.Count > 0)
        {
            IModule module = InitializeQueue.Dequeue();
            yield return module.Initialize();

            initializationOrder.Add(module);

            if (module is ILateInitialize lateModule)
            {
                LateInitializeQueue.Enqueue(lateModule);
            }
            else
            {
                SetModuleReady(module);
            }

            yield return null;
        }
    }

    public virtual IEnumerator LateInitialize()
    {
        while (LateInitializeQueue.Count > 0)
        {
            ILateInitialize lateModule = LateInitializeQueue.Dequeue();

            yield return lateModule.LateInitialize();

            SetModuleReady((IModule)lateModule);

            yield return null;
        }
    }

    #region Awake에서 처리할 함수들

    protected virtual void RegisterPreset() { }

    private void RegisterDiscovered()
    {
        var modules = GetComponents<IModule>();

        List<IModule> priorityModules = new();
        List<IModule> normalModules = new();

        foreach (var module in modules)
        {
            if (module is BaseModuleHub hub && hub != this)
                continue;

            if (module is IPriority)
            {
                priorityModules.Add(module);
            }
            else
            {
                normalModules.Add(module);
            }
        }

        priorityModules.Sort((a, b) =>
            ((IPriority)a).Priority.CompareTo(((IPriority)b).Priority));

        foreach (var module in priorityModules)
        {
            TryInputModule(module);
        }

        foreach (var module in normalModules)
        {
            TryInputModule(module);
        }
    }

    #endregion

    #region 모듈 접근 헬퍼

    public T GetModule<T>() //where T : IModule
    {
        Type moduleType = typeof(T);

        if (moduleDict.TryGetValue(moduleType, out IModule module))
        {
            return (T)module;
        }

        Debug.LogError($"Object({moduleType.Name}) is not in moduleDict");
        return default;
    }

    #endregion

    #region 하드코딩 등록 헬퍼

    protected void TryGetOrAddMonoModule<T>()
        where T : MonoBehaviour, IModule
    {
        T module = gameObject.GetOrAddComponent<T>();
        TryInputModule(module);
    }

    protected virtual bool TryInputModule(IModule module)
    {
        if (module == null)
        {
            throw new ArgumentNullException(nameof(module));
        }
        // 잘못된 허브-모듈 연결이 감지되면 개발자에게 알리는 용도
        ValidateModule(module);

        Type moduleType = module.GetType();

        if (moduleTypeSet.Add(moduleType))
        {
            InitializeQueue.Enqueue(module);
            return true;
        }

        Debug.LogWarning($"Module({moduleType.Name}) is already added");
        return false;
    }



    #endregion

    #region Policy System

    /// <summary>
    /// 허브에 연결하는 모듈이 의미있는 연결인지 확인하는 정책.
    /// <para>자동화된 검증은 CheckPolicy 계열을 사용</para>
    /// <para>명시적 실패 처리는 ThrowPolicyViolation 사용</para>
    /// </summary>
    protected abstract void ValidateModule(IModule module);

    /// <summary>
    /// 모듈이 특정 요구 타입을 만족하는지 검사하는 자동 정책 체크
    /// </summary>
    /// <typeparam name="TRequired">요구되는 타입</typeparam>
    protected void CheckPolicy<TRequired>(IModule module)
    {
        if (module is not TRequired)
        {
            ThrowPolicyViolation<TRequired>(module);
        }
    }

    /// <summary>
    /// 정책 위반 시 예외 발생
    /// </summary>
    protected void ThrowPolicyViolation(string message)
    {
        throw new InvalidOperationException(message);
    }

    /// <summary>
    /// 타입 기반 정책 위반 처리
    /// </summary>
    private void ThrowPolicyViolation(IModule module, Type requiredType)
    {
        ThrowPolicyViolation(
            $"{module.GetType().Name} must implement {requiredType.Name}");
    }

    /// <summary>
    /// 제네릭 타입 기반 정책 위반 처리
    /// </summary>
    private void ThrowPolicyViolation<TRequired>(IModule module)
    {
        ThrowPolicyViolation(module, typeof(TRequired));
    }

    #endregion

    private void SetModuleReady(IModule module)
    {
        moduleDict[module.GetType()] = module;
        module.SetActive(true);
    }
}

