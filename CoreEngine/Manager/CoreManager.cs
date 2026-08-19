using System;

using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using CoreEngine;
/// <summary>
/// 
/// </summary>
/// <typeparam name="TMainManager">TSubManager와 기타 GameObject의 라이프 사이클을 관리하는 객체</typeparam>
/// <typeparam name="TSubManager">실제 작업을 담당하는 매니저 객체</typeparam>
/// <typeparam name="TGameObject">Manager를 제외한 TMainManager에서 관리되는 기타 GameObject</typeparam>
public abstract class CoreManager<TMainManager, TSubManager, TGameObject> : MonoBehaviour//BaseModuleHub<THub, TManager>
    where TMainManager : CoreManager<TMainManager, TSubManager, TGameObject>
    where TSubManager :  IManager, IModule
    where TGameObject :  IInitialize, ILateInitialize, IPriority
{
    protected static TMainManager _instance;
    public static TMainManager Inst => _instance;

    protected Disabled_ManagerHub managerHub;
    

    // 개별 순서를 가진 최우선 초기화 대상들
    //protected List<TSubManager> PreSetManagerList = new();

    //protected Dictionary<Type, TSubManager> managerDict = new();
    protected Dictionary<Type, List<TGameObject> /*중복된 객체 허용*/> objectsDict = new();

    protected virtual void Awake()
    {
        if (!(this as TMainManager).TryMakeSingleton(ref _instance))
        {
            Destroy(gameObject);
        }

        // 하이라키 자식객체에서 ManagerHub를 찾아서 가져오기
        managerHub = gameObject.GetComponentInChildren<Disabled_ManagerHub>();
    }


    //protected bool TryGetOrAddManager<T>() where T : MonoBehaviour, TSubManager
    //{
    //    T manager = gameObject.GetOrAddComponent<T>();
    //    if (manager is null)
    //    {
    //        Debug.LogAssertion($"GetOrAddComponent Failed => {typeof(T).Name}");
    //        return false;
    //    }

    //    if (!managerDict.TryAdd(manager.GetType(), manager))
    //    {
    //        Debug.LogWarning($"Manager({typeof(T).Name}) is alreay Added");
    //        return false;
    //    }
    //    PreSetManagerList.Add(manager);
    //    return true;
    //}

    

    private static List<TGameObject> GetRawObjects<T>() where T : MonoBehaviour, TGameObject
    {
        Type wantType = typeof(T);

        if (Inst.objectsDict.ContainsKey(wantType))
        {
            return Inst.objectsDict[wantType];
        }
        Debug.LogWarning($"Type({wantType.Name}) 객체 없음");
        return null;
    }

    #region 관리 객체 접근 헬퍼
    public static T GetObject<T>() where T : MonoBehaviour, TGameObject
    {
        if (Inst == null)
        {
            Debug.LogError($"{typeof(TMainManager).Name} 인스턴스가 존재하지 않습니다.");
            return null;
        }

        // 딕셔너리에 들어있는 리스트에서 첫번째 원소를 반환
        List<TGameObject> rawObjects = GetRawObjects<T>();
        TGameObject result = default;
        if (rawObjects != null) result = rawObjects[0];
        return (T)result;
    }

    public static T[] GetObjects<T>() where T : MonoBehaviour, TGameObject
    {
        if (Inst == null)
        {
            Debug.LogError($"{typeof(TMainManager).Name} 인스턴스가 존재하지 않습니다.");
            return null;
        }

        // 딕셔너리에 있는 리스트를 T로 캐스팅하여 배열로 반환
        return GetRawObjects<T>()?.Cast<T>().ToArray();
    }

    public static T GetManager<T>() where T : MonoBehaviour, TSubManager
    {
        if (Inst == null)
        {
            Debug.LogError($"{typeof(TMainManager).Name} 인스턴스가 존재하지 않습니다.");
            return null;
        }

        //Type managerType = typeof(T);

        //if (Inst.managerDict.TryGetValue(managerType, out TSubManager manager))
        //{
        //    return (T)manager;
        //}

        //Debug.LogError("정의되지 않은 매니저 객체");
        //return null;
        return Inst.managerHub.GetModule<T>();
    }
    #endregion
}