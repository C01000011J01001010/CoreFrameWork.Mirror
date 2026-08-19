using UnityEngine;

namespace CoreEngine.Environment
{
    public abstract class BaseEnvironment : MonoBehaviour
    {
        protected abstract string FolderName { get;}
        protected virtual void Awake()
        {

#if !UNITY_EDITOR
            Destroy(this);
#endif
        }

        protected virtual void OnDestroy()
        {
#if !UNITY_EDITOR
            Transform folder = transform.Find(FolderName);
            if (folder == null) return;
            foreach (var renderer in folder.GetComponentsInChildren<MeshRenderer>(true))
            {
                // 1. 메쉬 데이터(형태) 메모리 해제
                if (renderer.TryGetComponent(out MeshFilter filter))
                {
                    Destroy(filter);
                }
                // 2. 그리는 도구(재질 등) 메모리 해제
                Destroy(renderer);
            }
#endif
        }
    }
}

