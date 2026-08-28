using System.Collections.Generic;
using UnityEditor.EditorTools;
using UnityEngine;
using UnityEngine.UI;

namespace CoreEngine.Data
{
    /// <summary>
    /// Viewer를 리스트로 관리해주는 객체
    /// </summary>
    /// <typeparam name="DataContainer"></typeparam>
    /// <typeparam name="Data"></typeparam>
    /// <typeparam name="Viewer"></typeparam>
    public class BaseDataViewerProvider<DataContainer, Data, Viewer> : MonoBehaviour
        where DataContainer : BaseDataContainer<Data>
        where Data : BaseData
        where Viewer : BaseDataViewer<DataContainer, Data>
    {
        protected Dictionary<DataContainer, Viewer> showerDictionary = new();


        [SerializeField] protected LayoutGroup containBox;

        GameObject _showerPreset = null;
        public GameObject ViewerPreset
        {
            get
            {
                if (_showerPreset) return _showerPreset;
                else return _showerPreset = containBox?.transform.GetChild(0)?.gameObject;
            }
        }

        public void Awake()
        {
            if (ViewerPreset is null) Debug.LogError($"{gameObject.name} has no Preset Object");
            else ViewerPreset.SetActive(false); // 프리셋은 가져왔으니 꺼버림

            Initialize();
        }

        public virtual void Initialize() 
        {
        }

        public Viewer CreateViewer(DataContainer newContainer)
        {
            if (ViewerPreset is null) return null;

            //poolManager.Spawn(ViewerPreset);
            GameObject inst = Instantiate(ViewerPreset, containBox.transform);
            // 생성되었고         컴포넌트도 잘 가지고 있다면
            if (inst && inst.TryGetComponent(out Viewer result))
            {
                // View에 Model을 연결
                result.Connect(newContainer);

                OnCreateViewerSucceed(result, newContainer);
                result.gameObject.SetActive(true); // 설정 끝나면 켜기

                return result;
            }
            else
            {
                OnCreateViewerFailed(inst, newContainer);
                return null;
            }
        }

        protected virtual void OnCreateViewerSucceed(Viewer newShower, DataContainer newContainer) { }
        protected virtual void OnCreateViewerFailed(GameObject inst, DataContainer newContainer)
        {
            //Instantiate가 실패하고 말았습니다 (프리팹의 널 체크는 위에서 했어요!)
            //메모리 할당에 실패한 거예요!
            if (inst)
            {
                //나중에 저희가 잘못 적용했을 때에 확인할 수 있도록 준비해준 에러메시지!
                Debug.LogError($"Invalid Shower Preset : {inst.name}");
                Destroy(inst);
            }
        }

        //Unique : 유일한 -> 중복으로 추가하지 않을 것임!
        public virtual void AddUnique(DataContainer newContainer)
        {
            //이미 있는 오브젝트라면 추가하지 않습니다!
            if (showerDictionary.ContainsKey(newContainer)) return;

            //shower를 만들고
            //새로운 요소를 추가하고
            //shower에 연결하고
            //그 다음에 layoutgroup에 추가해주기

            Viewer newShower = CreateViewer(newContainer);
            if (newShower is null) return;
            showerDictionary.Add(newContainer, newShower);
            //newShower.Connect(newContainer);

        }
    }
}
